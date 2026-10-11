using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KartGame.KartSystems;
using KartGame.Traps;
using KartGame.Track;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class TrapTestInput : MonoBehaviour, IInput
{
    public bool accelerate;
    public bool brake;
    public InputData GenerateInput() => new InputData { Accelerate = accelerate, Brake = brake };
}

public class LapTrapTests
{
    readonly List<GameObject> m_Objects = new List<GameObject>();
    Action m_PreviousLapDisplay;

    GameObject MakeObject(string name)
    {
        var g = new GameObject(name); m_Objects.Add(g); return g;
    }

    [SetUp]
    public void Setup()
    {
        m_PreviousLapDisplay = TimeDisplay.OnUpdateLap;
        TimeDisplay.OnUpdateLap = delegate { };
        var ground = MakeObject("Trap test ground"); ground.layer = 9;
        ground.transform.position = new Vector3(0, -.1f, 0);
        ground.AddComponent<BoxCollider>().size = new Vector3(200, .2f, 200);
    }

    [TearDown]
    public void Teardown()
    {
        foreach (var g in m_Objects) if (g != null) Object.DestroyImmediate(g);
        m_Objects.Clear(); TimeDisplay.OnUpdateLap = m_PreviousLapDisplay;
    }

    ArcadeKart MakeKart(Vector3 position)
    {
        var root = MakeObject("Trap test kart"); root.SetActive(false); root.transform.position = position;
        var body = root.AddComponent<Rigidbody>(); body.mass = 150; body.constraints = RigidbodyConstraints.FreezeRotation;
        root.AddComponent<TrapTestInput>();
        var collider = root.AddComponent<BoxCollider>(); collider.size = new Vector3(1, .3f, 1.4f);
        var kart = root.AddComponent<ArcadeKart>();
        kart.CenterOfMass = root.transform;
        kart.JumpVFX = MakeObject("Test landing effect");
        kart.baseStats = new ArcadeKart.Stats { TopSpeed = 20, Acceleration = 3, AccelerationCurve = .2f,
            ReverseSpeed = 5, ReverseAcceleration = 3, Braking = 10, Grip = .95f, Steer = 5 };
        var wheels = new List<WheelCollider>();
        foreach (float x in new[] { -.4f, .4f }) foreach (float z in new[] { -.55f, .55f })
        {
            var wheel = new GameObject("Test wheel"); wheel.transform.SetParent(root.transform, false);
            wheel.transform.localPosition = new Vector3(x, -.1f, z);
            wheel.layer = 2; var wc = wheel.AddComponent<WheelCollider>(); wc.radius = .3f; wheels.Add(wc);
        }
        kart.FrontLeftWheel = wheels[0]; kart.FrontRightWheel = wheels[1];
        kart.RearLeftWheel = wheels[2]; kart.RearRightWheel = wheels[3];
        root.SetActive(true); return kart;
    }

    BoostSpikeZone MakeZone(LapZoneMode mode)
    {
        var zone = MakeObject("Trap test zone").AddComponent<BoostSpikeZone>();
        var box = zone.GetComponent<BoxCollider>(); box.isTrigger = true;
        box.center = Vector3.up; box.size = new Vector3(30, 3, 40);
        zone.modesByLap = new[] { mode }; zone.ApplyLap(1); return zone;
    }

    IEnumerator Settle(ArcadeKart kart)
    {
        for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
        Assert.That(kart.GroundPercent, Is.GreaterThan(0), "The test kart must contact the real ground.");
    }

    [Test]
    public void SchedulesAreOneBasedAndPreserveLastConfiguration()
    {
        var zone = MakeZone(LapZoneMode.Boost);
        zone.modesByLap = new[] { LapZoneMode.Boost, LapZoneMode.Spikes, LapZoneMode.Inactive };
        Assert.That(zone.GetModeForLap(1), Is.EqualTo(LapZoneMode.Boost));
        Assert.That(zone.GetModeForLap(2), Is.EqualTo(LapZoneMode.Spikes));
        Assert.That(zone.GetModeForLap(3), Is.EqualTo(LapZoneMode.Inactive));
        Assert.That(zone.GetModeForLap(8), Is.EqualTo(LapZoneMode.Inactive));
        zone.modesByLap = Array.Empty<LapZoneMode>();
        Assert.That(zone.GetModeForLap(1), Is.EqualTo(LapZoneMode.Inactive));
    }

    [Test]
    public void RealLapObjectiveIgnoresStartCrossingAndDoesNotStartLapFour()
    {
        var objective = MakeObject("Lap objective").AddComponent<ObjectiveCompleteLaps>();
        objective.enabled = false; objective.gameMode = GameMode.Laps; objective.lapsToComplete = 3;
        // Finish notification requires these HUD managers; no UI registration runs in this focused test.
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        typeof(Objective).GetField("m_ObjectiveHUDManger", flags).SetValue(objective, MakeObject("Objective HUD").AddComponent<ObjectiveHUDManger>());
        typeof(Objective).GetField("m_NotificationHUDManager", flags).SetValue(objective, MakeObject("Notification HUD").AddComponent<NotificationHUDManager>());
        var finish = MakeObject("Finish").AddComponent<LapObject>();
        finish.finishLap = true; finish.gameMode = GameMode.Laps; finish.LapsCount = 0;
        objective.RegisterPickup(finish);
        var controller = MakeObject("Controller").AddComponent<LapTrapController>(); controller.enabled = false;
        controller.lapObjective = objective;
        var zone = MakeZone(LapZoneMode.Boost);
        zone.modesByLap = new[] { LapZoneMode.Boost, LapZoneMode.Spikes, LapZoneMode.Inactive };
        controller.traps.Add(zone); controller.enabled = true;
        var started = new List<int>(); objective.LapStarted += started.Add;

        Assert.That(controller.CurrentLap, Is.EqualTo(1));
        objective.UnregisterPickup(finish); // departure: the stock timer's first crossing
        Assert.That(objective.currentLap, Is.Zero);
        Assert.That(zone.CurrentMode, Is.EqualTo(LapZoneMode.Boost));
        objective.UnregisterPickup(finish);
        Assert.That(controller.CurrentLap, Is.EqualTo(2));
        Assert.That(zone.CurrentMode, Is.EqualTo(LapZoneMode.Spikes));
        controller.enabled = false; objective.UnregisterPickup(finish);
        controller.enabled = true; // late/re-enabled listeners synchronize to the current lap
        Assert.That(controller.CurrentLap, Is.EqualTo(3));
        Assert.That(zone.CurrentMode, Is.EqualTo(LapZoneMode.Inactive));
        objective.UnregisterPickup(finish);
        Assert.That(objective.isCompleted, Is.True);
        Assert.That(controller.CurrentLap, Is.EqualTo(3));
        Assert.That(started, Is.EqualTo(new[] { 2, 3 }));
    }

    [UnityTest]
    public IEnumerator SpikeBrakesToZeroWithThrottleHeldThenRestoresThrottle()
    {
        var kart = MakeKart(new Vector3(0, .5f, 0)); yield return Settle(kart);
        var input = kart.GetComponent<TrapTestInput>(); input.accelerate = true;
        kart.Rigidbody.linearVelocity = Vector3.forward * 12;
        kart.ApplyTrapBrake(12);
        float previous = 12; int ticks = 0;
        var previousSimulationMode = Physics.simulationMode;
        // Measure the controller's exact stop before tire/suspension forces introduce residual motion.
        // The compound-trigger and boost tests below retain the full automatic physics simulation.
        Physics.simulationMode = SimulationMode.Script;
        try
        {
            while (kart.IsTrapBraking && ticks++ < 100)
            {
                yield return new WaitForFixedUpdate();
                float speed = Vector3.ProjectOnPlane(kart.Rigidbody.linearVelocity, Vector3.up).magnitude;
                Assert.That(speed, Is.LessThanOrEqualTo(previous + .01f)); previous = speed;
                Assert.That(kart.Input.Accelerate, Is.False);
            }
            Assert.That(ticks, Is.GreaterThan(1), "Braking must be progressive.");
            Assert.That(kart.IsTrapBraking, Is.False);
            Assert.That(previous, Is.LessThan(.01f));
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(kart.Input.Accelerate, Is.True);
            Assert.That(kart.Rigidbody.linearVelocity.z, Is.GreaterThan(.02f));
        }
        finally { Physics.simulationMode = previousSimulationMode; }
    }

    [UnityTest]
    public IEnumerator TriggerFiresOnceForCompoundKartAndRearmsAfterExit()
    {
        MakeZone(LapZoneMode.Spikes);
        var kart = MakeKart(new Vector3(0, .5f, -30)); yield return Settle(kart);
        var extra = new GameObject("Second chassis collider"); extra.transform.SetParent(kart.transform, false);
        extra.AddComponent<BoxCollider>().size = Vector3.one * .6f;
        kart.Rigidbody.position = new Vector3(0, .5f, -8); kart.Rigidbody.linearVelocity = Vector3.forward * 6;
        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.That(kart.IsTrapBraking, Is.True);
        for (int i = 0; i < 70; i++) yield return new WaitForFixedUpdate();
        Assert.That(kart.IsTrapBraking, Is.False, "Standing inside the spikes cannot re-trigger the stop.");
        kart.GetComponent<TrapTestInput>().accelerate = true;
        for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
        Assert.That(kart.IsTrapBraking, Is.False);
        Assert.That(kart.Rigidbody.linearVelocity.z, Is.GreaterThan(.1f));
        kart.Rigidbody.position = new Vector3(0, .5f, -30); Physics.SyncTransforms();
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        kart.Rigidbody.position = new Vector3(0, .5f, -8); Physics.SyncTransforms();
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.That(kart.IsTrapBraking, Is.True);
    }

    [UnityTest]
    public IEnumerator BoostTimersAreIndependentAndModeChangeWaitsForNextEntry()
    {
        var zone = MakeZone(LapZoneMode.Boost); zone.boostStats.MaxTime = .5f;
        var first = MakeKart(new Vector3(-3, .5f, -30));
        var second = MakeKart(new Vector3(3, .5f, -30)); yield return Settle(first);
        first.Rigidbody.position = new Vector3(-3, .5f, 0); Physics.SyncTransforms();
        for (int i = 0; i < 12; i++) yield return new WaitForFixedUpdate();
        Assert.That(first.GetMaxSpeed(), Is.EqualTo(25));
        second.Rigidbody.position = new Vector3(3, .5f, 0); Physics.SyncTransforms();
        for (int i = 0; i < 16; i++) yield return new WaitForFixedUpdate();
        Assert.That(first.GetMaxSpeed(), Is.EqualTo(20));
        Assert.That(second.GetMaxSpeed(), Is.EqualTo(25));
        zone.modesByLap = new[] { LapZoneMode.Boost, LapZoneMode.Spikes };
        zone.ApplyLap(2);
        for (int i = 0; i < 2; i++) yield return new WaitForFixedUpdate();
        Assert.That(first.IsTrapBraking, Is.False);
        Assert.That(second.IsTrapBraking, Is.False);
        first.Rigidbody.position = new Vector3(-3, .5f, -30); Physics.SyncTransforms();
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        first.Rigidbody.position = new Vector3(-3, .5f, 0); first.Rigidbody.linearVelocity = Vector3.forward * 10;
        Physics.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.That(first.IsTrapBraking, Is.True);
    }

    [UnityTest]
    public IEnumerator SpikeReleaseDoesNotUnlockCountdownOrWallBounce()
    {
        var kart = MakeKart(new Vector3(0, .5f, 0)); yield return Settle(kart);
        kart.GetComponent<TrapTestInput>().accelerate = true; kart.SetCanMove(false);
        kart.Rigidbody.linearVelocity = Vector3.forward;
        kart.ApplyTrapBrake(12);
        for (int i = 0; i < 12; i++) yield return new WaitForFixedUpdate();
        Assert.That(kart.IsTrapBraking, Is.False);
        Assert.That((bool)typeof(ArcadeKart).GetField("m_CanMove", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(kart), Is.False);
        Assert.That(Mathf.Abs(kart.Rigidbody.linearVelocity.z), Is.LessThan(.1f), "Wheel/suspension residual motion must not turn into engine acceleration.");
        kart.SetCanMove(true); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.That(kart.Rigidbody.linearVelocity.z, Is.GreaterThan(.02f));
    }

    [UnityTest]
    public IEnumerator InactiveZoneHasNoEffectAndDisablingCollidersAllowsReentry()
    {
        var zone = MakeZone(LapZoneMode.Inactive);
        var kart = MakeKart(new Vector3(0, .5f, -30)); yield return Settle(kart);
        kart.Rigidbody.position = new Vector3(0, .5f, 0); Physics.SyncTransforms();
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.That(kart.IsTrapBraking, Is.False); Assert.That(kart.GetMaxSpeed(), Is.EqualTo(20));
        // Disabling a collider need not send an exit callback. The zone must still clean up.
        var colliders = kart.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders) collider.enabled = false;
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        zone.modesByLap = new[] { LapZoneMode.Boost }; zone.ApplyLap(1);
        foreach (var collider in colliders) collider.enabled = true;
        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.That(kart.GetMaxSpeed(), Is.EqualTo(25));
    }

#if UNITY_EDITOR
    [UnityTest]
    public IEnumerator ExampleRaceUsesItsActualPlayerAndFinishTrigger()
    {
        // Load the actual authored scene and let its original HUD/objective register normally.
        foreach (var g in m_Objects) if (g != null) Object.DestroyImmediate(g);
        m_Objects.Clear();
        yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
            "Assets/DungeonTrack/DungeonCircuit_Trampas.unity", new LoadSceneParameters(LoadSceneMode.Additive));
        var scene = SceneManager.GetSceneByPath("Assets/DungeonTrack/DungeonCircuit_Trampas.unity");
        try
        {
            yield return null; yield return null; yield return null;
            var roots = scene.GetRootGameObjects();
            var controller = roots.SelectMany(g => g.GetComponentsInChildren<LapTrapController>(true)).First();
            var player = roots.SelectMany(g => g.GetComponentsInChildren<ArcadeKart>(true)).First();
            var finish = roots.SelectMany(g => g.GetComponentsInChildren<LapObject>(true)).First(lap => lap.finishLap);
            player.Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            player.Rigidbody.useGravity = false;
            player.Rigidbody.sleepThreshold = 0;
            for (int crossing = 0; crossing < 3; crossing++)
            {
                player.Rigidbody.position = finish.transform.position + new Vector3(-5, -1.85f, 0);
                player.Rigidbody.linearVelocity = Vector3.zero;
                player.Rigidbody.WakeUp();
                Physics.SyncTransforms();
                yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                player.Rigidbody.position = finish.transform.position + new Vector3(0, -1.85f, 0);
                player.Rigidbody.linearVelocity = Vector3.zero;
                player.Rigidbody.WakeUp();
                Physics.SyncTransforms();
                yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                Assert.That(finish.lapOverNextPass, Is.True, "The actual player's collider must enter the finish trigger.");
                Assert.That(controller.CurrentLap, Is.EqualTo(crossing + 1),
                    "Targets: " + controller.lapObjective.Pickups.Count + ", active: " + controller.lapObjective.NumberOfActivePickupsRemaining());
                Assert.That(controller.lapObjective.currentLap, Is.EqualTo(crossing));
                for (int i = 0; i < 3; i++)
                {
                    var expected = crossing == 2 || (crossing == 1 && i == 1) ? LapZoneMode.Spikes : LapZoneMode.Boost;
                    Assert.That(((BoostSpikeZone)controller.traps[i]).CurrentMode, Is.EqualTo(expected));
                }
            }
        }
        finally
        {
            if (scene.IsValid() && scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
        }
        yield return null;
    }
#endif
}

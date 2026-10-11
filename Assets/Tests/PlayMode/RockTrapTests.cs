using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KartGame.KartSystems;
using KartGame.Traps;
using KartGame.Track;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class RockTrapTests
{
    readonly List<GameObject> m_Objects = new List<GameObject>();
    UnityEngine.Random.State m_Random;
    Action m_PreviousLapDisplay;
    GameObject m_Ground;
    GameObject MakeObject(string name)
    {
        var g = new GameObject(name); m_Objects.Add(g); return g;
    }

    [SetUp]
    public void Setup()
    {
        m_Random = UnityEngine.Random.state;
        m_PreviousLapDisplay = TimeDisplay.OnUpdateLap; TimeDisplay.OnUpdateLap = delegate { };
        m_Ground = MakeObject("Rock test ground"); m_Ground.layer = 9;
        m_Ground.transform.position = Vector3.down * .1f;
        m_Ground.AddComponent<BoxCollider>().size = new Vector3(200, .2f, 200);
    }
    [TearDown]
    public void Teardown()
    {
        foreach (var g in m_Objects) if (g != null) Object.DestroyImmediate(g);
        m_Objects.Clear(); UnityEngine.Random.state = m_Random; TimeDisplay.OnUpdateLap = m_PreviousLapDisplay;
    }

    Transform Point(Vector3 p)
    {
        var point = MakeObject("Rock test point").transform; point.position = p; return point;
    }
    RockRoute Route(string name, Vector3 spawn, Vector3 impact, params Vector3[] points)
    {
        var route = MakeObject(name).AddComponent<RockRoute>();
        route.spawnPoint = Point(spawn); route.impactPoint = Point(impact);
        route.travelPoints = points.Select(Point).ToArray(); return route;
    }
    RockHazard Prototype()
    {
        var g = MakeObject("Rock prototype"); g.transform.position = Vector3.one * 500;
        var rock = g.AddComponent<RockHazard>();
        var visual = MakeObject("Rock visual"); visual.transform.SetParent(g.transform, false); rock.visual = visual.transform;
        return rock;
    }
    RockHazard Spawn(RockRoute route, float speed = 6, float stun = .3f)
    {
        var rock = Object.Instantiate(Prototype()); m_Objects.Add(rock.gameObject);
        Assert.That(rock.Initialize(route, speed, stun, .55f, 5), Is.True); return rock;
    }
    RockTrap Spawner(params RockRoute[] routes)
    {
        var trap = MakeObject("Rock spawner").AddComponent<RockTrap>();
        trap.rockPrefab = Prototype(); trap.routes = routes.ToList(); trap.waitForRaceStart = false;
        trap.initialDelay = .02f; trap.maximumActiveRocks = 20;
        trap.laps = new[] { new RockLapSettings { rollingSpeed = 1, spawnInterval = .08f } };
        trap.ApplyLap(1); return trap;
    }
    ArcadeKart Kart(Vector3 position)
    {
        var g = MakeObject("Rock test kart"); g.SetActive(false); g.transform.position = position;
        var body = g.AddComponent<Rigidbody>(); body.mass = 150; body.constraints = RigidbodyConstraints.FreezeRotation;
        g.AddComponent<TrapTestInput>();
        g.AddComponent<BoxCollider>().size = new Vector3(1, .3f, 1.4f);
        var kart = g.AddComponent<ArcadeKart>(); kart.CenterOfMass = g.transform;
        kart.JumpVFX = MakeObject("Rock test landing VFX");
        kart.baseStats = new ArcadeKart.Stats { TopSpeed = 20, Acceleration = 3, AccelerationCurve = .2f,
            ReverseSpeed = 5, ReverseAcceleration = 3, Braking = 10, Grip = .95f, Steer = 5 };
        var wheels = new List<WheelCollider>();
        foreach (float x in new[] { -.4f, .4f }) foreach (float z in new[] { -.55f, .55f })
        {
            var wheel = new GameObject("Rock test wheel"); wheel.transform.SetParent(g.transform, false);
            wheel.transform.localPosition = new Vector3(x, -.1f, z); wheel.layer = 2;
            var wc = wheel.AddComponent<WheelCollider>(); wc.radius = .3f; wheels.Add(wc);
        }
        kart.FrontLeftWheel = wheels[0]; kart.FrontRightWheel = wheels[1];
        kart.RearLeftWheel = wheels[2]; kart.RearRightWheel = wheels[3]; g.SetActive(true); return kart;
    }
    IEnumerator Settle()
    {
        for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
    }

    [UnityTest]
    public IEnumerator ControlledFallUsesEditedPointsAndDisappearsAtTheEnd()
    {
        var route = Route("A", new Vector3(0, 10, 0), new Vector3(0, 1, 0), new Vector3(4, 1, 0), new Vector3(4, 1, 4));
        route.fallDuration = .12f;
        // Move the empty object before spawning; the rock must use its edited position.
        route.spawnPoint.position = new Vector3(0, 12, 0);
        var rock = Spawn(route, 10); var body = rock.GetComponent<Rigidbody>();
        Assert.That(body.isKinematic, Is.True); Assert.That(body.useGravity, Is.False);
        Assert.That(rock.GetComponent<SphereCollider>().isTrigger, Is.True);
        yield return new WaitForFixedUpdate();
        Assert.That(body.position.y, Is.LessThan(12).And.GreaterThan(1));
        for (int i = 0; i < 9; i++) yield return new WaitForFixedUpdate();
        Assert.That(rock.IsRolling, Is.True);
        Assert.That(body.position.y, Is.EqualTo(1).Within(.001f));
        Assert.That(body.position.x, Is.GreaterThan(0));
        for (int i = 0; i < 55; i++) yield return new WaitForFixedUpdate();
        yield return null; Assert.That(rock == null, Is.True);
    }

    [Test]
    public void SmoothPathVisitsEveryAnchorAndLastLapIsReused()
    {
        var route = Route("Smooth", Vector3.up * 10, Vector3.zero, Vector3.right * 3, new Vector3(3, 2, 4));
        route.pathShape = RockPathShape.Smooth;
        Assert.That(route.TryGetPath(out var path), Is.True);
        Assert.That(path[0], Is.EqualTo(Vector3.zero)); Assert.That(path[path.Length - 1], Is.EqualTo(new Vector3(3, 2, 4)));
        Assert.That(path.Any(p => (p - Vector3.right * 3).sqrMagnitude < .0001f), Is.True);
        var trap = Spawner(route); trap.laps = new[] { new RockLapSettings { rollingSpeed = 3 }, new RockLapSettings { rollingSpeed = 9 } };
        Assert.That(trap.GetSettingsForLap(1).rollingSpeed, Is.EqualTo(3));
        Assert.That(trap.GetSettingsForLap(3).rollingSpeed, Is.EqualTo(9));
        route.travelPoints[0] = null; Assert.That(route.TryGetPath(out _), Is.False);
    }

    [UnityTest]
    public IEnumerator ShuffledBatchesUseEveryRouteAndRespectEmissionSpacing()
    {
        var routes = Enumerable.Range(0, 3).Select(i => Route("Lane " + i, new Vector3(i * 5, 10, 0),
            new Vector3(i * 5, 1, 0), new Vector3(i * 5, 1, 100))).ToArray();
        UnityEngine.Random.InitState(457);
        var trap = Spawner(routes); var emitted = new List<RockRoute>(); var times = new List<float>();
        trap.RockSpawned += route => { emitted.Add(route); times.Add(Time.fixedTime); };
        for (int i = 0; i < 45 && emitted.Count < 6; i++) yield return new WaitForFixedUpdate();
        Assert.That(emitted.Count, Is.EqualTo(6));
        Assert.That(emitted.Take(3).Distinct().Count(), Is.EqualTo(3));
        Assert.That(emitted.Skip(3).Take(3).Distinct().Count(), Is.EqualTo(3));
        Assert.That(emitted[3], Is.Not.SameAs(emitted[2]));
        for (int i = 1; i < times.Count; i++) Assert.That(times[i] - times[i - 1], Is.GreaterThanOrEqualTo(.079f));
    }

    [UnityTest]
    public IEnumerator ManualSequenceAndLapFilteringApplyOnlyToNewRocks()
    {
        var a = Route("A", new Vector3(0, 10, 0), new Vector3(0, 1, 0), new Vector3(0, 1, 100));
        var b = Route("B", new Vector3(5, 10, 0), new Vector3(5, 1, 0), new Vector3(5, 1, 100));
        var trap = Spawner(a, b); trap.spawnOrder = RockSpawnOrder.Sequence; trap.sequence = new List<RockRoute> { b, a };
        trap.laps = new[] { new RockLapSettings { rollingSpeed = 2, spawnInterval = .08f },
            new RockLapSettings { rollingSpeed = 8, spawnInterval = .08f, enabledRoutes = new[] { a } },
            new RockLapSettings { active = false } };
        var emitted = new List<RockRoute>(); trap.RockSpawned += emitted.Add;
        for (int i = 0; i < 12 && emitted.Count < 2; i++) yield return new WaitForFixedUpdate();
        Assert.That(emitted[0], Is.SameAs(b)); Assert.That(emitted[1], Is.SameAs(a));
        var oldRock = Object.FindObjectsByType<RockHazard>(FindObjectsSortMode.None).First(r => r != trap.rockPrefab);
        trap.ApplyLap(2);
        for (int i = 0; i < 12 && emitted.Count < 3; i++) yield return new WaitForFixedUpdate();
        Assert.That(emitted[2], Is.SameAs(a));
        Assert.That(oldRock.RollingSpeed, Is.EqualTo(2));
        Assert.That(Object.FindObjectsByType<RockHazard>(FindObjectsSortMode.None).Any(r => r.RollingSpeed == 8), Is.True);
        trap.ApplyLap(3); int count = emitted.Count;
        for (int i = 0; i < 12; i++) yield return new WaitForFixedUpdate();
        Assert.That(emitted.Count, Is.EqualTo(count));
    }

    [UnityTest]
    public IEnumerator RaceTimerPreventsCountdownRocksAndCleansUpAfterRace()
    {
        var route = Route("A", Vector3.up * 10, Vector3.up, Vector3.forward * 100 + Vector3.up);
        var timer = MakeObject("Rock race timer").AddComponent<TimeManager>();
        var trap = Spawner(route); trap.waitForRaceStart = true; trap.raceTimer = timer;
        for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
        Assert.That(trap.SpawnedCount, Is.Zero);
        timer.StartRace();
        for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
        Assert.That(trap.SpawnedCount, Is.GreaterThan(0));
        timer.StopRace(); int count = trap.SpawnedCount;
        yield return new WaitForFixedUpdate(); yield return null;
        Assert.That(trap.ActiveRockCount, Is.Zero);
        for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
        Assert.That(trap.SpawnedCount, Is.EqualTo(count));
    }

    [UnityTest]
    public IEnumerator RockStunHoldsOnSlopeAndRestoresControlsAndConstraints()
    {
        m_Ground.transform.rotation = Quaternion.Euler(12, 0, 0); Physics.SyncTransforms();
        var kart = Kart(new Vector3(0, .55f, 0)); yield return Settle();
        var original = kart.Rigidbody.constraints;
        kart.GetComponent<TrapTestInput>().accelerate = true;
        kart.Rigidbody.linearVelocity = Vector3.forward * 8;
        kart.ApplyTrapStun(.3f); var position = kart.Rigidbody.position;
        for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
        Assert.That(kart.IsTrapStunned, Is.True);
        Assert.That(kart.Rigidbody.position, Is.EqualTo(position));
        Assert.That(kart.Rigidbody.linearVelocity, Is.EqualTo(Vector3.zero));
        Assert.That(kart.Input.Accelerate, Is.False);
        for (int i = 0; i < 12; i++) yield return new WaitForFixedUpdate();
        Assert.That(kart.IsTrapStunned, Is.False); Assert.That(kart.Rigidbody.constraints, Is.EqualTo(original));
        Assert.That(kart.Input.Accelerate, Is.True);
        Assert.That(kart.Rigidbody.linearVelocity.magnitude, Is.GreaterThan(.01f));
    }

    [UnityTest]
    public IEnumerator StunDoesNotReleaseCountdownOrBounceAndDisableRestoresConstraints()
    {
        var kart = Kart(new Vector3(0, .5f, 0)); yield return Settle();
        var original = kart.Rigidbody.constraints; kart.SetCanMove(false);
        kart.GetComponent<TrapTestInput>().accelerate = true; kart.ApplyTrapStun(.1f);
        for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
        Assert.That(kart.IsTrapStunned, Is.False);
        Assert.That((bool)typeof(ArcadeKart).GetField("m_CanMove", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(kart), Is.False);
        Assert.That(Mathf.Abs(kart.Rigidbody.linearVelocity.z), Is.LessThan(.1f));
        kart.ApplyTrapStun(1); kart.enabled = false;
        Assert.That(kart.IsTrapStunned, Is.False); Assert.That(kart.Rigidbody.constraints, Is.EqualTo(original));
    }

    [UnityTest]
    public IEnumerator OneRockHitsCompoundKartOnceAndFastSweepsCannotSkipIt()
    {
        var kart = Kart(new Vector3(0, .5f, 0)); yield return Settle();
        var extra = MakeObject("Second kart collider"); extra.transform.SetParent(kart.transform, false); extra.AddComponent<SphereCollider>().radius = .5f;
        var route = Route("Slow", new Vector3(0, 1, 0), new Vector3(0, .7f, 0), new Vector3(0, .7f, 100));
        route.fallDuration = .01f; Spawn(route, .1f, .3f);
        for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
        Assert.That(kart.IsTrapStunned, Is.True);
        Assert.That(kart.TrapStunSecondsRemaining, Is.LessThan(.2f), "Multiple colliders and persistent overlap must not renew one rock's hit.");
        for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
        Assert.That(kart.IsTrapStunned, Is.False, "The kart can recover even while still touching the same rock.");
        var fast = Route("Fast", new Vector3(-8, 1, 0), new Vector3(-8, .7f, 0), new Vector3(100, .7f, 0));
        fast.fallDuration = .01f; Spawn(fast, 1000, .5f);
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.That(kart.IsTrapStunned, Is.True, "The scripted sweep must detect a rock crossing the kart between ticks.");
    }

    [UnityTest]
    public IEnumerator DamageFeedbackRestoresSharedMaterialAndExistingOverrides()
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube); m_Objects.Add(g);
        var renderer = g.GetComponent<Renderer>();
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.SetColor("_BaseColor", Color.red);
        renderer.sharedMaterial = material;
        var original = new MaterialPropertyBlock(); original.SetColor("_BaseColor", Color.blue); original.SetFloat("_Smoothness", .23f);
        renderer.SetPropertyBlock(original, 0);
        var feedback = g.AddComponent<KartDamageFeedback>();
        try
        {
            feedback.Show(.1f, .5f, 0);
            var actual = new MaterialPropertyBlock(); renderer.GetPropertyBlock(actual, 0);
            Assert.That(actual.GetColor("_BaseColor").b, Is.EqualTo(.5f).Within(.001f));
            Assert.That(actual.GetFloat("_Smoothness"), Is.EqualTo(.23f));
            Assert.That(material.GetColor("_BaseColor"), Is.EqualTo(Color.red));
            for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
            renderer.GetPropertyBlock(actual, 0);
            Assert.That(feedback.IsShowingDamage, Is.False); Assert.That(actual.GetColor("_BaseColor"), Is.EqualTo(Color.blue));
            feedback.Show(2, .5f, 0); feedback.enabled = false; renderer.GetPropertyBlock(actual, 0);
            Assert.That(actual.GetColor("_BaseColor"), Is.EqualTo(Color.blue));
            renderer.SetPropertyBlock(null, 0);
            original.SetColor("_BaseColor", Color.green); original.SetFloat("_Smoothness", .65f);
            renderer.SetPropertyBlock(original); feedback.enabled = true; feedback.Show(.1f, .5f, 0);
            renderer.GetPropertyBlock(actual, 0);
            Assert.That(actual.GetColor("_BaseColor").g, Is.EqualTo(.5f).Within(.001f));
            Assert.That(actual.GetFloat("_Smoothness"), Is.EqualTo(.65f));
            feedback.enabled = false; renderer.GetPropertyBlock(actual, 0);
            Assert.That(actual.isEmpty, Is.True, "An originally inherited block must remain inherited after recovery.");
            renderer.GetPropertyBlock(actual); Assert.That(actual.GetColor("_BaseColor"), Is.EqualTo(Color.green));
        }
        finally { Object.DestroyImmediate(material); }
    }

#if UNITY_EDITOR
    [UnityTest]
    public IEnumerator ActualRockSceneUsesRealMetaLapControllerAndDamagesActualPlayer()
    {
        foreach (var g in m_Objects) if (g != null) Object.DestroyImmediate(g); m_Objects.Clear();
        yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
            "Assets/DungeonTrack/DungeonCircuit_Rocas.unity", new LoadSceneParameters(LoadSceneMode.Additive));
        var scene = SceneManager.GetSceneByPath("Assets/DungeonTrack/DungeonCircuit_Rocas.unity");
        try
        {
            yield return null; yield return null; yield return null;
            var roots = scene.GetRootGameObjects();
            var controller = roots.SelectMany(g => g.GetComponentsInChildren<LapTrapController>(true)).First();
            var rocks = roots.SelectMany(g => g.GetComponentsInChildren<RockTrap>(true)).First();
            var player = roots.SelectMany(g => g.GetComponentsInChildren<ArcadeKart>(true)).First();
            var finish = roots.SelectMany(g => g.GetComponentsInChildren<LapObject>(true)).First(l => l.finishLap);
            player.Rigidbody.constraints = RigidbodyConstraints.FreezeRotation; player.Rigidbody.useGravity = false; player.Rigidbody.sleepThreshold = 0;
            for (int crossing = 0; crossing < 3; crossing++)
            {
                player.Rigidbody.position = finish.transform.position + new Vector3(-5, -1.85f, 0);
                player.Rigidbody.linearVelocity = Vector3.zero; player.Rigidbody.WakeUp(); Physics.SyncTransforms();
                yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                player.Rigidbody.position = finish.transform.position + new Vector3(0, -1.85f, 0);
                player.Rigidbody.linearVelocity = Vector3.zero; player.Rigidbody.WakeUp(); Physics.SyncTransforms();
                yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                Assert.That(controller.CurrentLap, Is.EqualTo(crossing + 1));
                Assert.That(rocks.CurrentLap, Is.EqualTo(crossing + 1));
                Assert.That(rocks.GetSettingsForLap(rocks.CurrentLap).rollingSpeed, Is.EqualTo(new[] { 6f, 9f, 12f }[crossing]));
            }
            var route = Route("Actual player hit", player.transform.position + Vector3.up * 2, player.transform.position, player.transform.position + Vector3.right * 100);
            route.fallDuration = .01f;
            var rock = Object.Instantiate(rocks.rockPrefab); SceneManager.MoveGameObjectToScene(rock.gameObject, scene);
            try
            {
                Assert.That(rock.Initialize(route, .1f, .3f, .55f, 5), Is.True);
                yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                Assert.That(player.IsTrapStunned, Is.True);
                Assert.That(player.Rigidbody.linearVelocity, Is.EqualTo(Vector3.zero));
                Assert.That(player.GetComponent<KartDamageFeedback>().IsShowingDamage, Is.True);
                Assert.That(player.GetComponentsInChildren<Renderer>().Any(r => r.HasPropertyBlock()), Is.True);
            }
            finally { if (rock != null) Object.DestroyImmediate(rock.gameObject); }
        }
        finally { if (scene.IsValid() && scene.isLoaded) SceneManager.UnloadSceneAsync(scene); }
        yield return null;
    }
#endif
}

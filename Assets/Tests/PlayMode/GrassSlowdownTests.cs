using System.Collections;
using System.Collections.Generic;
using KartGame.KartSystems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class GrassSlowdownTests
{
    readonly List<GameObject> m_Objects = new List<GameObject>();
    GameObject Make(string name) { var g = new GameObject(name); m_Objects.Add(g); return g; }
    [TearDown] public void Teardown()
    {
        foreach (var g in m_Objects) if (g != null) Object.DestroyImmediate(g);
        m_Objects.Clear();
    }
    GrassSlowZone Ground()
    {
        var g = Make("Grass test ground"); g.layer = 9; g.transform.position = Vector3.down * .1f;
        g.AddComponent<BoxCollider>().size = new Vector3(200, .2f, 200);
        return g.AddComponent<GrassSlowZone>();
    }
    ArcadeKart Kart()
    {
        var g = Make("Grass test kart"); g.SetActive(false); g.transform.position = Vector3.up;
        var body = g.AddComponent<Rigidbody>(); body.mass = 150; body.constraints = RigidbodyConstraints.FreezeRotation;
        g.AddComponent<TrapTestInput>(); g.AddComponent<BoxCollider>().size = new Vector3(1, .3f, 1.4f);
        var kart = g.AddComponent<ArcadeKart>(); kart.CenterOfMass = g.transform;
        kart.JumpVFX = Make("Grass test landing VFX");
        kart.baseStats = new ArcadeKart.Stats { TopSpeed = 20, Acceleration = 3, AccelerationCurve = .2f,
            ReverseSpeed = 5, ReverseAcceleration = 3, Braking = 10, Grip = .95f, Steer = 5 };
        var wheels = new List<WheelCollider>();
        foreach (float x in new[] { -.4f, .4f }) foreach (float z in new[] { -.55f, .55f })
        {
            var wheel = new GameObject("Grass test wheel"); wheel.transform.SetParent(g.transform, false);
            wheel.transform.localPosition = new Vector3(x, -.1f, z); wheel.layer = 2;
            var wc = wheel.AddComponent<WheelCollider>(); wc.radius = .3f; wheels.Add(wc);
        }
        kart.FrontLeftWheel = wheels[0]; kart.FrontRightWheel = wheels[1];
        kart.RearLeftWheel = wheels[2]; kart.RearRightWheel = wheels[3];
        g.AddComponent<KartGrassSlowdown>(); g.SetActive(true); return kart;
    }
    IEnumerator Steps(int count = 40) { for (int i = 0; i < count; i++) yield return new WaitForFixedUpdate(); }

    [UnityTest] public IEnumerator GrassBrakesOverspeedSmoothlyEvenWithTheThrottleHeld()
    {
        var surface = Ground(); var kart = Kart(); yield return Steps();
        Assert.That(kart.GetComponent<KartGrassSlowdown>().CurrentSurface, Is.SameAs(surface));
        Assert.That(kart.GetMaxSpeed(), Is.EqualTo(16).Within(.01f));
        kart.GetComponent<TrapTestInput>().accelerate = true;
        kart.Rigidbody.linearVelocity = Vector3.forward * 22;
        yield return Steps(1);
        float early = Vector3.ProjectOnPlane(kart.Rigidbody.linearVelocity, Vector3.up).magnitude;
        Assert.That(early, Is.LessThan(22).And.GreaterThan(20));
        yield return Steps(60);
        Assert.That(Vector3.ProjectOnPlane(kart.Rigidbody.linearVelocity, Vector3.up).magnitude, Is.EqualTo(16).Within(.4f));
    }
    [UnityTest] public IEnumerator RoadAboveGrassIsNotPenalizedAndDrivingBackToRoadRestoresSpeed()
    {
        Ground(); var road = Make("Asphalt above grass"); road.layer = 11;
        road.transform.position = new Vector3(10, .35f, 0); road.AddComponent<BoxCollider>().size = new Vector3(8, .3f, 200);
        var kart = Kart(); yield return Steps();
        Assert.That(kart.GetMaxSpeed(), Is.EqualTo(16).Within(.01f));
        kart.Rigidbody.position = new Vector3(10, 1.5f, 0); kart.Rigidbody.linearVelocity = Vector3.zero;
        yield return Steps(70);
        Assert.That(kart.GetComponent<KartGrassSlowdown>().CurrentSurface, Is.Null);
        Assert.That(kart.GetMaxSpeed(), Is.EqualTo(20).Within(.01f));
    }
    [UnityTest] public IEnumerator BoostsRemainAdditiveAndDisablingGrassRestoresTheNormalStats()
    {
        var surface = Ground(); var kart = Kart(); yield return Steps();
        kart.AddPowerup(new ArcadeKart.StatPowerup { modifiers = new ArcadeKart.Stats { TopSpeed = 5 }, MaxTime = 10 });
        yield return Steps(5);
        Assert.That(kart.GetMaxSpeed(), Is.EqualTo(21).Within(.01f));
        surface.enabled = false; yield return Steps(5);
        Assert.That(kart.GetMaxSpeed(), Is.EqualTo(25).Within(.01f));
        Assert.That(kart.GetComponent<KartGrassSlowdown>().CurrentSurface, Is.Null);
    }
}

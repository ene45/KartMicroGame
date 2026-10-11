using System.Collections;
using System.Collections.Generic;
using KartGame.KartSystems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class TeleportTestCollisionCounter : MonoBehaviour
{
    public int count;
    void OnCollisionEnter(Collision collision) => count++;
}

public class KartTeleportTests
{
    readonly List<GameObject> m_Objects = new List<GameObject>();
    GameObject Make(string name) { var g = new GameObject(name); m_Objects.Add(g); return g; }
    [TearDown] public void Teardown()
    {
        foreach (var g in m_Objects) if (g != null) Object.DestroyImmediate(g);
        m_Objects.Clear();
    }
    KartTeleportSurface Surface(bool trigger = false)
    {
        var g = Make("Return floor"); g.layer = 9;
        g.transform.position = trigger ? Vector3.up : Vector3.down * .1f;
        var collider = g.AddComponent<BoxCollider>();
        collider.size = trigger ? new Vector3(6, 4, 6) : new Vector3(20, .2f, 20);
        collider.isTrigger = trigger;
        g.AddComponent<TeleportTestCollisionCounter>();
        return g.AddComponent<KartTeleportSurface>();
    }
    Transform Destination()
    {
        var road = Make("Destination road"); road.layer = 11;
        road.transform.position = new Vector3(30, -.1f, 0);
        road.AddComponent<BoxCollider>().size = new Vector3(8, .2f, 8);
        var point = Make("Editable destination").transform;
        point.SetPositionAndRotation(new Vector3(30, .65f, 0), Quaternion.Euler(0, 90, 0));
        return point;
    }
    ArcadeKart Kart(Vector3 position)
    {
        var g = Make("Teleport test kart"); g.SetActive(false); g.transform.position = position;
        var body = g.AddComponent<Rigidbody>(); body.mass = 150; body.constraints = RigidbodyConstraints.FreezeRotation;
        g.AddComponent<TrapTestInput>(); g.AddComponent<BoxCollider>().size = new Vector3(1, .2f, 1.4f);
        var kart = g.AddComponent<ArcadeKart>(); kart.CenterOfMass = g.transform;
        kart.JumpVFX = Make("Teleport test landing VFX");
        kart.baseStats = new ArcadeKart.Stats { TopSpeed = 15, Acceleration = 12, AccelerationCurve = .5f,
            ReverseSpeed = 5, ReverseAcceleration = 5, Braking = 10, CoastingDrag = 2, Grip = .95f, Steer = 3, AddedGravity = 1 };
        var wheels = new List<WheelCollider>();
        foreach (float x in new[] { -.4f, .4f }) foreach (float z in new[] { -.55f, .55f })
        {
            var wheel = new GameObject("Teleport test wheel"); wheel.transform.SetParent(g.transform, false);
            wheel.transform.localPosition = new Vector3(x, -.1f, z); wheel.layer = 2;
            var wc = wheel.AddComponent<WheelCollider>(); wc.radius = .3f; wheels.Add(wc);
        }
        kart.FrontLeftWheel = wheels[0]; kart.FrontRightWheel = wheels[1];
        kart.RearLeftWheel = wheels[2]; kart.RearRightWheel = wheels[3];
        g.SetActive(true); return kart;
    }
    IEnumerator Steps(int count) { for (int i = 0; i < count; i++) yield return new WaitForFixedUpdate(); }

    [UnityTest] public IEnumerator WheelContactReturnsTheKartEvenWithoutBodyCollision()
    {
        var surface = Surface(); var kart = Kart(Vector3.up * .8f);
        surface.player = kart; surface.destination = Destination();
        yield return Steps(45);
        Assert.That(kart.transform.position.x, Is.EqualTo(30).Within(.1f));
        Assert.That(kart.transform.position.y, Is.GreaterThan(0));
        Assert.That(Vector3.Dot(kart.transform.forward, Vector3.right), Is.GreaterThan(.99f));
        Assert.That(surface.GetComponent<TeleportTestCollisionCounter>().count, Is.Zero);
        Assert.That(kart.GroundPercent, Is.GreaterThan(0));
        kart.GetComponent<TrapTestInput>().accelerate = true;
        yield return Steps(25);
        Assert.That(kart.transform.position.x, Is.GreaterThan(30.1f));
    }

    [UnityTest] public IEnumerator TriggerContactTeleportsOnlyTheAssignedPlayer()
    {
        var surface = Surface(true); var player = Kart(Vector3.up * .8f);
        var other = Kart(new Vector3(2, .8f, 0));
        surface.player = player; surface.destination = Destination();
        var rock = Make("Non-player rigidbody"); rock.transform.position = new Vector3(-2, .8f, 0);
        rock.AddComponent<Rigidbody>(); rock.AddComponent<SphereCollider>();
        yield return Steps(20);
        Assert.That(player.transform.position.x, Is.EqualTo(30).Within(.1f));
        Assert.That(other.transform.position.x, Is.EqualTo(2).Within(.1f));
        Assert.That(rock.transform.position.x, Is.EqualTo(-2).Within(.1f));
    }

    [UnityTest] public IEnumerator TeleportClearsMotionAndTrapLocksButPreservesCountdownLock()
    {
        var surface = Surface(); var kart = Kart(Vector3.up * .8f);
        surface.player = kart; surface.destination = Destination(); surface.cooldown = .1f;
        kart.SetCanMove(false); kart.GetComponent<TrapTestInput>().accelerate = true;
        kart.SetDrift(true); kart.ApplyTrapBrake(30);
        kart.Rigidbody.linearVelocity = new Vector3(9, -4, 3); kart.Rigidbody.angularVelocity = Vector3.one * 3;
        Assert.That(surface.TryTeleport(kart), Is.True);
        Assert.That(kart.Rigidbody.position, Is.EqualTo(surface.destination.position));
        Assert.That(Quaternion.Angle(kart.Rigidbody.rotation, surface.destination.rotation), Is.LessThan(.01f));
        Assert.That(kart.Rigidbody.linearVelocity, Is.EqualTo(Vector3.zero));
        Assert.That(kart.Rigidbody.angularVelocity, Is.EqualTo(Vector3.zero));
        Assert.That(kart.IsTrapBraking || kart.IsDrifting, Is.False);
        yield return Steps(20);
        Assert.That(kart.transform.position.x, Is.EqualTo(30).Within(.05f));
        kart.ApplyTrapStun(3);
        Assert.That(kart.Rigidbody.constraints, Is.EqualTo(RigidbodyConstraints.FreezeAll));
        Assert.That(surface.TryTeleport(kart), Is.True);
        Assert.That(kart.IsTrapStunned, Is.False);
        Assert.That(kart.Rigidbody.constraints, Is.EqualTo(RigidbodyConstraints.FreezeRotation));
        yield return Steps(15);
        Assert.That(kart.transform.position.x, Is.EqualTo(30).Within(.05f));
    }

    [UnityTest] public IEnumerator MissingDestinationAndCooldownAreSafeAndMovedPointIsUsed()
    {
        var surface = Surface(); var kart = Kart(Vector3.up * .8f); surface.player = kart;
        Assert.That(surface.TryTeleport(kart), Is.False);
        surface.destination = Destination(); surface.cooldown = .1f;
        Assert.That(surface.TryTeleport(kart), Is.True);
        surface.destination.SetPositionAndRotation(new Vector3(40, .65f, 0), Quaternion.Euler(0, 270, 0));
        Assert.That(surface.TryTeleport(kart), Is.False);
        Assert.That(kart.transform.position.x, Is.EqualTo(30));
        yield return Steps(10);
        Assert.That(surface.TryTeleport(kart), Is.True);
        Assert.That(kart.Rigidbody.position, Is.EqualTo(surface.destination.position));
        Assert.That(Quaternion.Angle(kart.Rigidbody.rotation, surface.destination.rotation), Is.LessThan(.01f));
    }
}

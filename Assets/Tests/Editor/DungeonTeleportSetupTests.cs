using System.Linq;
using DungeonTrack.Editor;
using KartGame.KartSystems;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonTeleportSetupTests
{
    [Test] public void ConfigurationPreservesGeometryAndEditableDestinationOnRepeatedUse()
    {
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.OpenScene("Assets/DungeonTrack/DungeonCircuit_AnilloLava.unity", OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            var before = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var positions = before.Select(t => t.localPosition).ToArray();
            var floor = before.Single(t => t.name == "Sala_Suelo_Libre_Central");
            var collider = floor.GetComponent<MeshCollider>(); var mesh = collider.sharedMesh;
            var surface = DungeonTeleportSetup.Configure(scene);
            Assert.That(surface.player, Is.Not.Null);
            Assert.That(surface.destination.GetComponents<Component>().Length, Is.EqualTo(1));
            Assert.That(surface.destination.name, Is.EqualTo("Punto_Reaparicion_Entrada"));
            Assert.That(surface.destination.position.x, Is.EqualTo(-35).Within(.1f));
            Assert.That(Vector3.Dot(surface.destination.forward, Vector3.right), Is.GreaterThan(.99f));
            var moved = surface.destination.position + new Vector3(1, 2, 3);
            var rotation = Quaternion.Euler(0, 130, 0);
            surface.destination.SetPositionAndRotation(moved, rotation); surface.cooldown = 2;
            Assert.That(DungeonTeleportSetup.Configure(scene), Is.SameAs(surface));
            Assert.That(surface.destination.position, Is.EqualTo(moved));
            Assert.That(Quaternion.Angle(surface.destination.rotation, rotation), Is.LessThan(.01f));
            Assert.That(surface.cooldown, Is.EqualTo(2));
            Assert.That(collider.isTrigger, Is.False); Assert.That(collider.sharedMesh, Is.SameAs(mesh));
            CollectionAssert.AreEqual(positions, before.Select(t => t.localPosition).ToArray());
            Assert.That(floor.GetComponents<KartTeleportSurface>().Length, Is.EqualTo(1));
            Assert.That(scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Count(t => t.name == "Punto_Reaparicion_Entrada"), Is.EqualTo(1));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }
}

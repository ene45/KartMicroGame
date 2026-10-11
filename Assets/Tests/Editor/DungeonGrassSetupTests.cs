using System.Linq;
using DungeonTrack.Editor;
using KartGame.KartSystems;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonGrassSetupTests
{
    [Test] public void ExistingTrackGeometryAndPhysicalGrassColliderArePreserved()
    {
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.OpenScene("Assets/DungeonTrack/DungeonCircuit_AnilloLava.unity", OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            var transforms = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var positions = transforms.Select(t => t.localPosition).ToArray();
            var grass = transforms.Single(t => t.name == "Pasto_Suelo_Continuo_Exterior");
            var collider = grass.GetComponent<MeshCollider>(); var mesh = collider.sharedMesh;
            DungeonGrassSetup.Configure(scene); DungeonGrassSetup.Configure(scene);
            Assert.That(grass.GetComponents<GrassSlowZone>().Length, Is.EqualTo(1));
            Assert.That(grass.GetComponent<GrassSlowZone>().speedMultiplier, Is.EqualTo(.8f));
            Assert.That(collider.isTrigger, Is.False); Assert.That(collider.sharedMesh, Is.SameAs(mesh));
            CollectionAssert.AreEqual(positions, transforms.Select(t => t.localPosition).ToArray());
            Assert.That(scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Count(), Is.EqualTo(transforms.Length));
            foreach (var kart in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ArcadeKart>(true)))
                Assert.That(kart.GetComponents<KartGrassSlowdown>().Length, Is.EqualTo(1));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }
}

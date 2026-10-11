using System.Linq;
using DungeonTrack.Editor;
using KartGame.Traps;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

public class DungeonTrapSceneTests
{
    [Test]
    public void InstallationUsesEditedRoadHeightAndSupportsUndo()
    {
        var scene = EditorSceneManager.OpenScene(DungeonTrackBuilder.ScenePath, OpenSceneMode.Additive);
        try
        {
            var road = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ProBuilderMesh>(true))
                .First(pb => pb.name == "Z2_Curva_Drift_Peralte_Tramo_01");
            road.transform.position += Vector3.up * 5;
            Physics.SyncTransforms();
            int group;
            Undo.IncrementCurrentGroup(); group = Undo.GetCurrentGroup();
            var controller = DungeonTrapSetup.Install(scene);
            Undo.CollapseUndoOperations(group);
            Assert.That(controller.traps.Count, Is.EqualTo(3));
            Assert.That(controller.traps[0].transform.position.y, Is.GreaterThan(4));
            Undo.PerformUndo();
            Assert.That(controller == null, Is.True);
            Assert.That(road.transform.position.y, Is.EqualTo(5), "Installing or undoing traps must not alter the edited road.");
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }

    [Test]
    public void PrefabKeepsOneTriggerAndPersistentEditableVisualMeshes()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DungeonTrapSetup.PrefabPath);
        Assert.That(prefab, Is.Not.Null);
        var zone = prefab.GetComponent<BoostSpikeZone>(); Assert.That(zone, Is.Not.Null);
        var colliders = prefab.GetComponentsInChildren<Collider>(true);
        Assert.That(colliders.Length, Is.EqualTo(1));
        Assert.That(colliders[0], Is.TypeOf<BoxCollider>());
        Assert.That(colliders[0].isTrigger, Is.True);
        Assert.That(((BoxCollider)colliders[0]).size, Is.EqualTo(new Vector3(6, 2, 8)));
        foreach (var pb in prefab.GetComponentsInChildren<ProBuilderMesh>(true))
        {
            var mesh = pb.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh, Is.Not.Null, pb.name);
            Assert.That(AssetDatabase.Contains(mesh), Is.True, pb.name);
            Assert.That(pb.vertexCount, Is.GreaterThan(0));
        }
    }

    [Test]
    public void ChangingVisualModesKeepsExactlyTheSameTrigger()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DungeonTrapSetup.PrefabPath);
        var instance = Object.Instantiate(prefab);
        try
        {
            var zone = instance.GetComponent<BoostSpikeZone>();
            var box = instance.GetComponent<BoxCollider>(); var size = box.size;
            zone.modesByLap = new[] { LapZoneMode.Boost, LapZoneMode.Spikes, LapZoneMode.Inactive };
            for (int lap = 1; lap <= 3; lap++)
            {
                zone.ApplyLap(lap);
                Assert.That(instance.GetComponent<BoxCollider>(), Is.SameAs(box));
                Assert.That(box.size, Is.EqualTo(size)); Assert.That(box.enabled && box.isTrigger, Is.True);
                Assert.That(zone.boostVisual.activeSelf, Is.EqualTo(lap == 1));
                Assert.That(zone.spikesVisual.activeSelf, Is.EqualTo(lap == 2));
                Assert.That(zone.inactiveVisual.activeSelf, Is.EqualTo(lap == 3));
            }
        }
        finally { Object.DestroyImmediate(instance); }
    }

    [Test]
    public void ExampleScenePreservesRoadGeometryAndHasThreeConfiguredZones()
    {
        var source = EditorSceneManager.OpenScene(DungeonTrackBuilder.ScenePath, OpenSceneMode.Additive);
        var example = EditorSceneManager.OpenScene(DungeonTrapSetup.ExampleScenePath, OpenSceneMode.Additive);
        try
        {
            var controllers = example.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LapTrapController>(true)).ToArray();
            Assert.That(controllers.Length, Is.EqualTo(1));
            var controller = controllers[0]; Assert.That(controller.lapObjective, Is.Not.Null);
            Assert.That(controller.lapObjective.lapsToComplete, Is.EqualTo(3));
            Assert.That(controller.traps.Count, Is.EqualTo(3));
            for (int i = 0; i < 3; i++)
            {
                var zone = controller.traps[i] as BoostSpikeZone; Assert.That(zone, Is.Not.Null);
                Assert.That(zone.GetModeForLap(1), Is.EqualTo(LapZoneMode.Boost));
                Assert.That(zone.GetModeForLap(2), Is.EqualTo(i == 1 ? LapZoneMode.Spikes : LapZoneMode.Boost));
                Assert.That(zone.GetModeForLap(3), Is.EqualTo(LapZoneMode.Spikes));
            }
            Assert.That(DungeonTrapSetup.Install(example), Is.SameAs(controller));
            Assert.That(controller.traps.Count, Is.EqualTo(3), "Setup must not duplicate the zones.");
            var sourceRoads = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ProBuilderMesh>(true))
                .Where(pb => pb.transform.parent != null && pb.transform.parent.name.StartsWith("01_Pista_Editable")).ToArray();
            var newRoads = example.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ProBuilderMesh>(true))
                .Where(pb => pb.transform.parent != null && pb.transform.parent.name.StartsWith("01_Pista_Editable")).ToDictionary(pb => pb.name);
            Assert.That(sourceRoads.Length, Is.GreaterThan(20)); Assert.That(newRoads.Count, Is.EqualTo(sourceRoads.Length));
            foreach (var road in sourceRoads)
            {
                Assert.That(newRoads[road.name].positions, Is.EqualTo(road.positions), road.name);
                Assert.That(newRoads[road.name].GetComponent<MeshCollider>().sharedMesh, Is.Not.Null, road.name);
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(example, true); EditorSceneManager.CloseScene(source, true);
        }
    }
}

using System.Linq;
using DungeonTrack.Editor;
using KartGame.Traps;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

public class DungeonRockSceneTests
{
    static string HierarchyPath(Transform t) => t.parent == null ? t.name : HierarchyPath(t.parent) + "/" + t.name;
    [Test]
    public void RockPrefabIsRoundKinematicAndHasOnlyATrigger()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DungeonRockSetup.PrefabPath);
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponent<Rigidbody>().isKinematic, Is.True);
        Assert.That(prefab.GetComponent<Rigidbody>().useGravity, Is.False);
        var colliders = prefab.GetComponentsInChildren<Collider>(true);
        Assert.That(colliders.Length, Is.EqualTo(1));
        Assert.That(colliders[0], Is.TypeOf<SphereCollider>()); Assert.That(colliders[0].isTrigger, Is.True);
        var rock = prefab.GetComponent<RockHazard>(); Assert.That(rock.visual, Is.Not.Null);
        Assert.That(AssetDatabase.Contains(rock.visual.GetComponent<MeshFilter>().sharedMesh), Is.True);
    }

    [Test]
    public void ExamplePreservesRoadsAndRegistersThreeDownhillRoutesWithoutDuplicatingTraps()
    {
        var source = EditorSceneManager.OpenScene(DungeonTrapSetup.ExampleScenePath, OpenSceneMode.Additive);
        var example = EditorSceneManager.OpenScene(DungeonRockSetup.ExampleScenePath, OpenSceneMode.Additive);
        try
        {
            var root = example.GetRootGameObjects();
            var controller = root.SelectMany(g => g.GetComponentsInChildren<LapTrapController>(true)).Single();
            var trap = root.SelectMany(g => g.GetComponentsInChildren<RockTrap>(true)).Single();
            Assert.That(controller.traps.Count, Is.EqualTo(4));
            Assert.That(controller.traps.OfType<BoostSpikeZone>().Count(), Is.EqualTo(3));
            Assert.That(controller.traps.Contains(trap), Is.True); Assert.That(trap.raceTimer, Is.Not.Null);
            Assert.That(trap.routes.Count, Is.EqualTo(3)); Assert.That(trap.laps.Length, Is.EqualTo(3));
            foreach (var route in trap.routes)
            {
                Assert.That(route.TryGetPath(out var path), Is.True);
                Assert.That(path[0].y, Is.GreaterThan(path[path.Length - 1].y));
                Assert.That(path[0].x, Is.LessThan(path[path.Length - 1].x));
                Assert.That(route.spawnPoint.position.y - route.impactPoint.position.y, Is.EqualTo(10).Within(.001f));
            }
            Assert.That(DungeonRockSetup.Install(example), Is.SameAs(trap));
            Assert.That(controller.traps.Count, Is.EqualTo(4));
            var before = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ProBuilderMesh>(true)).ToDictionary(p => HierarchyPath(p.transform));
            var after = root.SelectMany(g => g.GetComponentsInChildren<ProBuilderMesh>(true)).ToDictionary(p => HierarchyPath(p.transform));
            Assert.That(after.Count, Is.EqualTo(before.Count));
            foreach (var pair in before)
            {
                Assert.That(after[pair.Key].positions, Is.EqualTo(pair.Value.positions), pair.Key);
                Assert.That(after[pair.Key].transform.position, Is.EqualTo(pair.Value.transform.position), pair.Key);
            }
        }
        finally { EditorSceneManager.CloseScene(example, true); EditorSceneManager.CloseScene(source, true); }
    }

    [Test]
    public void InstallingOnEditedHeightAndUndoPreservePreviousZonesAndRoad()
    {
        var scene = EditorSceneManager.OpenScene(DungeonTrapSetup.ExampleScenePath, OpenSceneMode.Additive);
        try
        {
            var roots = scene.GetRootGameObjects();
            var controller = roots.SelectMany(g => g.GetComponentsInChildren<LapTrapController>(true)).Single();
            var road = roots.SelectMany(g => g.GetComponentsInChildren<ProBuilderMesh>(true)).First(p => p.name == "Z4_Curva_Amplia_Tramo_03");
            road.transform.position += Vector3.up * 5; Physics.SyncTransforms();
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            var rocks = DungeonRockSetup.Install(scene); Undo.CollapseUndoOperations(group);
            Assert.That(rocks.routes.All(r => r.impactPoint.position.y > 18), Is.True);
            Assert.That(controller.traps.Count, Is.EqualTo(4));
            Undo.PerformUndo();
            Assert.That(rocks == null, Is.True); Assert.That(controller.traps.Count, Is.EqualTo(3));
            Assert.That(road.transform.position.y, Is.EqualTo(5));
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }
}

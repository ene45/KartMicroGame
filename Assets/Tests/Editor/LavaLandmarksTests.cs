using System.Linq;
using DungeonTrack.Editor;
using KartGame.Traps;
using MedievalCartoon;
using MedievalCartoon.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;

public class LavaLandmarksTests
{
    static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;
    [Test]
    public void CenterIsModularDecorativeAndFitsFloorReserve()
    {
        var center = LavaLandmarkBuilder.Prefab("Anillo_Lava_Cadenas");
        Assert.That(center, Is.Not.Null);
        Assert.That(center.transform.Find("Anillo_Suspendido_Lava"), Is.Not.Null);
        Assert.That(center.transform.Find("Cuenca_Lava_Circular"), Is.Not.Null);
        var chains = center.transform.Find("Cadenas_Mover_Anclajes_Como_Grupos");
        Assert.That(chains.Cast<Transform>().Count(t => t.name.StartsWith("Cadena_")), Is.EqualTo(4));
        Assert.That(center.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
        Assert.That(center.GetComponentsInChildren<Collider>(true), Is.Empty);
        Assert.That(center.GetComponentsInChildren<LavaFlowVisual>(true).All(v => v.surfaces.Length > 0), Is.True);
        var basin = MedievalPackBuilder.BoundsOf(LavaLandmarkBuilder.Prefab("Cuenca_Lava_Circular"));
        Assert.That(basin.size.x, Is.LessThan(110)); Assert.That(basin.size.z, Is.LessThan(100));
        var top = MedievalPackBuilder.BoundsOf(center);
        Assert.That(top.max.y - 4.5f, Is.LessThanOrEqualTo(60));
    }
    [Test]
    public void FourGuardiansHaveDistinctSilhouettesAndIncludedGrayTexture()
    {
        string[] parts = { "Cabeza_Martillo", "Espada_Hoja", "Escudo_Lanza_Corta", "Llave_Anillo" };
        for (int i = 0; i < 4; i++)
        {
            var prefab = LavaLandmarkBuilder.Prefab(LavaLandmarkBuilder.Guardians[i]);
            Assert.That(prefab.transform.Find("Estatua_Editar_Piezas").Find(parts[i]), Is.Not.Null);
            var materials = prefab.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials).ToArray();
            Assert.That(materials.Any(m => m.name == "Piedra_Gris" && AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")).EndsWith("Piedra_Gris_Tenebrosa.png")), Is.True);
            Assert.That(materials.Any(m => m.name == "Piedra" || m.name == "Oro"), Is.False);
        }
    }
    [Test]
    public void ExampleHasFourCornersAndPlacementDoesNotRebuildRoad()
    {
        var scene = EditorSceneManager.OpenScene(LavaTrackTheme.ScenePath, OpenSceneMode.Additive);
        var source = EditorSceneManager.OpenScene(MedievalTrackTheme.ScenePath, OpenSceneMode.Additive);
        try
        {
            var root = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).First(t => t.name == "CIRCUITO_MAZMORRA_PROBUILDER");
            var group = root.Find(LavaTrackTheme.GroupName);
            Assert.That(group, Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Landmark_Gran_Guardian"), Is.False);
            var originalRoot = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).First(t => t.name == "CIRCUITO_MAZMORRA_PROBUILDER");
            var before = originalRoot.GetComponentsInChildren<ProBuilderMesh>(true).ToDictionary(m => PathOf(m.transform));
            var after = root.GetComponentsInChildren<ProBuilderMesh>(true).ToDictionary(m => PathOf(m.transform));
            Assert.That(after.Keys, Is.EquivalentTo(before.Keys));
            foreach (var pair in before)
            {
                Assert.That(after[pair.Key].positions, Is.EqualTo(pair.Value.positions), pair.Key);
                Assert.That(after[pair.Key].transform.localToWorldMatrix, Is.EqualTo(pair.Value.transform.localToWorldMatrix), pair.Key);
            }
            var traps = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LapTrapController>(true)).Single();
            Assert.That(traps.traps.Count, Is.EqualTo(4));
            var originalRock = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RockTrap>(true)).Single();
            var rock = traps.traps.OfType<RockTrap>().Single();
            for (int i = 0; i < 3; i++)
            {
                Assert.That(rock.routes[i].TryGetPath(out var a), Is.True);
                Assert.That(originalRock.routes[i].TryGetPath(out var b), Is.True);
                Assert.That(a, Is.EqualTo(b));
            }
            var colliders = root.Find("01_Pista_Editable_18_24_32m").GetComponentsInChildren<Collider>();
            Physics.SyncTransforms();
            for (int i = 0; i < 4; i++)
            {
                var guardian = group.Find(LavaLandmarkBuilder.Guardians[i]);
                Assert.That(guardian.position, Is.EqualTo(LavaTrackTheme.Positions[i]));
                var pedestal = MedievalPackBuilder.BoundsOf(guardian.Find("Pedestal_Gran_Guardian").gameObject);
                foreach (float x in new[] { pedestal.min.x, pedestal.center.x, pedestal.max.x })
                    foreach (float z in new[] { pedestal.min.z, pedestal.center.z, pedestal.max.z })
                        Assert.That(colliders.Any(c => c.Raycast(new Ray(new Vector3(x,100,z),Vector3.down),out _,200)), Is.False, guardian.name + " pedestal over road");
            }
            int count = group.childCount;
            LavaTrackTheme.Apply(scene);
            Assert.That(root.Find(LavaTrackTheme.GroupName).childCount, Is.EqualTo(count));
        }
        finally { EditorSceneManager.CloseScene(source, true); EditorSceneManager.CloseScene(scene, true); }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using KartGame.Traps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DungeonTrack.Editor
{
    public static class DungeonTrapSetup
    {
        public const string PrefabPath = "Assets/DungeonTrack/Prefabs/Zona_Acelerador_Pinches.prefab";
        public const string ExampleScenePath = "Assets/DungeonTrack/DungeonCircuit_Trampas.unity";

        [MenuItem("Tools/Dungeon Track/Agregar aceleradores y pinches a la escena actual")]
        static void InstallInCurrentScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Salí de Play Mode antes de agregar las zonas.");
            Install(SceneManager.GetActiveScene());
        }

        // Additive scene creation preserves existing scene geometry and manual edits.
        public static void CreateExampleScene()
        {
            string destination = AssetDatabase.GenerateUniqueAssetPath(ExampleScenePath);
            if (!AssetDatabase.CopyAsset(DungeonTrackBuilder.ScenePath, destination))
                throw new InvalidOperationException("No se pudo copiar la escena de la pista.");
            string text = File.ReadAllText(destination);
            text = Regex.Replace(text, @"(m_Id:\s*)[0-9a-fA-F-]{36}", match => match.Groups[1].Value + Guid.NewGuid());
            File.WriteAllText(destination, text);
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(destination, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                Install(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("No se pudo guardar la escena con trampas.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        public static LapTrapController Install(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var existing = roots.SelectMany(g => g.GetComponentsInChildren<LapTrapController>(true)).FirstOrDefault();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                return existing;
            }
            var objective = roots.SelectMany(g => g.GetComponentsInChildren<ObjectiveCompleteLaps>(true))
                .FirstOrDefault(o => o.enabled && o.gameObject.activeInHierarchy);
            if (objective == null) throw new InvalidOperationException("La escena necesita un ObjectiveCompleteLaps activo.");

            var meshes = roots.SelectMany(g => g.GetComponentsInChildren<ProBuilderMesh>(true)).ToArray();
            var roadPieces = Enumerable.Range(1, 3).Select(i => meshes.FirstOrDefault(m =>
                m.name == "Z2_Curva_Drift_Peralte_Tramo_" + i.ToString("00"))).ToArray();
            if (roadPieces.Any(m => m == null))
                throw new InvalidOperationException("No se encontraron los primeros tres tramos de la curva Z2 de la mazmorra.");

            Physics.SyncTransforms();
            // Check all placements before creating anything in a manually edited scene.
            var placements = new Pose[3];
            for (int i = 0; i < 3; i++)
            {
                var road = roadPieces[i].GetComponent<MeshCollider>();
                if (road == null || !road.Raycast(new Ray(road.bounds.center + Vector3.up * 100, Vector3.down), out var hit, 200))
                    throw new InvalidOperationException("No se encontró la superficie de " + roadPieces[i].name);
                Vector3 forward = i < 2 ? roadPieces[i + 1].GetComponent<Renderer>().bounds.center - hit.point
                    : hit.point - roadPieces[i - 1].GetComponent<Renderer>().bounds.center;
                forward = Vector3.ProjectOnPlane(forward, hit.normal).normalized;
                placements[i] = new Pose(hit.point + hit.normal * .04f, Quaternion.LookRotation(forward, hit.normal));
            }
            var prefab = EnsurePrefab();
            var root = new GameObject("TRAMPAS_POR_VUELTA");
            SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Agregar trampas por vuelta");
            var controller = Undo.AddComponent<LapTrapController>(root);
            controller.lapObjective = objective;

            for (int i = 0; i < 3; i++)
            {
                var zoneObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                Undo.RegisterCreatedObjectUndo(zoneObject, "Agregar zona de trampas");
                zoneObject.name = "Zona_" + (char)('A' + i) + "_Acelerador_Pinches";
                zoneObject.transform.SetParent(root.transform, true);
                zoneObject.transform.SetPositionAndRotation(placements[i].position, placements[i].rotation);
                var zone = zoneObject.GetComponent<BoostSpikeZone>();
                zone.modesByLap = new[] { LapZoneMode.Boost, i == 1 ? LapZoneMode.Spikes : LapZoneMode.Boost, LapZoneMode.Spikes };
                controller.traps.Add(zone);
                PrefabUtility.RecordPrefabInstancePropertyModifications(zone);
            }
            controller.ApplyLap(1);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root;
            return controller;
        }

        public static GameObject EnsurePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null) return existing;
            if (!AssetDatabase.IsValidFolder("Assets/DungeonTrack/Prefabs")) AssetDatabase.CreateFolder("Assets/DungeonTrack", "Prefabs");

            var root = new GameObject("Zona_Acelerador_Pinches");
            try
            {
                var zone = root.AddComponent<BoostSpikeZone>();
                var box = root.GetComponent<BoxCollider>();
                box.isTrigger = true; box.center = Vector3.up; box.size = new Vector3(6, 2, 8);
                zone.boostVisual = VisualGroup("Acelerador", root.transform);
                zone.spikesVisual = VisualGroup("Pinches", root.transform);
                zone.inactiveVisual = VisualGroup("Inactiva", root.transform);
                Plate(zone.boostVisual.transform, ColorMaterial("Trampa_Boost", new Color(.08f, .65f, .24f)));
                Plate(zone.spikesVisual.transform, ColorMaterial("Trampa_Pinches", new Color(.65f, .08f, .1f)));
                Plate(zone.inactiveVisual.transform, ColorMaterial("Trampa_Inactiva", new Color(.3f, .3f, .3f)));
                var symbolMat = ColorMaterial("Trampa_Simbolos", new Color(1f, .85f, .15f));
                for (int i = 0; i < 3; i++)
                {
                    float z = -2.4f + i * 2.4f;
                    var arrow = ProBuilderMesh.Create(new[] { new Vector3(-1.8f,.08f,z-.7f), new Vector3(0,.08f,z+.5f), new Vector3(1.8f,.08f,z-.7f), new Vector3(0,.08f,z+1.5f) },
                        new[] { new Face(new[] { 0,3,1,1,3,2 }) });
                    arrow.name = "Flecha_" + (i + 1); arrow.transform.SetParent(zone.boostVisual.transform, false);
                    arrow.GetComponent<MeshRenderer>().sharedMaterial = symbolMat;
                    arrow.ToMesh(); arrow.Refresh(); RemoveColliders(arrow.gameObject);
                }
                for (int x = 0; x < 3; x++) for (int z = 0; z < 4; z++)
                {
                    var spike = ProBuilderMesh.Create(new[] {
                        new Vector3(-.35f,0,-.35f),new Vector3(.35f,0,-.35f),new Vector3(.35f,0,.35f),new Vector3(-.35f,0,.35f),new Vector3(0,.65f,0) },
                        new[] { new Face(new[] { 0,4,1,1,4,2,2,4,3,3,4,0 }), new Face(new[] { 0,1,2,0,2,3 }) });
                    spike.name = "Pinche_" + x + "_" + z; spike.transform.SetParent(zone.spikesVisual.transform, false);
                    spike.transform.localPosition = new Vector3((x-1)*1.8f,.05f,(z-1.5f)*1.8f);
                    spike.GetComponent<MeshRenderer>().sharedMaterial = symbolMat;
                    spike.ToMesh(); spike.Refresh(); RemoveColliders(spike.gameObject);
                }
                zone.ApplyLap(1);
                // Prefabs need asset-backed meshes; scene-local meshes are not persistent.
                if (!AssetDatabase.IsValidFolder("Assets/DungeonTrack/Prefabs/Meshes"))
                    AssetDatabase.CreateFolder("Assets/DungeonTrack/Prefabs", "Meshes");
                foreach (var pb in root.GetComponentsInChildren<ProBuilderMesh>(true))
                {
                    var mesh = pb.GetComponent<MeshFilter>().sharedMesh;
                    AssetDatabase.CreateAsset(mesh, AssetDatabase.GenerateUniqueAssetPath("Assets/DungeonTrack/Prefabs/Meshes/" + pb.name + ".asset"));
                }
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        static GameObject VisualGroup(string name, Transform parent)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false); return g;
        }
        static void Plate(Transform parent, Material material)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = "Placa"; g.transform.SetParent(parent, false);
            g.transform.localScale = new Vector3(6, .06f, 8);
            g.GetComponent<Renderer>().sharedMaterial = material; RemoveColliders(g);
        }
        static void RemoveColliders(GameObject g)
        {
            foreach (var collider in g.GetComponents<Collider>()) Object.DestroyImmediate(collider);
        }
        static Material ColorMaterial(string name, Color color)
        {
            string path = "Assets/DungeonTrack/Materials/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            mat.SetColor("_BaseColor", color); mat.SetFloat("_Smoothness", .2f);
            AssetDatabase.CreateAsset(mat, path); return mat;
        }
    }
}

using System;
using System.Collections.Generic;
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
    public static class DungeonRockSetup
    {
        public const string PrefabPath = "Assets/DungeonTrack/Prefabs/Roca_Redonda.prefab";
        public const string ExampleScenePath = "Assets/DungeonTrack/DungeonCircuit_Rocas.unity";

        [MenuItem("Tools/Dungeon Track/Agregar rocas a la escena actual")]
        static void InstallInCurrentScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Salí de Play Mode antes de agregar las rocas.");
            Install(SceneManager.GetActiveScene());
        }

        public static void CreateExampleScene()
        {
            string destination = AssetDatabase.GenerateUniqueAssetPath(ExampleScenePath);
            if (!AssetDatabase.CopyAsset(DungeonTrapSetup.ExampleScenePath, destination))
                throw new InvalidOperationException("No se pudo copiar la escena con aceleradores y pinches.");
            string text = File.ReadAllText(destination);
            text = Regex.Replace(text, @"(m_Id:\s*)[0-9a-fA-F-]{36}", m => m.Groups[1].Value + Guid.NewGuid());
            File.WriteAllText(destination, text); AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(destination, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene); Install(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("No se pudo guardar la escena con rocas.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        public static RockTrap Install(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var existing = roots.SelectMany(g => g.GetComponentsInChildren<RockTrap>(true)).FirstOrDefault();
            if (existing != null) { Selection.activeGameObject = existing.gameObject; return existing; }
            var objective = roots.SelectMany(g => g.GetComponentsInChildren<ObjectiveCompleteLaps>(true))
                .FirstOrDefault(o => o.isActiveAndEnabled);
            if (objective == null) throw new InvalidOperationException("La escena necesita un ObjectiveCompleteLaps activo.");
            var controller = roots.SelectMany(g => g.GetComponentsInChildren<LapTrapController>(true)).FirstOrDefault();
            var meshes = roots.SelectMany(g => g.GetComponentsInChildren<ProBuilderMesh>(true)).ToArray();
            var pieces = Enumerable.Range(1, 3).Reverse().Select(i => meshes.FirstOrDefault(m =>
                m.name == "Z4_Curva_Amplia_Tramo_" + i.ToString("00"))).ToArray();
            if (pieces.Any(p => p == null)) throw new InvalidOperationException("No se encontraron los primeros tres tramos Z4_Curva_Amplia.");
            Physics.SyncTransforms();

            // Derive editable points from the actual road vertices and collider, not saved heights.
            var lanes = new List<Vector3>[3];
            for (int lane = 0; lane < 3; lane++)
            {
                lanes[lane] = new List<Vector3>();
                float across = .5f + (lane - 1) * .25f;
                foreach (var piece in pieces)
                {
                    var p = piece.positions;
                    int segments = (p.Count - 56) / 64;
                    if (segments < 1 || segments * 64 + 56 != p.Count)
                        throw new InvalidOperationException("Cambió la topología de " + piece.name + ". Usá recorridos con puntos colocados manualmente.");
                    var rows = new List<Vector3>();
                    for (int i = 0; i < segments; i++)
                        rows.Add(piece.transform.TransformPoint(Vector3.Lerp(p[i * 64], p[i * 64 + 51], across)));
                    rows.Add(piece.transform.TransformPoint(Vector3.Lerp(p[(segments - 1) * 64 + 1], p[(segments - 1) * 64 + 50], across)));
                    rows.Reverse();
                    var collider = piece.GetComponent<MeshCollider>();
                    for (int i = 0; i < rows.Count; i += 3)
                        AddSurfacePoint(collider, rows[i], lanes[lane]);
                    if ((rows.Count - 1) % 3 != 0) AddSurfacePoint(collider, rows[rows.Count - 1], lanes[lane]);
                }
                if (lanes[lane].Count < 2) throw new InvalidOperationException("No se pudo construir el recorrido de la roca.");
            }
            var prefab = EnsurePrefab();
            if (controller == null)
            {
                var parent = new GameObject("TRAMPAS_POR_VUELTA"); SceneManager.MoveGameObjectToScene(parent, scene);
                Undo.RegisterCreatedObjectUndo(parent, "Agregar controlador por vueltas");
                controller = Undo.AddComponent<LapTrapController>(parent);
                controller.lapObjective = objective;
            }
            var root = new GameObject("ROCAS_POR_VUELTA"); SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Agregar rocas");
            root.transform.SetParent(controller.transform, false);
            var trap = Undo.AddComponent<RockTrap>(root);
            trap.rockPrefab = prefab.GetComponent<RockHazard>();
            trap.raceTimer = roots.SelectMany(g => g.GetComponentsInChildren<TimeManager>(true)).FirstOrDefault();
            for (int lane = 0; lane < 3; lane++)
            {
                var routeObject = new GameObject("Recorrido_" + (char)('A' + lane));
                Undo.RegisterCreatedObjectUndo(routeObject, "Agregar recorrido de roca");
                routeObject.transform.SetParent(root.transform, false);
                var route = Undo.AddComponent<RockRoute>(routeObject);
                route.impactPoint = Point("Impacto", lanes[lane][0], route.transform);
                route.spawnPoint = Point("Aparicion", lanes[lane][0] + Vector3.up * 10, route.transform);
                route.travelPoints = lanes[lane].Skip(1).Select((p, i) =>
                    Point(i == lanes[lane].Count - 2 ? "Final_Desaparicion" : "Punto_" + (i + 1).ToString("00"), p, route.transform)).ToArray();
                trap.routes.Add(route); trap.sequence.Add(route);
            }
            foreach (var lap in trap.laps) lap.enabledRoutes = trap.routes.ToArray();
            Undo.RecordObject(controller, "Registrar rocas por vuelta");
            if (controller.lapObjective == null) controller.lapObjective = objective;
            controller.traps.Add(trap);
            EditorUtility.SetDirty(controller);
            controller.ApplyLap(objective.CurrentRaceLap);
            EditorSceneManager.MarkSceneDirty(scene); Selection.activeGameObject = root;
            return trap;
        }

        static void AddSurfacePoint(MeshCollider road, Vector3 sample, List<Vector3> points)
        {
            if (road == null || !road.Raycast(new Ray(sample + Vector3.up * 100, Vector3.down), out var hit, 200))
                throw new InvalidOperationException("No se encontró la superficie de la pendiente. Ajustá los recorridos manualmente.");
            Vector3 center = hit.point + hit.normal * 1.54f;
            if (points.Count == 0 || (points[points.Count - 1] - center).sqrMagnitude > .01f) points.Add(center);
        }

        static Transform Point(string name, Vector3 position, Transform parent)
        {
            var g = new GameObject(name); Undo.RegisterCreatedObjectUndo(g, "Agregar punto de roca");
            g.transform.SetParent(parent, false); g.transform.position = position; return g.transform;
        }

        public static GameObject EnsurePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null) return existing;
            var root = new GameObject("Roca_Redonda");
            try
            {
                var rock = root.AddComponent<RockHazard>();
                var body = root.GetComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
                var sphere = root.GetComponent<SphereCollider>(); sphere.isTrigger = true; sphere.radius = .5f;
                var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere); visual.name = "Roca_Visual";
                Object.DestroyImmediate(visual.GetComponent<Collider>());
                visual.transform.SetParent(root.transform, false); rock.visual = visual.transform;
                var mesh = Object.Instantiate(visual.GetComponent<MeshFilter>().sharedMesh); mesh.name = "Roca_Redonda";
                var vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; i++)
                    vertices[i] *= .94f + .06f * Mathf.PerlinNoise(vertices[i].x * 7 + vertices[i].y * 11 + 30, vertices[i].z * 9 + 40);
                mesh.vertices = vertices; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, "Assets/DungeonTrack/Prefabs/Meshes/Roca_Redonda.asset");
                visual.GetComponent<MeshFilter>().sharedMesh = mesh;
                string path = "Assets/DungeonTrack/Materials/Roca.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Roca" };
                    material.SetColor("_BaseColor", new Color(.38f, .34f, .29f)); material.SetFloat("_Smoothness", .05f);
                    AssetDatabase.CreateAsset(material, path);
                }
                visual.GetComponent<Renderer>().sharedMaterial = material;
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}

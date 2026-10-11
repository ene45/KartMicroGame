using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MedievalCartoon.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DungeonTrack.Editor
{
    public static class DungeonPortalBuilder
    {
        const string Library = "Assets/MedievalCartoon";
        public const string PrefabPath = Library + "/Prefabs/Arquitectura/Portal_Integrado_Pared_21_6m.prefab";
        const string Example = "Assets/DungeonTrack/DungeonCircuit_AnilloLava 1.unity";
        const string Request = "Assets/DungeonTrack/Editor/IntegrarPortales.request";
        const float Inner = 10.8f, Outer = 12.1f, Spring = 2.6f, HalfWidth = 12.9f, Top = 16.3f;
        const int Segments = 18;

        sealed class Geometry
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<Face> faces = new List<Face>();
            void Triangle(Vector3 a, Vector3 b, Vector3 c, int material)
            {
                int n = vertices.Count; vertices.AddRange(new[] { a, b, c });
                faces.Add(new Face(new[] { n, n + 1, n + 2 }) { submeshIndex = material, manualUV = true });
            }
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int material)
            {
                int n = vertices.Count; vertices.AddRange(new[] { a, b, c, d });
                faces.Add(new Face(new[] { n, n + 1, n + 2, n, n + 2, n + 3 }) { submeshIndex = material, manualUV = true });
            }
            // Convex counter-clockwise polygons; both visible sides and the thickness are closed.
            public void Extrude(Vector2[] polygon, float depth, int material = 0)
            {
                Vector3[] front = polygon.Select(p => new Vector3(p.x, p.y, -depth / 2)).ToArray();
                Vector3[] back = polygon.Select(p => new Vector3(p.x, p.y, depth / 2)).ToArray();
                for (int i = 1; i < polygon.Length - 1; i++)
                {
                    Triangle(front[0], front[i + 1], front[i], material);
                    Triangle(back[0], back[i], back[i + 1], material);
                }
                for (int i = 0; i < polygon.Length; i++)
                {
                    int j = (i + 1) % polygon.Length;
                    Quad(front[i], front[j], back[j], back[i], material);
                }
            }
            public void Box(float left, float bottom, float right, float top, float depth, int material = 0)
                => Extrude(new[] { new Vector2(left, bottom), new Vector2(right, bottom),
                    new Vector2(right, top), new Vector2(left, top) }, depth, material);
        }

        static Vector2 Arc(float angle, float radius) => new Vector2(Mathf.Cos(angle) * radius, Spring + Mathf.Sin(angle) * radius);
        static Material Material(string name) => AssetDatabase.LoadAssetAtPath<Material>(Library + "/Materials/" + name + ".mat");

        static void Piece(Transform parent, string name, Geometry geometry, Material[] materials)
        {
            var pb = ProBuilderMesh.Create(geometry.vertices, geometry.faces);
            pb.name = name; pb.transform.SetParent(parent, false); pb.gameObject.layer = 10;
            pb.GetComponent<MeshRenderer>().sharedMaterials = materials;
            pb.ToMesh(); pb.Refresh();
            // Metre-based UVs keep the stone pattern consistent across the curved and straight pieces.
            var uv = pb.positions.Select(p => new Vector2(p.x / 4, p.y / 4)).ToArray();
            pb.textures = uv; pb.ToMesh(); pb.Refresh();
            var mesh = pb.GetComponent<MeshFilter>().sharedMesh;
            mesh.name = "Portal_Integrado_" + name;
            string path = Library + "/Meshes/" + mesh.name + ".asset";
            AssetDatabase.CreateAsset(mesh, path);
            pb.preserveMeshAssetOnDestroy = true;
            var collider = pb.GetComponent<MeshCollider>();
            if (collider == null) collider = pb.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh; collider.convex = false;
        }

        [MenuItem("Tools/Dungeon Track/Portales/Mostrar arco integrado en Project")]
        public static void ShowAsset()
        {
            EnsureAsset(); var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Selection.activeObject = asset; EditorUtility.FocusProjectWindow(); EditorGUIUtility.PingObject(asset);
        }

        public static void EnsureAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;
            var wall = Material("Piedra_Gris"); var trim = Material("Piedra_Gris_Clara");
            if (wall == null || trim == null) throw new InvalidOperationException("Faltan los materiales de piedra gris de la mazmorra.");
            var root = new GameObject("Portal_Integrado_Pared_21_6m"); root.SetActive(false);
            try
            {
                var fill = new Geometry();
                fill.Box(-HalfWidth, -5.5f, -Outer, Spring, 2.08f);
                fill.Box(Outer, -5.5f, HalfWidth, Spring, 2.08f);
                var arch = new Geometry();
                for (int i = 0; i < Segments; i++)
                {
                    float a = Mathf.PI * i / Segments, b = Mathf.PI * (i + 1) / Segments;
                    Vector2 oa = Arc(a, Outer), ob = Arc(b, Outer);
                    fill.Extrude(new[] { ob, oa, new Vector2(oa.x, Top), new Vector2(ob.x, Top) }, 2.08f);
                    arch.Extrude(new[] { Arc(a, Inner), oa, ob, Arc(b, Inner) }, 2.6f, i % 3 == 0 ? 1 : 0);
                }
                // The wings continue into the adjoining walls; the opening remains 21.6 m wide.
                fill.Box(-HalfWidth, Spring, -Outer, Top, 2.08f);
                fill.Box(Outer, Spring, HalfWidth, Top, 2.08f);
                Piece(root.transform, "01_Relleno_Pared_Laterales_Y_Sobre_Arco", fill, new[] { wall });
                Piece(root.transform, "02_Arco_Dovelas_Editable", arch, new[] { trim, wall });
                var pillars = new Geometry();
                pillars.Box(-Outer, 0, -Inner, Spring, 2.6f);
                pillars.Box(Inner, 0, Outer, Spring, 2.6f);
                Piece(root.transform, "03_Pilares_Editable", pillars, new[] { trim });
                var key = new Geometry(); key.Box(-.7f, Spring + Inner - .025f, .7f, Spring + Outer + .35f, 2.92f);
                Piece(root.transform, "04_Clave_Piedra_Editable", key, new[] { trim });
                root.SetActive(true); PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("DUNGEON_PORTAL_ASSET: " + PrefabPath);
        }

        [MenuItem("Tools/Dungeon Track/Portales/Integrar entrada y salida en la escena actual")]
        public static void ApplyCurrent() => Apply(SceneManager.GetActiveScene());

        public static void Apply(Scene scene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Salí de Play Mode para colocar los portales.");
            var transforms = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var circuit = transforms.SingleOrDefault(t => t.name == "CIRCUITO_MAZMORRA_PROBUILDER");
            var group = transforms.SingleOrDefault(t => t.name == "05_Arcos_Entrada_Salida");
            if (circuit == null || group == null) throw new InvalidOperationException("No se encontró el circuito de mazmorra.");
            // Validate both anchors before changing the scene. Existing handmade geometry is retained.
            foreach (string name in new[] { "Entrada", "Salida" })
                if (group.Find(name + "_Portal_Integrado") == null && group.Find(name + "_Boveda")?.GetComponent<ProBuilderMesh>() == null)
                    throw new InvalidOperationException("No se encontró el arco original de " + name + ".");
            EnsureAsset();
            Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Integrar arcos con las paredes");
            foreach (string name in new[] { "Entrada", "Salida" })
            {
                if (group.Find(name + "_Portal_Integrado") != null) continue;
                var vault = group.Find(name + "_Boveda").GetComponent<ProBuilderMesh>();
                var points = vault.positions.Select(p => circuit.InverseTransformPoint(vault.transform.TransformPoint(p))).ToArray();
                float minY = points.Min(p => p.y), maxY = points.Max(p => p.y);
                var center = new Vector3((points.Min(p => p.x) + points.Max(p => p.x)) / 2,
                    minY - Spring, (points.Min(p => p.z) + points.Max(p => p.z)) / 2);
                Vector3 forward = name == "Entrada" ? Vector3.right : new Vector3(-8, 0, 16.5f).normalized;
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
                Undo.RegisterCreatedObjectUndo(instance, "Agregar portal integrado"); instance.name = name + "_Portal_Integrado";
                instance.transform.SetParent(group, true); instance.transform.position = circuit.TransformPoint(center);
                instance.transform.rotation = circuit.rotation * Quaternion.LookRotation(forward, Vector3.up);
                float scale = (maxY - minY) / Outer;
                instance.transform.localScale = Vector3.Scale(circuit.lossyScale, Vector3.one * scale);
                // Preserve the originals for easy restoration without overlapping duplicate arch colliders.
                foreach (var old in transforms.Where(t => t.name == name + "_Boveda" || t.name.StartsWith(name + "_Pilar_")))
                {
                    Undo.RecordObject(old.gameObject, "Conservar arco original oculto"); old.gameObject.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(old.gameObject);
                }
                // Remove only decorative arch instances at this doorway, never unrelated arches elsewhere.
                foreach (var old in transforms.Where(t => t.name == "Arco_Abierto_18m" && Vector3.Distance(t.position, instance.transform.position) < 2))
                {
                    Undo.RecordObject(old.gameObject, "Ocultar arco decorativo superpuesto"); old.gameObject.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(old.gameObject);
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            }
            Undo.CollapseUndoOperations(undo); EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = group.Find("Entrada_Portal_Integrado").gameObject;
            Debug.Log("DUNGEON_PORTALS_APPLIED: " + scene.path);
        }

        static bool Hit(GameObject portal, float x, float y, bool back = false)
        {
            var ray = new Ray(portal.transform.TransformPoint(new Vector3(x, y, back ? 5 : -5)),
                portal.transform.TransformDirection(back ? Vector3.back : Vector3.forward));
            return portal.GetComponentsInChildren<Collider>().Any(c => c.Raycast(ray, out _, 10));
        }
        public static void Validate(GameObject portal)
        {
            Physics.SyncTransforms();
            // Test the whole 18 m driving corridor, including the low edges of the curved opening.
            for (float x = -9; x <= 9; x += .5f)
                for (float y = .3f; y <= 3; y += .5f)
                    if (Hit(portal, x, y) || Hit(portal, x, y, true)) throw new InvalidOperationException("El portal bloquea la pista: " + x + ", " + y);
            // Sample the former triangular holes and the upper edge, from both sides of the wall.
            int solidSamples = 0;
            for (float x = -12.2f; x <= 12.2f; x += .4f)
                for (float y = 2.8f; y <= 16.1f; y += .4f)
                {
                    if (x * x + (y - Spring) * (y - Spring) <= (Outer + .1f) * (Outer + .1f)) continue;
                    if (!Hit(portal, x, y) || !Hit(portal, x, y, true)) throw new InvalidOperationException("Hueco en el relleno del muro: " + x + ", " + y);
                    solidSamples++;
                }
            foreach (var pb in portal.GetComponentsInChildren<ProBuilderMesh>())
                if (pb.GetComponent<MeshFilter>().sharedMesh == null || pb.GetComponent<MeshCollider>().sharedMesh == null || pb.vertexCount == 0)
                    throw new InvalidOperationException("Pieza editable sin malla o collider: " + pb.name);
            Debug.Log("DUNGEON_PORTAL_VALIDATED: " + portal.name + "; muestras de muro=" + solidSamples + "; paso libre=18m");
        }

        public static void BuildExample()
        {
            var scene = EditorSceneManager.OpenScene(Example, OpenSceneMode.Single); Apply(scene);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            // Reopen to verify the actual serialized prefab meshes and colliders, not only the builder state.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene = EditorSceneManager.OpenScene(Example, OpenSceneMode.Single);
            foreach (var portal in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Where(t => t.name.EndsWith("_Portal_Integrado")))
                Validate(portal.gameObject);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        public static void Capture()
        {
            var scene = EditorSceneManager.OpenScene(Example, OpenSceneMode.Single);
            var portals = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Where(t => t.name.EndsWith("_Portal_Integrado")).ToArray();
            string output = Path.GetFullPath("../../outputs"); Directory.CreateDirectory(output);
            foreach (var portal in portals)
            {
                Validate(portal.gameObject);
                var camera = LavaLandmarkBuilder.PreviewCamera(scene, portal.TransformPoint(new Vector3(-17, 7, -30)), portal.TransformPoint(new Vector3(0, 8, 0)), 64);
                var light = new GameObject("Luz_Preview_Portal").AddComponent<Light>(); light.type = LightType.Point;
                light.transform.position = portal.TransformPoint(new Vector3(-6, 10, -10)); light.range = 65; light.intensity = 35; light.color = new Color(.83f, .87f, 1);
                string path = output + "/" + portal.name + ".png";
                LavaLandmarkBuilder.Render(camera, path); LavaLandmarkBuilder.Render(camera, path);
                Object.DestroyImmediate(camera.gameObject); Object.DestroyImmediate(light.gameObject);
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [InitializeOnLoadMethod] static void QueueInstall() => EditorApplication.update += TryInstall;
        static void TryInstall()
        {
            if (!File.Exists(Request)) { EditorApplication.update -= TryInstall; return; }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetActiveScene(); if (scene.path != Example) return;
            EditorApplication.update -= TryInstall;
            try
            {
                Apply(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); AssetDatabase.DeleteAsset(Request);
                Debug.Log("DUNGEON_PORTALS_INSTALLED: entrada y salida integradas con las paredes y escena guardada.");
            }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}

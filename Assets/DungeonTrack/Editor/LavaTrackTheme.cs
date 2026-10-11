using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MedievalCartoon.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonTrack.Editor
{
    public static class LavaTrackTheme
    {
        public const string ScenePath = "Assets/DungeonTrack/DungeonCircuit_AnilloLava.unity";
        public const string GroupName = "11_Anillo_Lava_Y_Guardianes_Grises";
        public static readonly Vector3[] Positions = {
            new Vector3(-121, 0, 92), new Vector3(126, 3, 82),
            new Vector3(122, 3, -84), new Vector3(-118, 20, -112)
        };
        public static void CreateExample()
        {
            string source = MedievalTrackTheme.ScenePath;
            string destination = AssetDatabase.GenerateUniqueAssetPath(ScenePath);
            if (!AssetDatabase.CopyAsset(source, destination)) throw new IOException("No se pudo copiar el circuito.");
            // Unity tutorial IDs are independent of the asset GUID; copied scenes need fresh IDs.
            string content = Regex.Replace(File.ReadAllText(destination), @"(m_Id:\s*)[0-9a-fA-F-]{36}", m => m.Groups[1].Value + Guid.NewGuid());
            File.WriteAllText(destination, content); AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);
            var scene = EditorSceneManager.OpenScene(destination, OpenSceneMode.Single);
            Apply(scene); EditorSceneManager.SaveScene(scene);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("LAVA_TRACK_CREATED: " + destination);
        }
        // Used only in the isolated build checkout for preview adjustments.
        public static void RefreshBuildExample()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var group = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).First(t => t.name == GroupName);
            UnityEngine.Object.DestroyImmediate(group.gameObject);
            Apply(scene); EditorSceneManager.SaveScene(scene);
        }
        [MenuItem("Tools/Assets de Mazmorra/Colocar anillo y guardianes en mi circuito")]
        public static void ApplyToCurrent()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Salí de Play Mode para decorar.");
            Undo.IncrementCurrentGroup(); int id = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Anillo y guardianes grises");
            Apply(SceneManager.GetActiveScene()); Undo.CollapseUndoOperations(id);
        }
        public static void Apply(Scene scene)
        {
            var root = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == "CIRCUITO_MAZMORRA_PROBUILDER");
            if (!root) throw new InvalidOperationException("Abrí una escena del circuito de mazmorra.");
            var existing = root.Find(GroupName);
            if (existing) { Selection.activeGameObject = existing.gameObject; return; }
            var old = root.Find("10_Decoracion_MedievalCartoon");
            if (old)
            {
                var guardian = old.Find("Landmark_Gran_Guardian");
                if (guardian) Undo.DestroyObjectImmediate(guardian.gameObject);
                // The new basin fills the center; retain the plants beyond its rim.
                foreach (Transform prop in old)
                    if (prop.name == "Jardinera_Borde")
                    {
                        Undo.RecordObject(prop, "Separar jardinera de lava");
                        var direction = new Vector3(prop.position.x, 0, prop.position.z + 5).normalized;
                        prop.position = new Vector3(direction.x * 49, -4.5f, direction.z * 49 - 5);
                    }
            }
            var group = new GameObject(GroupName); SceneManager.MoveGameObjectToScene(group, scene);
            Undo.RegisterCreatedObjectUndo(group, "Agregar decoración"); group.transform.SetParent(root, false);
            Add("Anillo_Lava_Cadenas", group.transform, new Vector3(0, -4.5f, -5), 0, 1);
            float[] yaw = { 145, -145, -55, 55 };
            for (int i = 0; i < 4; i++)
            {
                Add(LavaLandmarkBuilder.Guardians[i], group.transform, Positions[i], yaw[i], .7f);
                float ground = i == 0 ? 0 : -4.5f;
                if (Positions[i].y > ground)
                {
                    var support = new GameObject("Soporte_Esquina_" + (i + 1)); support.transform.SetParent(group.transform, false);
                    Undo.RegisterCreatedObjectUndo(support, "Agregar apoyo de esquina");
                    support.transform.position = new Vector3(Positions[i].x, (Positions[i].y + ground) / 2, Positions[i].z);
                    support.transform.localScale = new Vector3(13.5f, Positions[i].y - ground, 13.5f);
                    support.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(LavaLandmarkBuilder.F + "/Meshes/Bloque.asset");
                    support.AddComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(LavaLandmarkBuilder.F + "/Materials/Piedra_Gris.mat");
                }
            }
            foreach (string name in new[] { "02_Paredes_Sala_60m", "03_Techos_Ocultar_Para_Editar", "05_Arcos_Entrada_Salida", "08_Bordes_Contencion_1_4m" })
            {
                var parent = root.Find(name); if (!parent) continue;
                var material = AssetDatabase.LoadAssetAtPath<Material>(LavaLandmarkBuilder.F + "/Materials/Piedra_Gris.mat");
                foreach (var renderer in parent.GetComponentsInChildren<MeshRenderer>(true))
                {
                    Undo.RecordObject(renderer, "Piedra gris"); renderer.sharedMaterials = renderer.sharedMaterials.Select(m => material).ToArray();
                }
            }
            Selection.activeGameObject = group; EditorSceneManager.MarkSceneDirty(scene);
        }
        static void Add(string name, Transform parent, Vector3 position, float yaw, float scale)
        {
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(LavaLandmarkBuilder.Prefab(name), parent);
            Undo.RegisterCreatedObjectUndo(obj, "Agregar landmark"); obj.transform.position = position;
            obj.transform.rotation = Quaternion.Euler(0, yaw, 0); obj.transform.localScale = Vector3.one * scale;
        }
    }
}

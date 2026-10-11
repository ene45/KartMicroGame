using System;
using System.IO;
using System.Linq;
using KartGame.KartSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonTrack.Editor
{
    public static class DungeonGrassSetup
    {
        const string Request = "Assets/DungeonTrack/Editor/ConfigurarPasto.request";
        const string Example = "Assets/DungeonTrack/DungeonCircuit_AnilloLava 1.unity";

        [MenuItem("Tools/Dungeon Track/Pasto/Configurar ralentización en la escena actual")]
        public static void ConfigureCurrent()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Salí de Play Mode para configurar el pasto.");
            Configure(SceneManager.GetActiveScene());
        }

        public static void Configure(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var grass = roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .SingleOrDefault(t => t.name == "Pasto_Suelo_Continuo_Exterior");
            if (grass == null || grass.GetComponent<Collider>() == null)
                throw new InvalidOperationException("No se encontró Pasto_Suelo_Continuo_Exterior con su collider.");
            var karts = roots.SelectMany(g => g.GetComponentsInChildren<ArcadeKart>(true)).ToArray();
            if (karts.Length == 0) throw new InvalidOperationException("La escena no contiene ningún kart.");
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Pasto que ralentiza");
            var surface = grass.GetComponent<GrassSlowZone>();
            if (surface == null) surface = Undo.AddComponent<GrassSlowZone>(grass.gameObject);
            foreach (var kart in karts)
            {
                var driver = kart.GetComponent<KartGrassSlowdown>();
                if (driver == null) driver = Undo.AddComponent<KartGrassSlowdown>(kart.gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(driver);
            }
            Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = grass.gameObject;
            Debug.Log("DUNGEON_GRASS_CONFIGURED: " + scene.path + "; velocidad=" + surface.speedMultiplier
                + "; aceleración=" + surface.accelerationMultiplier + "; karts=" + karts.Length);
        }

        public static void BuildExample()
        {
            var scene = EditorSceneManager.OpenScene(Example, OpenSceneMode.Single); Configure(scene);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [InitializeOnLoadMethod]
        static void QueueInstall() { EditorApplication.update += TryInstall; }
        static void TryInstall()
        {
            if (!File.Exists(Request)) { EditorApplication.update -= TryInstall; return; }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetActiveScene();
            if (scene.path != Example) return;
            Configure(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            AssetDatabase.DeleteAsset(Request); EditorApplication.update -= TryInstall;
            Debug.Log("DUNGEON_GRASS_INSTALLED: pasto configurado y escena guardada.");
        }
    }
}

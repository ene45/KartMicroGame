using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MedievalCartoon.Editor
{
    public sealed class DungeonAssetsWindow : EditorWindow
    {
        [MenuItem("Tools/Assets de Mazmorra/Abrir biblioteca", false, 0)]
        public static void Open() => GetWindow<DungeonAssetsWindow>("Assets de Mazmorra");
        void OnGUI()
        {
            GUILayout.Label("Medieval cartoon · piedra gris y lava", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Los modelos están en Assets/MedievalCartoon/Prefabs. Arrastrá un prefab desde Project a Scene o Hierarchy. Las piezas se editan expandiendo cada objeto.", MessageType.Info);
            if (GUILayout.Button("Mostrar TODOS los assets en Project")) Show("Assets/MedievalCartoon/Prefabs");
            if (GUILayout.Button("Mostrar escudo del guardián con cetro")) Show(GuardianShieldBuilder.PrefabPath);
            if (GUILayout.Button("Mostrar anillo, cadenas y 4 guardianes")) Show(LavaLandmarkBuilder.PrefabFolder);
            if (GUILayout.Button("Abrir catálogo de anillo y guardianes")) OpenScene(LavaLandmarkBuilder.CatalogPath);
            if (GUILayout.Button("Abrir circuito con anillo de lava")) OpenScene("Assets/DungeonTrack/DungeonCircuit_AnilloLava.unity");
            GUILayout.Space(12);
            foreach (string name in new[] { "Anillo_Lava_Cadenas", "Anillo_Suspendido_Lava", "Cuenca_Lava_Circular", "Cadena_Modulo_8m", "Guardian_Martillo_Gris", "Guardian_Espada_Gris", "Guardian_Escudo_Gris", "Guardian_Llave_Gris" })
                if (GUILayout.Button("Seleccionar " + name.Replace('_', ' '))) Show(LavaLandmarkBuilder.PrefabFolder + "/" + name + ".prefab");
            GUILayout.Space(12);
            EditorGUILayout.HelpBox("Para añadir el conjunto a tu circuito actual: Tools → Assets de Mazmorra → Colocar anillo y guardianes en mi circuito. La acción admite Ctrl+Z. No vuelve a generar tu pista.", MessageType.None);
        }
        static void Show(string path)
        {
            var obj = AssetDatabase.LoadMainAssetAtPath(path);
            Selection.activeObject = obj; EditorUtility.FocusProjectWindow(); EditorGUIUtility.PingObject(obj);
        }
        static void OpenScene(string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path);
        }
    }
}

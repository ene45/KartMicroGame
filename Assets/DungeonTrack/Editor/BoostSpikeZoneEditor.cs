using KartGame.Traps;
using UnityEditor;
using UnityEngine;

namespace DungeonTrack.Editor
{
    [CustomEditor(typeof(BoostSpikeZone)), CanEditMultipleObjects]
    public class BoostSpikeZoneEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

            var modes = serializedObject.FindProperty("modesByLap");
            EditorGUILayout.LabelField("Estado en cada vuelta", EditorStyles.boldLabel);
            int size = Mathf.Max(0, EditorGUILayout.IntField("Cantidad de vueltas configuradas", modes.arraySize));
            if (size != modes.arraySize) modes.arraySize = size;
            for (int i = 0; i < modes.arraySize; i++)
                EditorGUILayout.PropertyField(modes.GetArrayElementAtIndex(i), new GUIContent("Vuelta " + (i + 1)));
            EditorGUILayout.HelpBox("La misma zona cambia entre acelerador, pinches e inactiva. Se activa una vez por pasada. Si hay más vueltas que entradas, se usa la última.", MessageType.Info);

            DrawPropertiesExcluding(serializedObject, "m_Script", "modesByLap");
            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying && targets.Length == 1)
                EditorGUILayout.LabelField("Estado actual", ((BoostSpikeZone)target).CurrentMode.ToString());
        }
    }
}

using KartGame.Traps;
using UnityEditor;
using UnityEngine;

namespace DungeonTrack.Editor
{
    [CustomEditor(typeof(RockTrap)), CanEditMultipleObjects]
    public class RockTrapEditor : UnityEditor.Editor
    {
        void Field(string name, string label) => EditorGUILayout.PropertyField(serializedObject.FindProperty(name), new GUIContent(label), true);

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            Field("rockPrefab", "Prefab de roca");
            Field("routes", "Recorridos");
            Field("spawnOrder", "Orden de caída");
            if (serializedObject.FindProperty("spawnOrder").enumValueIndex == (int)RockSpawnOrder.Sequence)
                Field("sequence", "Secuencia (vacía = Recorridos)");
            Field("initialDelay", "Espera inicial (segundos)");
            Field("maximumActiveRocks", "Máximo de rocas en pista");
            Field("waitForRaceStart", "Esperar inicio de carrera");
            if (serializedObject.FindProperty("waitForRaceStart").boolValue) Field("raceTimer", "TimeManager de la carrera");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Configuración por vuelta", EditorStyles.boldLabel);
            var laps = serializedObject.FindProperty("laps");
            int size = Mathf.Clamp(EditorGUILayout.IntField("Cantidad de vueltas configuradas", laps.arraySize), 0, 100);
            if (size != laps.arraySize) laps.arraySize = size;
            for (int i = 0; i < laps.arraySize; i++)
            {
                var lap = laps.GetArrayElementAtIndex(i);
                lap.isExpanded = EditorGUILayout.Foldout(lap.isExpanded, "Vuelta " + (i + 1), true);
                if (!lap.isExpanded) continue;
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(lap.FindPropertyRelative("active"), new GUIContent("Generar rocas"));
                EditorGUILayout.PropertyField(lap.FindPropertyRelative("rollingSpeed"), new GUIContent("Velocidad al rodar (m/s)"));
                EditorGUILayout.PropertyField(lap.FindPropertyRelative("spawnInterval"), new GUIContent("Tiempo entre caídas (segundos)"));
                EditorGUILayout.PropertyField(lap.FindPropertyRelative("stunDuration"), new GUIContent("Detención del kart (segundos)"));
                EditorGUILayout.PropertyField(lap.FindPropertyRelative("enabledRoutes"), new GUIContent("Recorridos habilitados (vacío = todos)"), true);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.HelpBox("Las rocas ya lanzadas terminan con sus valores originales. La nueva vuelta configura las siguientes. Nunca se lanzan varias en el mismo paso.", MessageType.Info);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Apariencia de daño del kart", EditorStyles.boldLabel);
            Field("damageDarkening", "Intensidad de oscurecimiento");
            Field("damageBlinksPerSecond", "Parpadeos por segundo (0 = fijo)");
            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying && targets.Length == 1)
            {
                var rock = (RockTrap)target;
                EditorGUILayout.LabelField("Vuelta actual", rock.CurrentLap.ToString());
                EditorGUILayout.LabelField("Rocas en pista", rock.ActiveRockCount.ToString());
            }
        }
    }
}

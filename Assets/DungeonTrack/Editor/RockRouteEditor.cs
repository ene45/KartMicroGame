using KartGame.Traps;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DungeonTrack.Editor
{
    [CustomEditor(typeof(RockRoute))]
    public class RockRouteEditor : UnityEditor.Editor
    {
        ReorderableList m_Points;
        float m_SpawnHeight = 10;

        void OnEnable()
        {
            m_Points = new ReorderableList(serializedObject, serializedObject.FindProperty("travelPoints"), true, true, true, true);
            m_Points.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Puntos al rodar (último = desaparición)");
            m_Points.drawElementCallback = (rect, index, active, focused) =>
                EditorGUI.PropertyField(new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight),
                    m_Points.serializedProperty.GetArrayElementAtIndex(index), new GUIContent("Punto " + (index + 1)));
        }

        void Field(string name, string label) => EditorGUILayout.PropertyField(serializedObject.FindProperty(name), new GUIContent(label));
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            Field("spawnPoint", "Aparición");
            Field("impactPoint", "Impacto");
            m_Points.DoLayoutList();
            Field("diameter", "Diámetro de roca (metros)");
            Field("fallDuration", "Duración de caída (segundos)");
            Field("pathShape", "Movimiento entre puntos");
            serializedObject.ApplyModifiedProperties();
            var route = (RockRoute)target;
            if (!route.TryGetPath(out _))
                EditorGUILayout.HelpBox("Asigná Aparición, Impacto y al menos un punto final distinto de Impacto.", MessageType.Warning);
            EditorGUILayout.HelpBox("Los puntos representan el centro de la roca. Dejá medio diámetro entre ellos y el suelo. Arrastrá las filas para cambiar el orden. Los cambios se usan en las próximas rocas.", MessageType.Info);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("Crear un punto al final"))
                {
                    var points = route.travelPoints ?? new Transform[0];
                    var last = points.Length > 0 ? points[points.Length - 1] : route.impactPoint;
                    var g = new GameObject("Punto_" + (points.Length + 1).ToString("00"));
                    Undo.RegisterCreatedObjectUndo(g, "Crear punto de roca");
                    g.transform.SetParent(route.transform, false);
                    g.transform.position = last != null ? last.position + Vector3.right * 5 : route.transform.position;
                    Undo.RecordObject(route, "Agregar punto de roca");
                    var list = new System.Collections.Generic.List<Transform>(points) { g.transform };
                    route.travelPoints = list.ToArray();
                    EditorUtility.SetDirty(route);
                    Selection.activeGameObject = g;
                    serializedObject.Update();
                }
                m_SpawnHeight = Mathf.Max(.1f, EditorGUILayout.FloatField("Altura sobre Impacto", m_SpawnHeight));
                using (new EditorGUI.DisabledScope(route.spawnPoint == null || route.impactPoint == null))
                    if (GUILayout.Button("Colocar Aparición sobre Impacto"))
                    {
                        Undo.RecordObject(route.spawnPoint, "Mover aparición de roca");
                        route.spawnPoint.position = route.impactPoint.position + Vector3.up * m_SpawnHeight;
                    }
            }
        }

        void OnSceneGUI()
        {
            var route = (RockRoute)target;
            Handle(route.spawnPoint, "Aparición", new Color(1, .7f, .1f));
            Handle(route.impactPoint, "Impacto", Color.yellow);
            if (route.travelPoints == null) return;
            for (int i = 0; i < route.travelPoints.Length; i++)
                Handle(route.travelPoints[i], i == route.travelPoints.Length - 1 ? "Final / desaparece" : "Punto " + (i + 1), Color.cyan);
        }

        static void Handle(Transform point, string label, Color color)
        {
            if (point == null) return;
            Handles.color = color; Handles.Label(point.position + Vector3.up * .6f, label);
            EditorGUI.BeginChangeCheck();
            Vector3 position = Handles.PositionHandle(point.position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(point, "Mover punto de roca");
                point.position = position;
            }
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace MedievalCartoon.Editor
{
    [CustomEditor(typeof(CartoonNightSky))]
    public class CartoonNightSkyEditor : UnityEditor.Editor
    {
        UnityEditor.Editor m_LightEditor;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var sky = (CartoonNightSky)target;
            if (sky.moonLight == null || sky.moonLight.type != LightType.Directional)
            { EditorGUILayout.HelpBox("Asigná una Directional Light para sincronizar la luna.",MessageType.Warning); return; }
            EditorGUILayout.Space(); EditorGUILayout.LabelField("Directional Light de la luna",EditorStyles.boldLabel);
            Vector3 towardMoon = -sky.moonLight.transform.forward;
            float elevation = Mathf.Asin(Mathf.Clamp(towardMoon.y,-1,1))*Mathf.Rad2Deg;
            float azimuth = Mathf.Repeat(Mathf.Atan2(towardMoon.x,towardMoon.z)*Mathf.Rad2Deg,360);
            EditorGUI.BeginChangeCheck();
            elevation = EditorGUILayout.Slider("Altura de la luna (grados)",elevation,5,85);
            azimuth = EditorGUILayout.Slider("Dirección alrededor de la pista",azimuth,0,360);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sky.moonLight.transform,"Mover luna y su luz");
                float e=elevation*Mathf.Deg2Rad,a=azimuth*Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Sin(a)*Mathf.Cos(e),Mathf.Sin(e),Mathf.Cos(a)*Mathf.Cos(e));
                sky.moonLight.transform.rotation = Quaternion.LookRotation(-direction);
                PrefabUtility.RecordPrefabInstancePropertyModifications(sky.moonLight.transform); sky.SyncMoonDirection();
            }
            EditorGUILayout.HelpBox("La rotación de esta luz mueve también la luna visible. Ajustá abajo su color, intensidad y sombras; el brillo del cielo es independiente.",MessageType.Info);
            CreateCachedEditor(sky.moonLight,null,ref m_LightEditor); m_LightEditor.OnInspectorGUI();
            if (GUILayout.Button("Seleccionar Directional Light de la luna")) Selection.activeGameObject = sky.moonLight.gameObject;
        }
        void OnDisable() { if(m_LightEditor != null) DestroyImmediate(m_LightEditor); }
    }
}

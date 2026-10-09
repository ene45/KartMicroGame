using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MedievalCartoon
{
    [ExecuteAlways, DisallowMultipleComponent, AddComponentMenu("Mazmorra/Cielo nocturno cartoon")]
    public class CartoonNightSky : MonoBehaviour
    {
        [Header("Cielo y luna")]
        [Tooltip("Usá un material propio por escena. En Play Mode se usa una copia temporal.")]
        public Material skyboxMaterial;
        [Tooltip("Directional Light que ilumina como luna. Su rotación también mueve la luna del cielo.")]
        public Light moonLight;
        [HideInInspector] public Skybox[] cameraSkyboxes = new Skybox[0];
        public Color zenithColor = new Color(.025f,.035f,.085f);
        public Color horizonColor = new Color(.20f,.24f,.34f);
        public Color cloudColor = new Color(.10f,.13f,.20f);
        public Color moonColor = new Color(.83f,.89f,1);
        [Range(.3f,8f)] public float moonRadiusDegrees = 3.2f;
        [Range(0f,2f)] public float moonGlow = .45f;
        [Range(.1f,3f)] public float skyBrightness = 1f;
        [Header("Estrellas y nubes")]
        [Range(0f,1f)] public float stars = .6f;
        [Range(0f,3f)] public float starBrightness = .9f;
        [Range(0f,1f)] public float clouds = .35f;
        [Range(1f,16f)] public float cloudScale = 5f;
        [Range(0f,360f)] public float skyRotation;
        [Header("Luz ambiental para mantener legible la pista")]
        public Color ambientSky = new Color(.20f,.24f,.34f);
        public Color ambientHorizon = new Color(.14f,.17f,.23f);
        public Color ambientGround = new Color(.065f,.075f,.11f);

        Material m_RuntimeMaterial, m_PreviousSky;
        Light m_PreviousSun;
        AmbientMode m_PreviousAmbientMode;
        Color m_PreviousSkyColor, m_PreviousEquatorColor, m_PreviousGroundColor;
        bool m_Applied;
        Vector3 m_LastDirection = Vector3.zero;
        readonly System.Collections.Generic.Dictionary<Skybox,Material> m_PreviousCameraSkies = new System.Collections.Generic.Dictionary<Skybox,Material>();
        public Material AppliedMaterial => m_RuntimeMaterial != null ? m_RuntimeMaterial : skyboxMaterial;
        bool IsActiveScene => gameObject.scene.IsValid() && gameObject.scene == SceneManager.GetActiveScene();

        void OnEnable() => Apply();
        void OnValidate() { if (isActiveAndEnabled) Apply(); }
        void LateUpdate()
        {
            if (!IsActiveScene) return;
            if (!m_Applied) Apply();
            else SyncMoonDirection();
        }

        public void Apply()
        {
            if (!IsActiveScene || skyboxMaterial == null || moonLight == null || moonLight.type != LightType.Directional) return;
            if (!m_Applied)
            {
                m_PreviousSky = RenderSettings.skybox; m_PreviousSun = RenderSettings.sun;
                m_PreviousAmbientMode = RenderSettings.ambientMode;
                m_PreviousSkyColor = RenderSettings.ambientSkyColor;
                m_PreviousEquatorColor = RenderSettings.ambientEquatorColor;
                m_PreviousGroundColor = RenderSettings.ambientGroundColor;
                m_Applied = true;
            }
            if (Application.isPlaying && m_RuntimeMaterial == null)
                m_RuntimeMaterial = new Material(skyboxMaterial) { name = skyboxMaterial.name + " (Runtime)", hideFlags = HideFlags.DontSave };
            var material = AppliedMaterial;
            material.SetColor("_ZenithColor",zenithColor); material.SetColor("_HorizonColor",horizonColor);
            material.SetColor("_CloudColor",cloudColor); material.SetColor("_MoonColor",moonColor);
            material.SetFloat("_MoonSize",moonRadiusDegrees); material.SetFloat("_MoonGlow",moonGlow);
            material.SetFloat("_Stars",stars); material.SetFloat("_StarBrightness",starBrightness);
            material.SetFloat("_Clouds",clouds); material.SetFloat("_CloudScale",cloudScale);
            material.SetFloat("_SkyRotation",skyRotation); material.SetFloat("_Exposure",skyBrightness);
            RenderSettings.skybox = material; RenderSettings.sun = moonLight;
            foreach (var box in cameraSkyboxes)
                if (box != null)
                {
                    if (!m_PreviousCameraSkies.ContainsKey(box)) m_PreviousCameraSkies.Add(box,box.material);
                    box.material=material;
                }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSky; RenderSettings.ambientEquatorColor = ambientHorizon; RenderSettings.ambientGroundColor = ambientGround;
            m_LastDirection = Vector3.zero; SyncMoonDirection();
            MarkMaterialDirty();
        }

        public void SyncMoonDirection()
        {
            if (!m_Applied || moonLight == null || AppliedMaterial == null) return;
            // Light.forward points toward the ground; the visible moon is opposite that ray.
            Vector3 direction = -moonLight.transform.forward;
            if ((direction-m_LastDirection).sqrMagnitude < .0000001f) return;
            AppliedMaterial.SetVector("_MoonDirection",new Vector4(direction.x,direction.y,direction.z,0));
            m_LastDirection = direction; MarkMaterialDirty();
        }
        void MarkMaterialDirty()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && skyboxMaterial != null) UnityEditor.EditorUtility.SetDirty(skyboxMaterial);
#endif
        }
        void OnDisable()
        {
            if (m_Applied && RenderSettings.skybox == AppliedMaterial)
            {
                RenderSettings.skybox = m_PreviousSky; RenderSettings.sun = m_PreviousSun;
                RenderSettings.ambientMode = m_PreviousAmbientMode; RenderSettings.ambientSkyColor = m_PreviousSkyColor;
                RenderSettings.ambientEquatorColor = m_PreviousEquatorColor; RenderSettings.ambientGroundColor = m_PreviousGroundColor;
            }
            m_Applied = false;
            foreach (var previous in m_PreviousCameraSkies)
                if(previous.Key!=null && previous.Key.material==AppliedMaterial) previous.Key.material=previous.Value;
            m_PreviousCameraSkies.Clear();
            if (m_RuntimeMaterial != null)
            {
                if (Application.isPlaying) Destroy(m_RuntimeMaterial); else DestroyImmediate(m_RuntimeMaterial);
                m_RuntimeMaterial = null;
            }
        }
    }
}

using UnityEngine;

namespace MedievalCartoon
{
    [DisallowMultipleComponent]
    public sealed class CartoonTorch : MonoBehaviour
    {
        [Header("Luz y llama")]
        public Light pointLight;
        public Transform flame;
        public bool lightEnabled = true;
        [Min(0)] public float intensity = 2.5f;
        [Min(.1f)] public float range = 9;
        public Color color = new Color(1, .57f, .16f);
        [Header("Parpadeo suave")]
        [Range(0, .5f)] public float flicker = .12f;
        [Min(0)] public float flickerSpeed = 3;
        Vector3 originalScale;
        float phase;

        void Awake()
        {
            if (flame) originalScale = flame.localScale;
            phase = Mathf.Repeat(transform.position.x * 1.71f + transform.position.z * .89f, 6.28f);
        }

        void OnEnable() { Apply(1); }
        void OnDisable() { if (pointLight) pointLight.enabled = false; }
        void OnValidate() { Apply(1); }

        void Update()
        {
            float pulse = 1 + Mathf.Sin(Time.time * flickerSpeed + phase) * flicker;
            Apply(pulse);
            if (flame) flame.localScale = Vector3.Scale(originalScale, new Vector3(1, pulse, 1));
        }

        void Apply(float pulse)
        {
            if (!pointLight) return;
            pointLight.enabled = lightEnabled && isActiveAndEnabled;
            pointLight.color = color;
            pointLight.intensity = Mathf.Max(0, intensity) * pulse;
            pointLight.range = Mathf.Max(.1f, range);
            pointLight.shadows = LightShadows.None;
        }
    }
}

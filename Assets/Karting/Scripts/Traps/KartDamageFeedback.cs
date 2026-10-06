using System.Collections.Generic;
using UnityEngine;

namespace KartGame.Traps
{
    [DisallowMultipleComponent, AddComponentMenu("Kart/Trampas/Apariencia de daño")]
    public class KartDamageFeedback : MonoBehaviour
    {
        class Surface
        {
            public Renderer renderer;
            public int index, property;
            public Color color;
            public MaterialPropertyBlock original;
        }

        readonly List<Surface> m_Surfaces = new List<Surface>();
        MaterialPropertyBlock m_Work;
        float m_Remaining, m_Elapsed, m_Darkening, m_Blinks;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int LegacyColor = Shader.PropertyToID("_Color");
        public bool IsShowingDamage => m_Remaining > 0f;

        void Awake() => m_Work = new MaterialPropertyBlock();

        public void Show(float seconds, float darkening, float blinksPerSecond)
        {
            if (seconds <= 0f) return;
            if (!IsShowingDamage) Capture();
            m_Remaining = Mathf.Max(m_Remaining, seconds);
            m_Elapsed = 0f;
            m_Darkening = Mathf.Clamp01(darkening);
            m_Blinks = Mathf.Max(0f, blinksPerSecond);
            Paint();
        }

        void Capture()
        {
            m_Surfaces.Clear();
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                // Trails and particles are independent effects, not the kart's body.
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null) continue;
                    int property = material.HasProperty(BaseColor) ? BaseColor : LegacyColor;
                    if (!material.HasProperty(property)) continue;
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block, i);
                    // A renderer-wide block also applies when this slot has no override.
                    var inherited = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(inherited);
                    Color color = block.HasProperty(property) ? block.GetColor(property)
                        : inherited.HasProperty(property) ? inherited.GetColor(property) : material.GetColor(property);
                    m_Surfaces.Add(new Surface { renderer = renderer, index = i, property = property,
                        color = color, original = block });
                }
            }
        }

        void Update()
        {
            if (!IsShowingDamage) return;
            m_Remaining -= Time.deltaTime;
            m_Elapsed += Time.deltaTime;
            if (m_Remaining <= 0f) Restore(); else Paint();
        }

        void Paint()
        {
            float pulse = m_Blinks > 0f ? .65f + .35f * Mathf.Cos(m_Elapsed * m_Blinks * Mathf.PI * 2) : 1f;
            float brightness = 1f - m_Darkening * pulse;
            foreach (var surface in m_Surfaces)
            {
                if (surface.renderer == null) continue;
                // Read existing properties without instantiating or editing shared materials.
                surface.renderer.GetPropertyBlock(m_Work, surface.index);
                if (m_Work.isEmpty) surface.renderer.GetPropertyBlock(m_Work);
                Color color = surface.color * brightness; color.a = surface.color.a;
                m_Work.SetColor(surface.property, color);
                surface.renderer.SetPropertyBlock(m_Work, surface.index);
            }
        }

        void Restore()
        {
            foreach (var surface in m_Surfaces)
                if (surface.renderer != null) surface.renderer.SetPropertyBlock(surface.original, surface.index);
            m_Surfaces.Clear();
            m_Remaining = 0f;
        }

        void OnDisable() => Restore();
    }
}

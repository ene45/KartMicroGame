using UnityEngine;

namespace MedievalCartoon
{
    /// <summary>Decorative lava: scrolls UVs without changing shared materials or using physics.</summary>
    [DisallowMultipleComponent]
    public sealed class LavaFlowVisual : MonoBehaviour
    {
        [Header("Superficies de lava")]
        public Renderer[] surfaces = new Renderer[0];
        [Header("Movimiento visual (sin daño ni físicas)")]
        [Tooltip("Velocidad de la textura hacia abajo. Cero deja la lava quieta.")]
        [Min(0)] public float flowSpeed = .12f;
        static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
        MaterialPropertyBlock[] originals;
        MaterialPropertyBlock working;
        Vector4[] initialST;
        float elapsed;

        void OnEnable()
        {
            elapsed = 0;
            int count = surfaces == null ? 0 : surfaces.Length;
            originals = new MaterialPropertyBlock[count];
            initialST = new Vector4[count];
            working = new MaterialPropertyBlock();
            for (int i = 0; i < count; i++)
            {
                var renderer = surfaces[i];
                originals[i] = new MaterialPropertyBlock();
                if (!renderer || !renderer.sharedMaterial) continue;
                renderer.GetPropertyBlock(originals[i]);
                var scale = renderer.sharedMaterial.GetTextureScale("_BaseMap");
                var offset = renderer.sharedMaterial.GetTextureOffset("_BaseMap");
                initialST[i] = originals[i].HasVector(BaseMapST) ? originals[i].GetVector(BaseMapST)
                    : new Vector4(scale.x, scale.y, offset.x, offset.y);
            }
        }
        void Update()
        {
            elapsed = Mathf.Repeat(elapsed + Time.deltaTime * Mathf.Max(0, flowSpeed), 1);
            if (originals == null || surfaces == null) return;
            for (int i = 0; i < Mathf.Min(surfaces.Length, originals.Length); i++)
            {
                var renderer = surfaces[i];
                if (!renderer || !renderer.sharedMaterial) continue;
                renderer.GetPropertyBlock(working);
                Vector4 st = initialST[i];
                // Increasing the offset moves a feature toward smaller V, i.e. downward.
                st.w += elapsed;
                working.SetVector(BaseMapST, st);
                renderer.SetPropertyBlock(working);
            }
        }
        void OnDisable()
        {
            if (originals == null || surfaces == null) return;
            for (int i = 0; i < Mathf.Min(surfaces.Length, originals.Length); i++)
                if (surfaces[i]) surfaces[i].SetPropertyBlock(originals[i]);
            originals = null;
        }
    }
}

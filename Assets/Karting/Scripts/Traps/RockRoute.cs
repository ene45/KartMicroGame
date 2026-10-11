using System.Collections.Generic;
using UnityEngine;

namespace KartGame.Traps
{
    public enum RockPathShape
    {
        [InspectorName("Recto entre puntos")] Straight,
        [InspectorName("Suavizado")] Smooth
    }

    [DisallowMultipleComponent, AddComponentMenu("Kart/Trampas/Recorrido de roca")]
    public class RockRoute : MonoBehaviour
    {
        [Tooltip("Centro de la roca al aparecer, arriba de la pista.")]
        public Transform spawnPoint;
        [Tooltip("Centro de la roca al terminar de caer. Colocalo a un radio sobre el suelo.")]
        public Transform impactPoint;
        [Tooltip("Se recorren en orden después del impacto. El último es la desaparición.")]
        public Transform[] travelPoints = new Transform[0];
        [Min(.1f)] public float diameter = 3f;
        [Min(.01f)] public float fallDuration = .8f;
        public RockPathShape pathShape = RockPathShape.Straight;

        public bool TryGetPath(out Vector3[] path)
        {
            path = null;
            if (spawnPoint == null || impactPoint == null || travelPoints == null || travelPoints.Length == 0)
                return false;
            var anchors = new List<Vector3> { impactPoint.position };
            foreach (var point in travelPoints)
            {
                if (point == null) return false;
                if ((point.position - anchors[anchors.Count - 1]).sqrMagnitude > .000001f)
                    anchors.Add(point.position);
            }
            if (anchors.Count < 2) return false;
            if (pathShape == RockPathShape.Straight) { path = anchors.ToArray(); return true; }

            // Sample the curve by distance later, so unequal point spacing does not change speed.
            var samples = new List<Vector3> { anchors[0] };
            for (int i = 0; i < anchors.Count - 1; i++)
            {
                Vector3 a = anchors[Mathf.Max(0, i - 1)], b = anchors[i];
                Vector3 c = anchors[i + 1], d = anchors[Mathf.Min(anchors.Count - 1, i + 2)];
                for (int j = 1; j <= 16; j++)
                {
                    float t = j / 16f, t2 = t * t, t3 = t2 * t;
                    Vector3 p = .5f * ((2 * b) + (-a + c) * t +
                        (2 * a - 5 * b + 4 * c - d) * t2 + (-a + 3 * b - 3 * c + d) * t3);
                    if ((p - samples[samples.Count - 1]).sqrMagnitude > .000001f) samples.Add(p);
                }
            }
            path = samples.ToArray();
            return true;
        }

        void OnDrawGizmosSelected()
        {
            if (spawnPoint != null)
            {
                Gizmos.color = new Color(1, .7f, .1f);
                Gizmos.DrawWireSphere(spawnPoint.position, Mathf.Max(.1f, diameter) * .5f);
                if (impactPoint != null) Gizmos.DrawLine(spawnPoint.position, impactPoint.position);
            }
            if (!TryGetPath(out var path)) return;
            Gizmos.color = Color.cyan;
            for (int i = 1; i < path.Length; i++) Gizmos.DrawLine(path[i - 1], path[i]);
            Gizmos.DrawWireSphere(path[path.Length - 1], .4f);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace MedievalCartoon.Editor
{
    internal static class LavaLandmarkMeshes
    {
        public static Mesh Link()
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
            const int around = 20, tube = 8;
            for (int i = 0; i <= around; i++)
            {
                float a = i * 2 * Mathf.PI / around;
                var radial = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0).normalized;
                var center = new Vector3(.62f * Mathf.Cos(a), 1.02f * Mathf.Sin(a), 0);
                for (int j = 0; j <= tube; j++)
                {
                    float b = j * 2 * Mathf.PI / tube;
                    vertices.Add(center + .17f * (radial * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b)));
                    uv.Add(new Vector2((float)i / around, (float)j / tube));
                    if (i == around || j == tube) continue;
                    int n = i * (tube + 1) + j;
                    indices.AddRange(new[] { n, n + tube + 1, n + 1, n + 1, n + tube + 1, n + tube + 2 });
                }
            }
            return Finish("Eslabon_Grueso", vertices, uv, indices);
        }
        public static Mesh Waterfall()
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
            // Unit radius, height one, 22 degree arc. Cylindrical UVs keep the flow vertical.
            const int steps = 10;
            for (int i = 0; i <= steps; i++)
            {
                float a = (-11 + i * 22f / steps) * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                vertices.Add(p); vertices.Add(p + Vector3.up);
                uv.Add(new Vector2((float)i / steps, 0)); uv.Add(new Vector2((float)i / steps, 1));
                if (i == steps) continue;
                int n = i * 2;
                indices.AddRange(new[] { n, n + 2, n + 1, n + 2, n + 3, n + 1 });
            }
            return Finish("Cascada_Lava_22grados", vertices, uv, indices);
        }
        static Mesh Finish(string name, List<Vector3> vertices, List<Vector2> uv, List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}

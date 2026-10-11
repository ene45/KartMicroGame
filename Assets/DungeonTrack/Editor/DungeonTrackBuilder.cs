using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Cinemachine;
using KartGame.KartSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DungeonTrack.Editor
{
    // Editor only. Once saved, the scene contains ordinary, static ProBuilder meshes.
    // Nothing runs during gameplay or regenerates geometry after manual edits.
    public static class DungeonTrackBuilder
    {
        public const string Folder = "Assets/DungeonTrack";
        public const string ScenePath = Folder + "/DungeonCircuit_Ajustes.unity";
        public static string LastGeneratedScenePath { get; private set; }
        const float Width = 18f;
        const float Step = 2f;
        const float Thickness = .6f;
        const float CeilingHeight = 60f;
        const float BorderHeight = 1.4f;
        const float BorderThickness = .9f;
        const float RoomFloor = -4.5f;
        static readonly Vector3 Entry = new Vector3(-28, 0, 82);
        static readonly Vector3 Exit = new Vector3(-101, 0, 35);
        static readonly Vector3 ExitForward = new Vector3(-8, 0, 16.5f).normalized;
        static readonly Vector3[] RoomOutline = MakeRoomOutline();
        static Transform roads, walls, roofs, borders;
        static MeshCollider roomFloorCollider, exteriorGroundCollider;
        static Material roadMat, stoneMat, roofMat, grassMat, whiteMat, blackMat, cyanMat;
        static readonly List<RoadRecord> records = new List<RoadRecord>();
        static readonly List<BorderRecord> borderRecords = new List<BorderRecord>();

        struct Frame
        {
            public Vector3 p, right;
            public float bank, width;
            public Vector3 At(float offset, float extra = 0f) => p + right * offset + Vector3.up * (-Mathf.Tan(bank * Mathf.Deg2Rad) * offset + extra);
        }
        class RoadRecord
        {
            public ProBuilderMesh mesh;
            public List<Frame> frames;
            public string route;
        }
        class BorderRecord
        {
            public MeshCollider collider;
            public List<Frame> frames;
            public string name;
            public int side;
        }
        struct Knot
        {
            public Vector3 p;
            public float bank, width;
            public Knot(float x, float z, float y = 0, float b = 0, float w = Width) { p = new Vector3(x, y, z); bank = b; width = w; }
        }
        class Geometry
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<Face> faces = new List<Face>();
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool smooth = false)
            {
                int n = vertices.Count;
                vertices.AddRange(new[] { a, b, c, d });
                faces.Add(new Face(new[] { n, n + 1, n + 2, n, n + 2, n + 3 }) { smoothingGroup = smooth ? 1 : 0 });
            }
            public void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int n = vertices.Count;
                vertices.AddRange(new[] { a, b, c });
                faces.Add(new Face(new[] { n, n + 1, n + 2 }));
            }
        }

        [MenuItem("Tools/Dungeon Track/Crear una copia nueva del circuito")]
        public static void CreateNewCopy()
        {
            // Always create a new scene; existing edits are never overwritten.
            Build(AssetDatabase.GenerateUniqueAssetPath(ScenePath));
        }

        public static void BuildDefault() => Build(AssetDatabase.GenerateUniqueAssetPath(ScenePath));

        static Transform Group(string name, Transform parent = null)
        {
            var g = new GameObject(name);
            if (parent != null) g.transform.SetParent(parent, false);
            return g.transform;
        }

        static Material Material(string name, Color color, float smoothness = .15f)
        {
            string path = Folder + "/Materials/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Falta el shader URP/Lit del proyecto.");
            var mat = new Material(shader) { name = name };
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static ProBuilderMesh Mesh(string name, Geometry data, Transform parent, Material material, int layer, bool collision = true)
        {
            var pb = ProBuilderMesh.Create(data.vertices, data.faces);
            pb.name = name;
            pb.transform.SetParent(parent, false);
            pb.gameObject.layer = layer;
            // Positions are local to the zone group. Shared vertices are rebuilt by ProBuilder,
            // so moving an edge moves all incident faces of this individual piece.
            pb.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (collision)
            {
                // Unity's missing/destroyed component wrappers must be checked with its own
                // null semantics. The C# ?? operator can keep an invalid native wrapper.
                if (!pb.gameObject.TryGetComponent<MeshCollider>(out var collider))
                    collider = pb.gameObject.AddComponent<MeshCollider>();
                if (collider == null) throw new InvalidOperationException("No se pudo crear el collider: " + name);
                collider.convex = false;
            }
            pb.ToMesh();
            pb.Refresh();
            if (collision)
                pb.GetComponent<MeshCollider>().sharedMesh = pb.GetComponent<MeshFilter>().sharedMesh;
            GameObjectUtility.SetStaticEditorFlags(pb.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic);
            return pb;
        }

        static void Solid(string name, List<Frame> f, Transform parent, Material material, int layer,
            float widthOverride, float centerOffset, float upper, float lower, bool banked, int cross = 1, bool collision = true)
        {
            var geo = new Geometry();
            Func<Frame, float, float, Vector3> at = (row, offset, height) => row.p + row.right * offset +
                Vector3.up * (height + (banked ? -Mathf.Tan(row.bank * Mathf.Deg2Rad) * offset : 0));
            for (int i = 0; i < f.Count - 1; i++)
            {
                float wa = widthOverride > 0 ? widthOverride : f[i].width;
                float wb = widthOverride > 0 ? widthOverride : f[i + 1].width;
                // A centerOffset of +/-1 places wall strips outside the driving surface.
                float ca = centerOffset == 0 ? 0 : centerOffset * (f[i].width / 2 + widthOverride / 2);
                float cb = centerOffset == 0 ? 0 : centerOffset * (f[i + 1].width / 2 + widthOverride / 2);
                for (int j = 0; j < cross; j++)
                {
                    float a0 = ca + wa * ((float)j / cross - .5f), a1 = ca + wa * ((float)(j + 1) / cross - .5f);
                    float b0 = cb + wb * ((float)j / cross - .5f), b1 = cb + wb * ((float)(j + 1) / cross - .5f);
                    geo.Quad(at(f[i], a0, upper), at(f[i + 1], b0, upper), at(f[i + 1], b1, upper), at(f[i], a1, upper), banked);
                    geo.Quad(at(f[i], a1, lower), at(f[i + 1], b1, lower), at(f[i + 1], b0, lower), at(f[i], a0, lower));
                }
                float al = ca - wa / 2, ar = ca + wa / 2, bl = cb - wb / 2, br = cb + wb / 2;
                geo.Quad(at(f[i], al, lower), at(f[i + 1], bl, lower), at(f[i + 1], bl, upper), at(f[i], al, upper));
                geo.Quad(at(f[i], ar, upper), at(f[i + 1], br, upper), at(f[i + 1], br, lower), at(f[i], ar, lower));
            }
            for (int end = 0; end < 2; end++)
            {
                Frame row = f[end == 0 ? 0 : f.Count - 1];
                float w = widthOverride > 0 ? widthOverride : row.width;
                float c = centerOffset == 0 ? 0 : centerOffset * (row.width / 2 + widthOverride / 2);
                for (int j = 0; j < cross; j++)
                {
                    float l = c + w * ((float)j / cross - .5f), r = c + w * ((float)(j + 1) / cross - .5f);
                    if (end == 0) geo.Quad(at(row, l, upper), at(row, r, upper), at(row, r, lower), at(row, l, lower));
                    else geo.Quad(at(row, l, lower), at(row, r, lower), at(row, r, upper), at(row, l, upper));
                }
            }
            var mesh = Mesh(name, geo, parent, material, layer, collision);
            if (parent == roads) records.Add(new RoadRecord { mesh = mesh, frames = f, route = name });
            if (parent == borders) borderRecords.Add(new BorderRecord { collider = mesh.GetComponent<MeshCollider>(), frames = f, name = name, side = centerOffset < 0 ? -1 : 1 });
        }

        static List<Frame> Frames(Knot a, Knot b, Vector3 ta, Vector3 tb, Func<float, float> width = null)
        {
            int count = Mathf.Max(4, Mathf.CeilToInt((Vector3.Distance(a.p, b.p) + ta.magnitude * .2f + tb.magnitude * .2f) / Step));
            var rows = new List<Frame>();
            for (int i = 0; i <= count; i++)
            {
                float t = (float)i / count, t2 = t * t, t3 = t2 * t;
                Vector3 p = (2 * t3 - 3 * t2 + 1) * a.p + (t3 - 2 * t2 + t) * ta + (-2 * t3 + 3 * t2) * b.p + (t3 - t2) * tb;
                Vector3 tangent = (6 * t2 - 6 * t) * a.p + (3 * t2 - 4 * t + 1) * ta + (-6 * t2 + 6 * t) * b.p + (3 * t2 - 2 * t) * tb;
                Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;
                rows.Add(new Frame { p = p, right = right, bank = Mathf.Lerp(a.bank, b.bank, Mathf.SmoothStep(0, 1, t)), width = width == null ? Mathf.Lerp(a.width, b.width, Mathf.SmoothStep(0, 1, t)) : width(t) });
            }
            return rows;
        }

        static void Corridor(string name, List<Frame> f, bool indoors)
        {
            Solid(name, f, roads, roadMat, 11, 0, 0, 0, -Thickness, true, 7);
            if (!indoors) return;
            TrackBorders(name, f);
        }

        static void TrackBorders(string name, List<Frame> f)
        {
            // These low barriers follow the road's height and bank. The large room's
            // outer walls and ceiling are independent of the driving surface.
            Solid(name + "_Borde_Izq", f, borders, stoneMat, 10, BorderThickness, -1, BorderHeight, -Thickness, true);
            Solid(name + "_Borde_Der", f, borders, stoneMat, 10, BorderThickness, 1, BorderHeight, -Thickness, true);
        }

        static void Path(string zone, Knot[] knots, bool indoors, Vector3? startTangent = null, Vector3? endTangent = null)
        {
            for (int i = 0; i < knots.Length - 1; i++)
            {
                Vector3 ta = i == 0 ? knots[1].p - knots[0].p : (knots[i + 1].p - knots[i - 1].p) * .5f;
                Vector3 tb = i == knots.Length - 2 ? knots[i + 1].p - knots[i].p : (knots[i + 2].p - knots[i].p) * .5f;
                if (i == 0 && startTangent.HasValue) ta = startTangent.Value;
                if (i == knots.Length - 2 && endTangent.HasValue) tb = endTangent.Value;
                Corridor(zone + "_Tramo_" + (i + 1).ToString("00"), Frames(knots[i], knots[i + 1], ta, tb), indoors);
            }
        }

        static List<Frame> Straight(Vector3 start, Vector3 end, float width)
            => Frames(new Knot(start.x, start.z, start.y), new Knot(end.x, end.z, end.y), end - start, end - start, t => width);

        static void Box(string name, Vector3 center, Vector3 size, Transform parent, Material mat, int layer = 10, bool collision = true)
        {
            Solid(name, Straight(center - Vector3.forward * size.z / 2, center + Vector3.forward * size.z / 2, size.x), parent, mat, layer,
                size.x, 0, size.y / 2, -size.y / 2, false, 1, collision);
        }

        static void ExteriorGround(Transform exterior)
        {
            // One continuous ground surface extends below the entire exterior road,
            // including the finish approach, exit and first jump.
            Vector3[] ground = { new Vector3(-158, 0, 16), RoomOutline[8], RoomOutline[7],
                RoomOutline[6], RoomOutline[5], RoomOutline[4], RoomOutline[3], new Vector3(-8, 0, 168), new Vector3(-158, 0, 168) };
            exteriorGroundCollider = PolygonSlab("Pasto_Suelo_Continuo_Exterior", ground, -.75f, -1.45f, exterior, grassMat, 9).GetComponent<MeshCollider>();
        }

        static Vector3[] MakeRoomOutline()
        {
            Vector3 exitRight = Vector3.Cross(Vector3.up, ExitForward);
            // A 280 x 280 m square envelope, with the exterior finish corner cut out.
            // The two doorway sections meet the existing arches without moving the track.
            return new[] { new Vector3(-140, 0, -130), new Vector3(140, 0, -130),
                new Vector3(140, 0, 150), new Vector3(-8, 0, 150), Entry + Vector3.forward * 12.3f,
                Entry + Vector3.back * 12.3f, Exit + exitRight * 12.3f, Exit - exitRight * 12.3f,
                new Vector3(-140, 0, 24) };
        }

        static float CrossXZ(Vector3 a, Vector3 b, Vector3 c)
            => (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);

        static bool InsideRoom(Vector3 p)
        {
            bool inside = false;
            for (int i = 0, j = RoomOutline.Length - 1; i < RoomOutline.Length; j = i++)
            {
                Vector3 a = RoomOutline[i], b = RoomOutline[j];
                if (Mathf.Abs(CrossXZ(a, b, p)) < .002f && p.x >= Mathf.Min(a.x, b.x) - .001f
                    && p.x <= Mathf.Max(a.x, b.x) + .001f && p.z >= Mathf.Min(a.z, b.z) - .001f && p.z <= Mathf.Max(a.z, b.z) + .001f) return true;
                if ((a.z > p.z) != (b.z > p.z) && p.x < (b.x - a.x) * (p.z - a.z) / (b.z - a.z) + a.x) inside = !inside;
            }
            return inside;
        }

        static List<int> Triangulate(Vector3[] outline)
        {
            float area = 0;
            for (int i = 0; i < outline.Length; i++)
                area += outline[i].x * outline[(i + 1) % outline.Length].z - outline[(i + 1) % outline.Length].x * outline[i].z;
            var remaining = Enumerable.Range(0, outline.Length).ToList();
            if (area < 0) remaining.Reverse();
            var triangles = new List<int>();
            while (remaining.Count > 3)
            {
                bool clipped = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int a = remaining[(i + remaining.Count - 1) % remaining.Count], b = remaining[i], c = remaining[(i + 1) % remaining.Count];
                    if (CrossXZ(outline[a], outline[b], outline[c]) <= .0001f) continue;
                    bool contains = remaining.Any(k => k != a && k != b && k != c
                        && CrossXZ(outline[a], outline[b], outline[k]) >= -.0001f
                        && CrossXZ(outline[b], outline[c], outline[k]) >= -.0001f
                        && CrossXZ(outline[c], outline[a], outline[k]) >= -.0001f);
                    if (contains) continue;
                    triangles.AddRange(new[] { a, b, c }); remaining.RemoveAt(i); clipped = true; break;
                }
                if (!clipped) throw new InvalidOperationException("El contorno de la sala o del pasto se cruza.");
            }
            triangles.AddRange(remaining);
            return triangles;
        }

        static ProBuilderMesh PolygonSlab(string name, Vector3[] outline, float top, float bottom, Transform parent, Material material, int layer)
        {
            var geo = new Geometry();
            var triangles = Triangulate(outline);
            Func<int, float, Vector3> at = (index, height) => new Vector3(outline[index].x, height, outline[index].z);
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                geo.Triangle(at(a, top), at(c, top), at(b, top));
                geo.Triangle(at(a, bottom), at(b, bottom), at(c, bottom));
            }
            float area = 0;
            for (int i = 0; i < outline.Length; i++) area += CrossXZ(Vector3.zero, outline[i], outline[(i + 1) % outline.Length]);
            for (int i = 0; i < outline.Length; i++)
            {
                int j = (i + 1) % outline.Length;
                if (area > 0) geo.Quad(at(i, top), at(j, top), at(j, bottom), at(i, bottom));
                else geo.Quad(at(i, bottom), at(j, bottom), at(j, top), at(i, top));
            }
            return Mesh(name, geo, parent, material, layer);
        }

        static void DungeonRoom(Transform circuit)
        {
            var floor = Group("09_Suelo_Sala_Y_Landmark", circuit);
            roomFloorCollider = PolygonSlab("Sala_Suelo_Libre_Central", RoomOutline, RoomFloor, RoomFloor - 1, floor, stoneMat, 10).GetComponent<MeshCollider>();
            PolygonSlab("Sala_Techo_Unico_60m", RoomOutline, CeilingHeight + 1, CeilingHeight, roofs, roofMat, 10);
            for (int i = 0; i < RoomOutline.Length; i++)
            {
                Vector3 a = RoomOutline[i], b = RoomOutline[(i + 1) % RoomOutline.Length];
                bool doorway = i == 4 || i == 6;
                Solid(doorway ? "Sala_Dintel_" + (i == 4 ? "Entrada" : "Salida") : "Sala_Pared_Perimetral_" + i,
                    Straight(a, b, 2), walls, stoneMat, 10, 2, 0, CeilingHeight, doorway ? 16 : RoomFloor - 1, false);
            }
        }

        static void Fork(Transform root)
        {
            // At each junction the three 6 m mouths tile the full 18 m track with no gaps.
            // They open to three separate 18 m roads; side detours are physically longer.
            for (int side = -1; side <= 1; side++)
            {
                var f = new List<Frame>();
                for (int i = 0; i <= 40; i++)
                {
                    float t = (float)i / 40;
                    float offset = side * (6 + 25 * Mathf.Pow(Mathf.Sin(t * Mathf.PI), 2));
                    float derivative = side * 25 * Mathf.PI * Mathf.Sin(2 * t * Mathf.PI);
                    float distance = 6 + 25 * Mathf.Pow(Mathf.Sin(t * Mathf.PI), 2);
                    float centerWidth = Mathf.Min(Width, distance);
                    float projectedWidth = Width * Mathf.Sqrt(1 + Mathf.Pow(derivative / 62, 2));
                    float branchWidth = side == 0 ? centerWidth : Mathf.Min(projectedWidth, 2 * distance - centerWidth);
                    // Common cross-section planes keep junction edges exactly coincident.
                    f.Add(new Frame { p = new Vector3(-22 + 62 * t, 0, 82 + offset), right = Vector3.back, width = branchWidth });
                }
                string label = side == 0 ? "Centro_Rapido_Reserva_Hacha" : side < 0 ? "Lateral_Interior_Largo" : "Lateral_Exterior_Largo";
                Solid("Z1_Bifurcacion_" + label, f, roads, roadMat, 11, 0, 0, 0, -Thickness, true, 7);
                TrackBorders("Z1_Bifurcacion_" + label, f);
            }
        }

        static void Arch(string name, Vector3 center, Vector3 forward, Transform parent)
        {
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            const float inner = 10.8f, outer = 12.1f, baseHeight = 2.6f;
            var geo = new Geometry();
            Func<float, float, float, Vector3> point = (angle, radius, depth) => center + right * (Mathf.Cos(angle) * radius) + Vector3.up * (baseHeight + Mathf.Sin(angle) * radius) + forward * depth;
            for (int i = 0; i < 18; i++)
            {
                float a = Mathf.PI * i / 18, b = Mathf.PI * (i + 1) / 18;
                Vector3 p0 = point(a, inner, -1), p1 = point(b, inner, -1), p2 = point(b, outer, -1), p3 = point(a, outer, -1);
                Vector3 q0 = point(a, inner, 1), q1 = point(b, inner, 1), q2 = point(b, outer, 1), q3 = point(a, outer, 1);
                geo.Quad(p0, p1, p2, p3); geo.Quad(q3, q2, q1, q0);
                geo.Quad(p1, p0, q0, q1); geo.Quad(p3, p2, q2, q3);
                geo.Quad(p0, p3, q3, q0); geo.Quad(p2, p1, q1, q2);
            }
            Mesh(name + "_Boveda", geo, parent, cyanMat, 10);
            foreach (float side in new[] { -1f, 1f })
            {
                // Square pillars remain outside the 18 m road, irrespective of arch orientation.
                Box(name + "_Pilar_" + side, center + right * (side * 11.45f) + Vector3.up * 1.3f,
                    new Vector3(1.3f, 2.6f, 1.3f), parent, stoneMat);
            }
        }

        static void Light(string name, Vector3 p, Transform parent)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false); g.transform.position = p;
            var light = g.AddComponent<UnityEngine.Light>();
            light.type = LightType.Point; light.range = 29; light.intensity = 3.5f; light.color = new Color(1, .82f, .57f);
            light.shadows = LightShadows.None;
        }

        public static void Build(string output)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Generar fuera de Play Mode.");
            if (File.Exists(output)) throw new IOException("La escena ya existe. Usá Crear una copia nueva para conservar tus ediciones.");
            Directory.CreateDirectory(Folder + "/Materials"); Directory.CreateDirectory(Folder + "/Preview");
            AssetDatabase.Refresh();
            // Work in a temporary scene. An unsuccessful build must not leave a file that
            // looks like the finished circuit but still contains the original track.
            string staging = AssetDatabase.GenerateUniqueAssetPath(Folder + "/DungeonCircuit_BuildInProgress.unity");
            if (!AssetDatabase.CopyAsset("Assets/Karting/Scenes/MainScene.unity", staging)) throw new IOException("No se pudo copiar MainScene.");
            // Tutorial IDs are global across loaded editor scenes. A copied SceneObjectGuid
            // must receive a fresh ID before OnValidate runs during additive loading.
            string copiedScene = File.ReadAllText(staging);
            copiedScene = Regex.Replace(copiedScene, @"(?ms)^--- !u!114 &-?\d+\r?\n.*?(?=^---|\z)", section =>
                section.Value.Contains("guid: c8534e17a0f90604c9afd4f5c73d829f")
                    ? Regex.Replace(section.Value, @"(?m)^  m_Id: [0-9a-fA-F-]{36}\r?$", "  m_Id: " + Guid.NewGuid())
                    : section.Value);
            File.WriteAllText(staging, copiedScene);
            AssetDatabase.ImportAsset(staging, ImportAssetOptions.ForceUpdate);
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(staging, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            records.Clear(); borderRecords.Clear();
            try
            {
                // Operates exclusively on the copied scene, including its prefab instances.
                foreach (var root in scene.GetRootGameObjects())
                    if (new[] { "Environment", "Trees", "Hills", "Stones", "OvalTrack", "AdditionalTrack", "Clouds", "Action1", "Particle System" }.Contains(root.name)
                        || root.name.StartsWith("StoneFlat", StringComparison.Ordinal)) Object.DestroyImmediate(root);
                foreach (var target in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TargetObject>(true)).ToArray())
                    Object.DestroyImmediate(target.gameObject);

                roadMat = Material("Pista_Piedra", new Color(.42f, .37f, .40f));
                stoneMat = Material("Mazmorra_Pared", new Color(.24f, .28f, .33f));
                roofMat = Material("Mazmorra_Techo", new Color(.19f, .22f, .28f));
                grassMat = Material("Pasto", new Color(.21f, .56f, .28f));
                whiteMat = Material("Meta_Claro", new Color(.92f, .89f, .78f));
                blackMat = Material("Meta_Oscuro", new Color(.08f, .10f, .14f));
                cyanMat = Material("Arcos_Celeste", new Color(.17f, .67f, .73f));
                var circuit = Group("CIRCUITO_MAZMORRA_PROBUILDER");
                roads = Group("01_Pista_Editable_18_24_32m", circuit);
                walls = Group("02_Paredes_Sala_60m", circuit);
                roofs = Group("03_Techos_Ocultar_Para_Editar", circuit);
                var exterior = Group("04_Pasto_Exterior", circuit);
                var arches = Group("05_Arcos_Entrada_Salida", circuit);
                var lighting = Group("06_Iluminacion", circuit);
                foreach (var light in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UnityEngine.Light>(true)).Where(l => l.type == LightType.Directional))
                {
                    light.color = Color.white;
                    light.intensity = 1.2f;
                }
                var reserves = Group("07_Reservas_Trampas_Y_Landmark", circuit);
                borders = Group("08_Bordes_Contencion_1_4m", circuit);

                Path("Z1_Meta_Aproximacion", new[] { new Knot(-104, 62), new Knot(-94, 78), new Knot(-78, 82), new Knot(-57, 82) }, false, new Vector3(3, 0, 15), Vector3.right * 21);
                // First small jump follows the finish. The ramps and landings are editable meshes.
                Corridor("Z1_Salto1_Despegue", Frames(new Knot(-57, 82), new Knot(-45, 82, 1.8f), new Vector3(12, 0, 0), new Vector3(12, 3.6f, 0)), false);
                Corridor("Z1_Salto1_Recepcion", Frames(new Knot(-42, 82, .5f), new Knot(-30, 82), new Vector3(12, -.9f, 0), new Vector3(12, 0, 0)), false);
                Corridor("Z1_Entrada_Mazmorra", Straight(new Vector3(-30, 0, 82), new Vector3(-22, 0, 82), Width), true);
                Fork(circuit);
                Path("Z2_Curva_Drift_Peralte", new[] { new Knot(40, 82), new Knot(74, 79, 0, 5, 24), new Knot(96, 64, 0, 9, 24), new Knot(108, 36, 0, 9, 24), new Knot(108, 4, 0, 5, 24) }, true, Vector3.right * 34, Vector3.back * 32);
                Path("Z3_Recta_Reserva_Piso", new[] { new Knot(108, 4, 0, 5, 24), new Knot(106, -35, 0, 3, 24), new Knot(100, -59, 0, 0, 24) }, true, Vector3.back * 32, new Vector3(-7, 0, -18));
                Vector3 jumpDir = new Vector3(-.6f, 0, -.8f), launch = new Vector3(89.2f, 1.8f, -73.4f), landing = launch + jumpDir * 3.5f;
                landing.y = .3f;
                Corridor("Z3_Salto2_Despegue", Frames(new Knot(100, -59, 0, 0, 24), new Knot(launch.x, launch.z, launch.y, 0, 24), new Vector3(-7, 0, -18), new Vector3(-10.8f, 3.6f, -14.4f)), true);
                Corridor("Z3_Salto2_Recepcion", Frames(new Knot(landing.x, landing.z, landing.y, 0, 24), new Knot(67, -94, 0, 0, 32), jumpDir * 22 + Vector3.down, new Vector3(-24, 0, -12)), true);
                // Continue the low lateral barriers across the jump. The ground remains
                // open at the intentional gap, inside the large dungeon room.
                var gapFrames = Straight(launch, landing, 24);
                TrackBorders("Z3_Salto2", gapFrames);
                Path("Z4_Curva_Amplia", new[] { new Knot(67, -94, 0, 0, 32), new Knot(36, -104, 3, 7, 32), new Knot(-14, -103, 9, 7, 32), new Knot(-61, -96, 15, 9, 32), new Knot(-90, -78, 20, 9, 28), new Knot(-100, -53, 20, 3) }, true, new Vector3(-24, 0, -12), Vector3.forward * 25);
                Path("Z4_Subida_Puente_Bajada", new[] { new Knot(-100, -53, 20, 3), new Knot(-96, -30, 23), new Knot(-83, -8, 28), new Knot(-86, 12, 19), new Knot(-101, 35) }, true, Vector3.forward * 25, new Vector3(-8, 0, 16.5f));
                Path("Z1_Salida_Exterior", new[] { new Knot(-101, 35), new Knot(-104, 48), new Knot(-104, 62) }, false, new Vector3(-8, 0, 16.5f), new Vector3(3, 0, 15));

                DungeonRoom(circuit);
                ExteriorGround(exterior);
                Arch("Entrada", Entry, Vector3.right, arches);
                Arch("Salida", Exit, ExitForward, arches);
                Group("Landmark_Central_Reserva_110x100m", reserves).position = new Vector3(0, RoomFloor, -5);
                Group("Z1_Hacha_Centro_Pendiente", reserves).position = new Vector3(9, 0, 82);
                Group("Z2_Pinchos_Boost_Pendiente", reserves).position = new Vector3(106, 0, 35);
                Group("Z3_Piso_Desmoronable_Pendiente", reserves).position = new Vector3(106, 0, -25);
                Group("Z4_Rocas_Pendiente", reserves).position = new Vector3(-68, 16, -96);
                Group("Z1_Martillos_Pendiente", reserves).position = new Vector3(-102, 0, 56);
                foreach (var record in records.Where(r => r.route.StartsWith("Z2") || r.route.StartsWith("Z3") || r.route.StartsWith("Z4")))
                    Light("Luz_" + record.route, record.frames[record.frames.Count / 2].p + Vector3.up * 7, lighting);
                Light("Luz_Sala_Bifurcacion", new Vector3(9, 8, 82), lighting);
                Light("Luz_Sala_Lateral_Interior", new Vector3(9, 8, 58), lighting);
                Light("Luz_Sala_Lateral_Exterior", new Vector3(9, 8, 107), lighting);

                ConfigureGameplay(scene, circuit);
                RenderSettings.fog = false;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.43f, .47f, .53f);
                Validate(scene);
                Preview(scene, circuit);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, output)) throw new IOException("No se pudo guardar la escena.");
                AssetDatabase.SaveAssets();
                LastGeneratedScenePath = output;
                Debug.Log("DUNGEON_TRACK_OK: " + output);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                AssetDatabase.DeleteAsset(staging);
                AssetDatabase.Refresh();
            }
            VerifySavedScene(output);
        }

        public static void VerifySavedScene(string path)
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene loaded = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var root = loaded.GetRootGameObjects().FirstOrDefault(g => g.name == "CIRCUITO_MAZMORRA_PROBUILDER");
                if (root == null) throw new InvalidOperationException("La escena guardada no contiene el circuito.");
                var meshes = root.GetComponentsInChildren<ProBuilderMesh>(true);
                if (meshes.Length < 50) throw new InvalidOperationException("La escena guardada está incompleta.");
                foreach (var pb in meshes)
                {
                    if (pb.GetComponent<MeshFilter>().sharedMesh == null || pb.vertexCount == 0 || pb.faceCount == 0)
                        throw new InvalidOperationException("La malla no se conservó al reabrir: " + pb.name);
                    if (pb.name.StartsWith("Z") && (!pb.TryGetComponent<MeshCollider>(out var collider) || collider.sharedMesh == null))
                        throw new InvalidOperationException("El collider no se conservó al reabrir: " + pb.name);
                }
                File.AppendAllText(Folder + "/Validation.txt", "Escena reabierta: " + meshes.Length + " mallas ProBuilder conservadas, colliders correctos.\n");
            }
            finally
            {
                EditorSceneManager.CloseScene(loaded, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        static void ConfigureGameplay(Scene scene, Transform circuit)
        {
            var objects = scene.GetRootGameObjects();
            var player = objects.SelectMany(g => g.GetComponentsInChildren<ArcadeKart>(true)).FirstOrDefault();
            if (player == null) throw new InvalidOperationException("No se encontró el kart original.");
            player.transform.SetPositionAndRotation(new Vector3(-77, .65f, 82), Quaternion.LookRotation(Vector3.right));
            foreach (var vc in objects.SelectMany(g => g.GetComponentsInChildren<CinemachineVirtualCamera>(true))) { vc.Follow = player.transform; vc.LookAt = player.transform; }
            foreach (var objective in objects.SelectMany(g => g.GetComponentsInChildren<Objective>(true)))
            {
                bool laps = objective is ObjectiveCompleteLaps;
                objective.enabled = laps;
                if (laps) { objective.gameMode = GameMode.Laps; objective.isTimed = false; ((ObjectiveCompleteLaps)objective).lapsToComplete = 3; }
            }
            if (!objects.SelectMany(g => g.GetComponentsInChildren<ObjectiveCompleteLaps>(true)).Any())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Karting/Prefabs/GameModes/ObjectiveLaps.prefab");
                var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var objective = g.GetComponent<ObjectiveCompleteLaps>(); objective.lapsToComplete = 3; objective.isTimed = false;
            }
            var finish = new GameObject("Meta_3Vueltas_Logica_Original"); finish.transform.SetParent(circuit, false);
            finish.transform.position = new Vector3(-70, 2.5f, 82);
            var trigger = finish.AddComponent<BoxCollider>(); trigger.isTrigger = true; trigger.size = new Vector3(1.5f, 7, Width);
            var lap = finish.AddComponent<LapObject>(); lap.finishLap = true; lap.LapsCount = 0; lap.gameMode = GameMode.Laps; lap.layerMask = ~0;
            var marks = Group("Meta_Ajedrez_Visual", circuit);
            for (int col = 0; col < 12; col++) for (int row = 0; row < 2; row++)
                Box("Meta_" + col + "_" + row, new Vector3(-70 + (row - .5f) * 1.5f, .015f, 82 - Width / 2 + (col + .5f) * 1.5f),
                    new Vector3(1.5f, .025f, 1.5f), marks, (col + row) % 2 == 0 ? whiteMat : blackMat, 11, false);
        }

        static void Validate(Scene scene)
        {
            Physics.SyncTransforms();
            int checks = 0;
            foreach (var record in records)
            {
                var collider = record.mesh.GetComponent<MeshCollider>();
                if (collider == null || collider.sharedMesh == null) throw new InvalidOperationException("Collider inválido: " + record.route);
                for (int row = 1; row < record.frames.Count - 1; row++)
                {
                    var f = record.frames[row];
                    foreach (float fraction in new[] { -.43f, -.2f, 0, .2f, .43f })
                    {
                        Vector3 point = f.At(f.width * fraction);
                        if (!collider.Raycast(new Ray(point + Vector3.up * 5, Vector3.down), out RaycastHit hit, 10) || Mathf.Abs(hit.point.y - point.y) > .07f)
                            throw new InvalidOperationException("Superficie discontinua en " + record.route + " fila " + row);
                        checks++;
                    }
                }
                foreach (var face in record.mesh.faces)
                    for (int i = 0; i < face.indexes.Count; i += 3)
                    {
                        var p = record.mesh.positions;
                        Vector3 normal = Vector3.Cross(p[face.indexes[i + 1]] - p[face.indexes[i]], p[face.indexes[i + 2]] - p[face.indexes[i]]);
                        if (normal.sqrMagnitude < .00000001f)
                            throw new InvalidOperationException("Triángulo degenerado: " + record.route);
                        if (face.smoothingGroup == 1 && normal.y <= 0)
                            throw new InvalidOperationException("Curva demasiado cerrada para el ancho de la carretera: " + record.route);
                    }
            }
            Func<string, float> length = label => records.Where(r => r.route.Contains(label)).Sum(r => Enumerable.Range(1, r.frames.Count - 1).Sum(i => Vector3.Distance(r.frames[i - 1].p, r.frames[i].p)));
            float middle = length("Centro_Rapido"), inside = length("Lateral_Interior"), outside = length("Lateral_Exterior");
            if (inside <= middle || outside <= middle) throw new InvalidOperationException("Los desvíos deben ser más largos.");
            foreach (var a in records) foreach (var b in records)
            {
                if (a == b) continue;
                Frame tail = a.frames[a.frames.Count - 1], head = b.frames[0];
                if (Vector3.Distance(tail.p, head.p) < .001f && Mathf.Abs(tail.width - head.width) < .001f)
                    if (Vector3.Dot(tail.right, head.right) < .9999f || Mathf.Abs(tail.bank - head.bank) > .001f)
                        throw new InvalidOperationException("Unión no alineada entre " + a.route + " y " + b.route);
            }
            foreach (Vector3 gap in new[] { new Vector3(-43.5f, 1.8f, 82), new Vector3(88.15f, 1.8f, -74.8f) })
                if (records.Any(r => r.mesh.GetComponent<MeshCollider>().Raycast(new Ray(gap + Vector3.up * 10, Vector3.down), out _, 30)))
                    throw new InvalidOperationException("Un salto tiene un collider de carretera en el hueco.");

            int borderChecks = 0;
            foreach (var border in borderRecords)
            {
                for (int row = 1; row < border.frames.Count - 1; row++)
                {
                    var frame = border.frames[row];
                    Vector3 origin = frame.At(border.side * (frame.width / 2 - .3f), .7f);
                    if (!border.collider.Raycast(new Ray(origin, frame.right * border.side), out _, BorderThickness + .8f))
                        throw new InvalidOperationException("Borde de contención discontinuo: " + border.name + " fila " + row);
                    borderChecks++;
                }
            }

            int groundChecks = 0;
            foreach (var record in records.Where(r => r.route.StartsWith("Z1_Meta_") || r.route.StartsWith("Z1_Salto1_") || r.route.StartsWith("Z1_Salida_")))
                foreach (var frame in record.frames.Skip(1).Take(record.frames.Count - 2))
                    foreach (float fraction in new[] { -.49f, 0, .49f })
                    {
                        Vector3 p = frame.At(frame.width * fraction);
                        if (!exteriorGroundCollider.Raycast(new Ray(p + Vector3.up * 10, Vector3.down), out var groundHit, 100)
                            || Mathf.Abs(groundHit.point.y + .75f) > .01f)
                            throw new InvalidOperationException("Falta pasto bajo la pista exterior: " + record.route);
                        groundChecks++;
                    }
            foreach (var record in records.Where(r => !(r.route.StartsWith("Z1_Meta_") || r.route.StartsWith("Z1_Salto1_") || r.route.StartsWith("Z1_Salida_"))))
                foreach (var frame in record.frames)
                {
                    if (record.route == "Z1_Entrada_Mazmorra" && frame.p.x < Entry.x) continue;
                    foreach (float side in new[] { -1f, 1f })
                    {
                        Vector3 edge = frame.At(side * (frame.width / 2 + BorderThickness));
                        if (!InsideRoom(edge)) throw new InvalidOperationException("La sala corta el borde de la pista: " + record.route);
                        if (edge.y <= RoomFloor + .2f) throw new InvalidOperationException("El suelo de la sala invade la carretera: " + record.route);
                    }
                }
            if (!roomFloorCollider.Raycast(new Ray(new Vector3(0, 2, -5), Vector3.down), out var centreHit, 15)
                || Mathf.Abs(centreHit.point.y - RoomFloor) > .01f)
                throw new InvalidOperationException("Falta el suelo libre para el landmark central.");
            foreach (var gate in new[] { (Entry, Vector3.right), (Exit, ExitForward) })
                foreach (float offset in new[] { -Width * .43f, 0, Width * .43f })
                {
                    Vector3 right = Vector3.Cross(Vector3.up, gate.Item2);
                    var ray = new Ray(gate.Item1 + right * offset + Vector3.up * 2 - gate.Item2 * 2, gate.Item2);
                    if (walls.GetComponentsInChildren<MeshCollider>().Any(c => c.Raycast(ray, out _, 4)))
                        throw new InvalidOperationException("Una pared bloquea el arco de entrada o salida.");
                }

            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ProBuilderMesh>(true)).ToArray();
            File.WriteAllText(Folder + "/Validation.txt", "Unity " + Application.unityVersion + "\n" +
                "Anchos: exterior y roja 18 m, azul 24 m, violeta 32 m; transiciones graduales. Ramales: hasta 18 m; bocas 6 m.\n" +
                "Mallas ProBuilder: " + all.Length + ". Tramos de carretera: " + records.Count + ".\n" +
                "Raycasts sobre superficie: " + checks + ", correctos. Triángulos sin degeneraciones.\n" +
                "Bordes bajos: " + BorderHeight.ToString("F1") + " m. Raycasts laterales: " + borderChecks + ", correctos.\n" +
                "Sala: envolvente 280 x 280 m, esquina exterior recortada, techo a 60 m, centro libre 110 x 100 m. Portales sin paredes que los bloqueen.\n" +
                "Pasto exterior: " + groundChecks + " raycasts bajo la carretera, correctos. Suelo central de la sala conservado.\n" +
                "Ramal central: " + middle.ToString("F1") + " m; lateral interior: " + inside.ToString("F1") + " m; lateral exterior: " + outside.ToString("F1") + " m.\n" +
                "Saltos: dos, huecos de 3 m y 3.5 m. Peralte máximo: 9 grados. Roja: cima aproximada 28 m, bajada hasta 0 m. Violeta: izquierda 20 m, derecha 0 m.\n" +
                "Trampas y comportamiento de salto del kart pendientes de ajuste en Play Mode.\n");
        }

        static void Preview(Scene scene, Transform circuit)
        {
            // URP render requests can also draw other additively loaded editor scenes.
            // Hide their renderers only while capturing, then restore their exact state.
            var otherRenderers = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt).Where(s => s != scene && s.isLoaded)
                .SelectMany(s => s.GetRootGameObjects())
                .SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).Where(r => r.enabled).ToArray();
            foreach (var renderer in otherRenderers) renderer.enabled = false;
            var ceilingGroup = circuit.Find("03_Techos_Ocultar_Para_Editar");
            ceilingGroup.gameObject.SetActive(false);
            var cameraObject = new GameObject("PreviewCamera_TEMP"); SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene;
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.09f, .12f, .17f);
            camera.nearClipPlane = .3f; camera.farClipPlane = 1200;
            var data = cameraObject.AddComponent<UniversalAdditionalCameraData>(); data.renderPostProcessing = false;
            try
            {
                camera.orthographic = true; camera.orthographicSize = 145;
                camera.transform.SetPositionAndRotation(new Vector3(0, 300, 15), Quaternion.Euler(90, 0, 0));
                Render(camera, Folder + "/Preview/Planta.png", 1600, 1600);
                camera.orthographic = false; camera.fieldOfView = 42;
                camera.transform.position = new Vector3(-250, 255, -295); camera.transform.LookAt(new Vector3(0, 0, 8));
                Render(camera, Folder + "/Preview/Perspectiva.png", 1600, 1100);
                camera.fieldOfView = 60; camera.transform.position = new Vector3(-63, 6, 82); camera.transform.LookAt(new Vector3(15, 3, 82));
                ceilingGroup.gameObject.SetActive(true);
                Render(camera, Folder + "/Preview/Entrada.png", 1600, 900);
                camera.fieldOfView = 65;
                camera.transform.position = new Vector3(32, 25, 70); camera.transform.LookAt(new Vector3(-8, 7, -25));
                Render(camera, Folder + "/Preview/Sala.png", 1600, 1000);
            }
            finally
            {
                ceilingGroup.gameObject.SetActive(true);
                Object.DestroyImmediate(cameraObject);
                foreach (var renderer in otherRenderers) if (renderer != null) renderer.enabled = true;
            }
        }

        static void Render(Camera camera, string path, int width, int height)
        {
            var target = new RenderTexture(width, height, 24);
            RenderTexture previous = RenderTexture.active;
            try
            {
                target.Create(); camera.targetTexture = target;
                if (GraphicsSettings.currentRenderPipeline != null)
                    RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                else camera.Render();
                RenderTexture.active = target;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            }
            finally { camera.targetTexture = null; RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); }
        }
    }
}

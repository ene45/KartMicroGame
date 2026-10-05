using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public const string ScenePath = Folder + "/DungeonCircuit.unity";
        const float Width = 18f;
        const float Step = 2f;
        const float Thickness = .6f;
        const float CeilingHeight = 14f;
        static Transform roads, walls, roofs;
        static Material roadMat, stoneMat, roofMat, grassMat, whiteMat, blackMat, cyanMat;
        static readonly List<RoadRecord> records = new List<RoadRecord>();
        static int serial;

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
        struct Knot
        {
            public Vector3 p;
            public float bank;
            public Knot(float x, float z, float y = 0, float b = 0) { p = new Vector3(x, y, z); bank = b; }
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
        }

        [MenuItem("Tools/Dungeon Track/Crear una copia nueva del circuito")]
        public static void CreateNewCopy()
        {
            // Always create a new scene; existing edits are never overwritten.
            Build(AssetDatabase.GenerateUniqueAssetPath(ScenePath));
        }

        public static void BuildDefault() => Build(ScenePath);

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
                var collider = pb.gameObject.GetComponent<MeshCollider>() ?? pb.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = pb.GetComponent<MeshFilter>().sharedMesh;
            }
            pb.ToMesh();
            pb.Refresh();
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
                float ca = centerOffset == 0 ? 0 : centerOffset * (f[i].width / 2 + .65f);
                float cb = centerOffset == 0 ? 0 : centerOffset * (f[i + 1].width / 2 + .65f);
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
                float c = centerOffset == 0 ? 0 : centerOffset * (row.width / 2 + .65f);
                for (int j = 0; j < cross; j++)
                {
                    float l = c + w * ((float)j / cross - .5f), r = c + w * ((float)(j + 1) / cross - .5f);
                    if (end == 0) geo.Quad(at(row, l, upper), at(row, r, upper), at(row, r, lower), at(row, l, lower));
                    else geo.Quad(at(row, l, lower), at(row, r, lower), at(row, r, upper), at(row, l, upper));
                }
            }
            var mesh = Mesh(name, geo, parent, material, layer, collision);
            if (parent == roads) records.Add(new RoadRecord { mesh = mesh, frames = f, route = name });
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
                rows.Add(new Frame { p = p, right = right, bank = Mathf.Lerp(a.bank, b.bank, Mathf.SmoothStep(0, 1, t)), width = width == null ? Width : width(t) });
            }
            return rows;
        }

        static void Corridor(string name, List<Frame> f, bool indoors)
        {
            Solid(name, f, roads, roadMat, 11, 0, 0, 0, -Thickness, true, 7);
            if (!indoors) return;
            Solid(name + "_Pared_Izq", f, walls, stoneMat, 10, 1.3f, -1, CeilingHeight, -3, false);
            Solid(name + "_Pared_Der", f, walls, stoneMat, 10, 1.3f, 1, CeilingHeight, -3, false);
            Solid(name + "_Techo", f, roofs, roofMat, 10, Width + 2.6f, 0, CeilingHeight + .5f, CeilingHeight, false, 7);
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
            }
            // A simple hall encloses all three routes. Openings align with the main road.
            Box("Sala_Bifurcacion_Pared_Norte", new Vector3(9, 7, 123.5f), new Vector3(62, 14, 1.4f), walls, stoneMat);
            Box("Sala_Bifurcacion_Pared_Sur", new Vector3(9, 7, 40.5f), new Vector3(62, 14, 1.4f), walls, stoneMat);
            foreach (float x in new[] { -22f, 40f })
                foreach (float z in new[] { 56.25f, 107.75f })
                    Box("Sala_Hombro_" + serial++, new Vector3(x, 7, z), new Vector3(1.4f, 14, 31.5f), walls, stoneMat);
            Box("Sala_Bifurcacion_Techo", new Vector3(9, 14.25f, 82), new Vector3(63.4f, .5f, 84.4f), roofs, roofMat);
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
            if (!AssetDatabase.CopyAsset("Assets/Karting/Scenes/MainScene.unity", output)) throw new IOException("No se pudo copiar MainScene.");
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(output, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            records.Clear(); serial = 0;
            try
            {
                // Operates exclusively on the copied scene, including its prefab instances.
                foreach (var root in scene.GetRootGameObjects())
                    if (new[] { "Environment", "Trees", "Hills", "Stones", "OvalTrack", "AdditionalTrack", "Clouds", "Action1" }.Contains(root.name)) Object.DestroyImmediate(root);
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
                roads = Group("01_Pista_Editable_18m", circuit);
                walls = Group("02_Paredes_Simples", circuit);
                roofs = Group("03_Techos_Ocultar_Para_Editar", circuit);
                var exterior = Group("04_Pasto_Exterior", circuit);
                var arches = Group("05_Arcos_Entrada_Salida", circuit);
                var lighting = Group("06_Iluminacion", circuit);
                var reserves = Group("07_Reservas_Trampas_Y_Landmark", circuit);

                Path("Z1_Meta_Aproximacion", new[] { new Knot(-104, 62), new Knot(-98, 78), new Knot(-78, 82), new Knot(-57, 82) }, false, new Vector3(3, 0, 15), Vector3.right * 21);
                // First small jump follows the finish. The ramps and landings are editable meshes.
                Corridor("Z1_Salto1_Despegue", Frames(new Knot(-57, 82), new Knot(-45, 82, 1.8f), new Vector3(12, 0, 0), new Vector3(12, 3.6f, 0)), false);
                Corridor("Z1_Salto1_Recepcion", Frames(new Knot(-42, 82, .5f), new Knot(-30, 82), new Vector3(12, -.9f, 0), new Vector3(12, 0, 0)), false);
                Corridor("Z1_Entrada_Mazmorra", Straight(new Vector3(-30, 0, 82), new Vector3(-22, 0, 82), Width), true);
                Fork(circuit);
                Path("Z2_Curva_Drift_Peralte", new[] { new Knot(40, 82), new Knot(74, 79, 0, 5), new Knot(96, 64, 0, 9), new Knot(108, 36, 0, 9), new Knot(108, 4, 0, 5) }, true, Vector3.right * 34, Vector3.back * 32);
                Path("Z3_Recta_Reserva_Piso", new[] { new Knot(108, 4, 0, 5), new Knot(106, -35, 0, 3), new Knot(100, -59) }, true, Vector3.back * 32, new Vector3(-7, 0, -18));
                Vector3 jumpDir = new Vector3(-.6f, 0, -.8f), launch = new Vector3(89.2f, 1.8f, -73.4f), landing = launch + jumpDir * 3.5f;
                landing.y = .3f;
                Corridor("Z3_Salto2_Despegue", Frames(new Knot(100, -59), new Knot(launch.x, launch.z, launch.y), new Vector3(-7, 0, -18), new Vector3(-10.8f, 3.6f, -14.4f)), true);
                Corridor("Z3_Salto2_Recepcion", Frames(new Knot(landing.x, landing.z, landing.y), new Knot(67, -94), jumpDir * 22 + Vector3.down, new Vector3(-24, 0, -12)), true);
                // Walls/roof bridge the jump without adding a road collider in the gap.
                var gapFrames = Straight(launch, landing, Width);
                Solid("Z3_Salto2_Pared_Izq", gapFrames, walls, stoneMat, 10, 1.3f, -1, CeilingHeight, -5, false);
                Solid("Z3_Salto2_Pared_Der", gapFrames, walls, stoneMat, 10, 1.3f, 1, CeilingHeight, -5, false);
                Solid("Z3_Salto2_Techo", gapFrames, roofs, roofMat, 10, Width + 2.6f, 0, CeilingHeight + .5f, CeilingHeight, false, 7);
                Path("Z4_Curva_Amplia", new[] { new Knot(67, -94), new Knot(36, -104, 0, 7), new Knot(-14, -103, 0, 7), new Knot(-61, -96, 0, 9), new Knot(-90, -78, 0, 9), new Knot(-100, -53, 0, 3) }, true, new Vector3(-24, 0, -12), Vector3.forward * 25);
                Path("Z4_Subida_Puente_Bajada", new[] { new Knot(-100, -53, 0, 3), new Knot(-90, -29, 3), new Knot(-72, -10, 8), new Knot(-88, 15, 6), new Knot(-101, 35) }, true, Vector3.forward * 25, new Vector3(-8, 0, 16.5f));
                Path("Z1_Salida_Exterior", new[] { new Knot(-101, 35), new Knot(-104, 48), new Knot(-104, 62) }, false, new Vector3(-8, 0, 16.5f), new Vector3(3, 0, 15));

                Box("Pasto_Norte_Exterior", new Vector3(-64, -1.05f, 135), new Vector3(148, .7f, 48), exterior, grassMat, 9);
                Box("Pasto_Oeste_Exterior", new Vector3(-132, -1.05f, 70), new Vector3(35, .7f, 85), exterior, grassMat, 9);
                var island = new Geometry();
                island.Quad(new Vector3(-92, -.75f, 68), new Vector3(-75, -.75f, 71), new Vector3(-40, -.75f, 69), new Vector3(-65, -.75f, 57));
                Mesh("Pasto_Isla_Interior_Z1", island, exterior, grassMat, 9);
                Arch("Entrada", new Vector3(-28, 0, 82), Vector3.right, arches);
                Arch("Salida", new Vector3(-101, 0, 35), new Vector3(-8, 0, 16.5f).normalized, arches);
                Group("Landmark_Central_Reserva_110x100m", reserves).position = new Vector3(0, 0, -5);
                Group("Z1_Hacha_Centro_Pendiente", reserves).position = new Vector3(9, 0, 82);
                Group("Z2_Pinchos_Boost_Pendiente", reserves).position = new Vector3(106, 0, 35);
                Group("Z3_Piso_Desmoronable_Pendiente", reserves).position = new Vector3(106, 0, -25);
                Group("Z4_Rocas_Pendiente", reserves).position = new Vector3(-68, 0, -96);
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
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, output)) throw new IOException("No se pudo guardar la escena.");
                AssetDatabase.SaveAssets();
                Validate(scene);
                Preview(scene, circuit);
                Debug.Log("DUNGEON_TRACK_OK: " + output);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                AssetDatabase.Refresh();
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
                        if (normal.sqrMagnitude < .00000001f || (face.smoothingGroup == 1 && normal.y <= 0))
                            throw new InvalidOperationException("Triángulo degenerado: " + record.route);
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

            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ProBuilderMesh>(true)).ToArray();
            File.WriteAllText(Folder + "/Validation.txt", "Unity " + Application.unityVersion + "\n" +
                "Ancho principal: 18 m. Ancho de cada ramal: 18 m; bocas de unión: 6 m cada una.\n" +
                "Mallas ProBuilder: " + all.Length + ". Tramos de carretera: " + records.Count + ".\n" +
                "Raycasts sobre superficie: " + checks + ", correctos. Triángulos sin degeneraciones.\n" +
                "Ramal central: " + middle.ToString("F1") + " m; lateral interior: " + inside.ToString("F1") + " m; lateral exterior: " + outside.ToString("F1") + " m.\n" +
                "Saltos: dos, huecos de 3 m y 3.5 m. Peralte máximo: 9 grados. Puente: 8 m de altura.\n" +
                "Trampas y comportamiento de salto del kart pendientes de ajuste en Play Mode.\n");
        }

        static void Preview(Scene scene, Transform circuit)
        {
            var ceilingGroup = circuit.Find("03_Techos_Ocultar_Para_Editar");
            ceilingGroup.gameObject.SetActive(false);
            var cameraObject = new GameObject("PreviewCamera_TEMP"); SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
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
            }
            finally { ceilingGroup.gameObject.SetActive(true); Object.DestroyImmediate(cameraObject); }
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

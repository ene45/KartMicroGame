using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MedievalCartoon.Editor
{
    public static class LavaLandmarkBuilder
    {
        public const string F = "Assets/MedievalCartoon";
        public const string PrefabFolder = F + "/Prefabs/Anillo_Y_Guardianes";
        public const string CatalogPath = F + "/Scenes/Catalogo_Anillo_Y_Guardianes.unity";
        public static readonly string[] Guardians = { "Guardian_Martillo_Gris", "Guardian_Espada_Gris", "Guardian_Escudo_Gris", "Guardian_Llave_Gris" };
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(F + "/Materials/" + name + ".mat");
        static Mesh Mesh(string name) => AssetDatabase.LoadAssetAtPath<Mesh>(F + "/Meshes/" + name + ".asset");
        public static GameObject Prefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + name + ".prefab");

        // Explicit build only. Importing the package never rebuilds or modifies a user's scene.
        public static void Build()
        {
            Directory.CreateDirectory(PrefabFolder);
            foreach (string name in new[] { "Piedra_Gris_Tenebrosa", "Lava_Corriente" })
            {
                string path = F + "/Textures/" + name + ".png";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.wrapMode = TextureWrapMode.Repeat; importer.sRGBTexture = true;
                importer.maxTextureSize = 2048; importer.SaveAndReimport();
            }
            CreateMaterial("Piedra_Gris", "Piedra_Gris_Tenebrosa", new Color(.85f, .85f, .86f));
            CreateMaterial("Piedra_Gris_Clara", "Piedra_Gris_Tenebrosa", new Color(1, 1, 1));
            CreateMaterial("Hierro_Gris", null, new Color(.20f, .22f, .25f), .65f);
            CreateMaterial("Lava_Naranja", "Lava_Corriente", Color.white, 0, new Color(1, .32f, .045f) * .9f);
            Mat("Lava_Naranja").SetFloat("_Cull", 0);
            StoreMesh("Eslabon_Grueso", LavaLandmarkMeshes.Link());
            StoreMesh("Cascada_Lava", LavaLandmarkMeshes.Waterfall());
            StoreMesh("Hoja_Espada", MedievalMeshes.Extrude("Hoja_Espada", new[] {
                new Vector2(-.65f, 0), new Vector2(.65f, 0), new Vector2(.55f, 7.4f), new Vector2(0, 9.8f), new Vector2(-.55f, 7.4f) }, .4f));
            BuildCenter();
            for (int i = 0; i < Guardians.Length; i++) BuildGuardian(i);
            BuildCatalog();
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("LAVA_LANDMARKS_BUILT: 9 prefabs, four gray guardians, modular ring and chains.");
        }
        static void StoreMesh(string name, Mesh mesh)
        {
            string path = F + "/Meshes/" + name + ".asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old) { EditorUtility.CopySerialized(mesh, old); Object.DestroyImmediate(mesh); }
            else AssetDatabase.CreateAsset(mesh, path);
        }
        static void CreateMaterial(string name, string texture, Color color, float metal = 0, Color emission = default)
        {
            string path = F + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name }; AssetDatabase.CreateAsset(mat, path); }
            mat.SetColor("_BaseColor", color); mat.SetFloat("_Metallic", metal); mat.SetFloat("_Smoothness", .2f);
            mat.enableInstancing = true;
            if (texture != null) mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(F + "/Textures/" + texture + ".png"));
            if (emission.maxColorComponent > 0)
            {
                mat.SetColor("_EmissionColor", emission);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                MaterialEditor.FixupEmissiveFlag(mat); mat.EnableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(mat);
        }
        static GameObject Group(Transform parent, string name)
        {
            var obj = new GameObject(name); if (parent) obj.transform.SetParent(parent, false); return obj;
        }
        static GameObject Part(Transform p, string name, string mesh, string material, Vector3 pos, Vector3 scale)
        {
            var obj = Group(p, name); obj.transform.localPosition = pos; obj.transform.localScale = scale;
            obj.AddComponent<MeshFilter>().sharedMesh = Mesh(mesh);
            obj.AddComponent<MeshRenderer>().sharedMaterial = Mat(material);
            return obj;
        }
        static GameObject Box(Transform p, string name, Vector3 pos, Vector3 scale, string mat = "Piedra_Gris") => Part(p, name, "Bloque", mat, pos, scale);
        static void Save(GameObject obj)
        {
            PrefabUtility.SaveAsPrefabAsset(obj, PrefabFolder + "/" + obj.name + ".prefab"); Object.DestroyImmediate(obj);
        }
        static GameObject Instance(string name, Transform parent, Vector3 pos)
        {
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(name), parent);
            obj.transform.localPosition = pos; return obj;
        }
        static void Flow(GameObject obj)
        {
            obj.AddComponent<LavaFlowVisual>().surfaces = obj.GetComponentsInChildren<MeshRenderer>().Where(r => r.sharedMaterial == Mat("Lava_Naranja")).Cast<Renderer>().ToArray();
        }
        static void Chain(Transform p, string name, Vector3 start, Vector3 end, float sag)
        {
            var group = Group(p, name).transform;
            int count = Mathf.CeilToInt(Vector3.Distance(start, end) / 1.52f);
            Func<float, Vector3> point = t => Vector3.Lerp(start, end, t) - Vector3.up * (4 * sag * t * (1 - t));
            for (int i = 0; i <= count; i++)
            {
                float t = (float)i / count;
                var obj = Part(group, "Eslabon_" + i.ToString("000"), "Eslabon_Grueso", "Hierro_Gris", point(t), Vector3.one);
                var tangent = point(Mathf.Min(1, t + .001f)) - point(Mathf.Max(0, t - .001f));
                obj.transform.localRotation = Quaternion.FromToRotation(Vector3.up, tangent.normalized) * Quaternion.Euler(0, i % 2 * 90, 0);
            }
        }
        static void BuildCenter()
        {
            var link = Group(null, "Eslabon_Cadena"); Part(link.transform, "Hierro", "Eslabon_Grueso", "Hierro_Gris", Vector3.zero, Vector3.one); Save(link);
            var module = Group(null, "Cadena_Modulo_8m"); Chain(module.transform, "Eslabones", Vector3.zero, V(0, 8, 0), 0); Save(module);
            var ring = Group(null, "Anillo_Suspendido_Lava");
            Part(ring.transform, "Borde_Exterior", "Anillo", "Piedra_Gris", V(0, 28, 0), V(36, 3.5f, 36));
            Part(ring.transform, "Cornisa_Superior", "Anillo", "Piedra_Gris_Clara", V(0, 30, 0), V(36.8f, .9f, 36.8f));
            Part(ring.transform, "Cornisa_Inferior", "Anillo", "Piedra_Gris_Clara", V(0, 25.8f, 0), V(35.8f, 1.2f, 35.8f));
            Part(ring.transform, "Canal_Lava", "Anillo", "Lava_Naranja", V(0, 30.6f, 0), V(34.8f, .25f, 34.8f));
            for (int i = 0; i < 10; i++)
            {
                var fall = Part(ring.transform, "Cascada_" + (i + 1).ToString("00"), "Cascada_Lava", "Lava_Naranja", V(0, 1.2f, 0), V(32.2f, 29.4f, 32.2f));
                fall.transform.localRotation = Quaternion.Euler(0, i * 36, 0);
                float a = (i * 36 + 18) * Mathf.Deg2Rad;
                var brace = Box(ring.transform, "Contrafuerte_" + i, V(Mathf.Sin(a) * 34.4f, 28.4f, Mathf.Cos(a) * 34.4f), V(3.4f, 5.5f, 6), "Piedra_Gris_Clara");
                brace.transform.localRotation = Quaternion.Euler(0, i * 36 + 18, 0);
            }
            ring.transform.localScale = V(1.24f, 1, 1.24f);
            Flow(ring); Save(ring);
            var basin = Group(null, "Cuenca_Lava_Circular");
            Part(basin.transform, "Contencion_Piedra", "Anillo", "Piedra_Gris", V(0, 1, 0), V(39, 2, 39));
            Part(basin.transform, "Lava_Superficie", "Cilindro", "Lava_Naranja", V(0, .75f, 0), V(36.4f, .12f, 36.4f));
            Part(basin.transform, "Borde_Claro", "Anillo", "Piedra_Gris_Clara", V(0, 2.1f, 0), V(39.3f, .35f, 39.3f));
            basin.transform.localScale = V(1.24f, 1, 1.24f);
            Flow(basin); basin.GetComponent<LavaFlowVisual>().flowSpeed = .025f; Save(basin);
            var center = Group(null, "Anillo_Lava_Cadenas");
            Instance("Anillo_Suspendido_Lava", center.transform, Vector3.zero);
            Instance("Cuenca_Lava_Circular", center.transform, Vector3.zero);
            var chains = Group(center.transform, "Cadenas_Mover_Anclajes_Como_Grupos").transform;
            Vector3[] starts = { V(-31.5f, 30, 31.5f), V(31.5f, 30, 31.5f), V(31.5f, 30, -31.5f), V(-31.5f, 30, -31.5f) };
            // The track floor is at -4.5 m. Leave room for the last link below the roof.
            Vector3[] ends = { V(-8, 63.1f, 118), V(115, 63.1f, 112), V(123, 63.1f, -95), V(-123, 63.1f, -116) };
            for (int i = 0; i < starts.Length; i++)
            {
                Chain(chains, "Cadena_" + (i + 1), starts[i], ends[i], 3);
                Box(chains, "Anclaje_Techo_" + (i + 1), ends[i], V(4, 2.8f, 4));
            }
            Save(center);
        }
        static GameObject GrayCopy(string source, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(F + "/Prefabs/Landmark/" + source + ".prefab");
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var collider in obj.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            foreach (var r in obj.GetComponentsInChildren<MeshRenderer>(true))
            {
                r.sharedMaterials = r.sharedMaterials.Select(m => m.name == "Negro" ? Mat("Negro") : m.name == "Oro" ? Mat("Hierro_Gris") : Mat("Piedra_Gris")).ToArray();
            }
            return obj;
        }
        static void BuildGuardian(int variant)
        {
            var root = Group(null, Guardians[variant]);
            GrayCopy("Pedestal_Gran_Guardian", root.transform);
            var statue = GrayCopy("Estatua_Gran_Guardian", root.transform).transform;
            statue.name = "Estatua_Editar_Piezas";
            if (variant > 0)
            {
                foreach (Transform part in statue.Cast<Transform>().ToArray())
                    if (part.name == "Mango_Martillo" || part.name == "Cabeza_Martillo" || part.name == "Refuerzo_Martillo") Object.DestroyImmediate(part.gameObject);
            }
            if (variant == 1)
            {
                Part(statue, "Espada_Hoja", "Hoja_Espada", "Piedra_Gris_Clara", V(-6.8f, 18, 1), Vector3.one);
                Box(statue, "Espada_Guarda", V(-6.8f, 17.8f, 1), V(4.4f, .6f, 1.1f));
                Box(statue, "Espada_Mango", V(-6.8f, 16.2f, 1), V(.6f, 2.8f, .6f), "Hierro_Gris");
            }
            if (variant == 2)
            {
                var shield = statue.Find("Escudo"); shield.localScale = Vector3.one * 1.4f;
                shield.localPosition = V(4.5f, 12, 2.7f);
                var forearm = statue.Find("Antebrazo_Martillo");
                Vector3 a = V(-5.5f, 12.5f, .35f), b = V(-6.7f, 10.8f, 1);
                forearm.localPosition = (a + b) / 2; forearm.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
                forearm.localScale = V(.82f, (b - a).magnitude, .82f);
                statue.Find("Guante_Martillo").localPosition = b;
                foreach (Transform part in statue.Cast<Transform>().ToArray()) if (part.name.StartsWith("Dedos_")) Object.DestroyImmediate(part.gameObject);
                Box(statue, "Escudo_Lanza_Corta", V(-6.7f, 10.5f, 1), V(.6f, 14, .6f));
            }
            if (variant == 3)
            {
                var loop = Part(statue, "Llave_Anillo", "Anillo", "Piedra_Gris_Clara", V(-6.8f, 24.2f, 1), V(3.2f, .7f, 3.2f));
                loop.transform.localRotation = Quaternion.Euler(90, 0, 0);
                Box(statue, "Llave_Mango", V(-6.8f, 17.4f, 1), V(.75f, 9.1f, .75f));
                Box(statue, "Llave_Diente_1", V(-5.3f, 14.1f, 1), V(2.8f, .8f, .85f));
                Box(statue, "Llave_Diente_2", V(-5.3f, 16, 1), V(2.8f, .8f, .85f));
            }
            var glow = Group(root.transform, "Luz_Para_Leer_Silueta").AddComponent<Light>();
            glow.type = LightType.Spot; glow.range = 34; glow.spotAngle = 100; glow.innerSpotAngle = 75;
            glow.intensity = 7; glow.color = new Color(.85f, .9f, 1); glow.shadows = LightShadows.None;
            glow.transform.localPosition = V(0, 19, 14); glow.transform.LookAt(root.transform.TransformPoint(V(0, 11, 0)));
            Save(root);
        }
        static void BuildCatalog()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetLighting(); var root = Group(null, "CATALOGO_ANILLO_Y_GUARDIANES");
            Instance("Anillo_Lava_Cadenas", root.transform, Vector3.zero);
            for (int i = 0; i < 4; i++) Instance(Guardians[i], root.transform, V(-39 + i * 26, 0, 125));
            EditorSceneManager.SaveScene(scene, CatalogPath);
        }
        static void SetLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.45f, .47f, .52f); RenderSettings.fog = false;
            var light = Group(null, "Luz_Catalogo").AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 1.5f; light.color = new Color(.93f, .95f, 1); light.transform.rotation = Quaternion.Euler(45, 145, 0);
        }
        public static Camera PreviewCamera(Scene scene, Vector3 position, Vector3 target, float fov = 50)
        {
            var camera = Group(null, "Camara_Preview_Temporal").AddComponent<Camera>();
            SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
            camera.scene = scene; camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.transform.position = position; camera.transform.LookAt(target);
            camera.fieldOfView = fov; camera.farClipPlane = 1000;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.055f, .075f, .10f);
            camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false; return camera;
        }
        public static void Render(Camera camera, string path, int width = 1600, int height = 1000)
        {
            var rt = new RenderTexture(width, height, 24); var previous = RenderTexture.active;
            try
            {
                rt.Create(); camera.targetTexture = rt;
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
                RenderTexture.active = rt; var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            }
            finally { camera.targetTexture = null; RenderTexture.active = previous; rt.Release(); Object.DestroyImmediate(rt); }
        }
        public static void CaptureAndExport()
        {
            string output = Path.GetFullPath("../../outputs"); Directory.CreateDirectory(output);
            var scene = EditorSceneManager.OpenScene(CatalogPath);
            var camera = PreviewCamera(scene, V(-111, 105, 156), V(0, 19, 0), 60);
            foreach (var obj in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>()).Where(t => Guardians.Contains(t.name))) obj.gameObject.SetActive(false);
            Render(camera, output + "/Anillo_Lava_Cadenas.png"); Render(camera, output + "/Anillo_Lava_Cadenas.png");
            foreach (var obj in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Where(t => Guardians.Contains(t.name))) obj.gameObject.SetActive(true);
            var ring = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>()).First(t => t.name == "Anillo_Lava_Cadenas"); ring.gameObject.SetActive(false);
            camera.transform.position = V(12, 26, 199); camera.transform.LookAt(V(0, 12, 125)); camera.fieldOfView = 64;
            Render(camera, output + "/Guardianes_Esquinas.png"); Render(camera, output + "/Guardianes_Esquinas.png");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            AssetDatabase.ExportPackage(F, output + "/Anillo_Lava_Cadenas.unitypackage", ExportPackageOptions.Recurse);
            Debug.Log("LAVA_PREVIEWS_AND_PACKAGE_READY");
        }
    }
}

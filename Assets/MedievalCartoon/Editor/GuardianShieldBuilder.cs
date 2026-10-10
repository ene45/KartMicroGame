using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MedievalCartoon.Editor
{
    public static class GuardianShieldBuilder
    {
        const string F = LavaLandmarkBuilder.F;
        public const string PrefabPath = F + "/Prefabs/Decoracion/Escudo_Guardian_Cetro.prefab";
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(F + "/Materials/" + name + ".mat");
        static GameObject Group(Transform parent, string name)
        {
            var obj = new GameObject(name); if (parent) obj.transform.SetParent(parent, false); return obj;
        }
        static GameObject Part(Transform parent, string name, string mesh, string material, Vector3 position, Vector3 scale)
        {
            var obj = Group(parent, name); obj.transform.localPosition = position; obj.transform.localScale = scale;
            obj.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(F + "/Meshes/" + mesh + ".asset");
            obj.AddComponent<MeshRenderer>().sharedMaterial = Mat(material); return obj;
        }
        static GameObject Box(Transform p, string name, string material, Vector3 pos, Vector3 size) => Part(p, name, "Bloque", material, pos, size);
        static void Beam(Transform p, string name, Vector3 a, Vector3 b, float radius, string material)
        {
            var obj = Part(p, name, "Cilindro", material, (a + b) / 2, V(radius, (b - a).magnitude, radius));
            obj.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
        }
        static void MaterialAsset(string name, Color color, float metal = 0, bool stone = false)
        {
            string path = F + "/Materials/" + name + ".mat";
            var mat = Mat(name);
            if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name }; AssetDatabase.CreateAsset(mat, path); }
            mat.SetColor("_BaseColor", color); mat.SetFloat("_Metallic", metal); mat.SetFloat("_Smoothness", .3f);
            if (stone) mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(F + "/Textures/Piedra_Gris_Tenebrosa.png"));
            mat.enableInstancing = true; EditorUtility.SetDirty(mat);
        }
        static void MeshAsset(string name, Mesh mesh)
        {
            string path = F + "/Meshes/" + name + ".asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old) { EditorUtility.CopySerialized(mesh, old); Object.DestroyImmediate(mesh); }
            else AssetDatabase.CreateAsset(mesh, path);
        }
        public static void Build()
        {
            // Independent asset: importing or refreshing never regenerates a user's scene.
            Vector2[] contour = {
                new Vector2(-3.25f,6.7f),new Vector2(-2.65f,6.48f),new Vector2(-1.9f,6.35f),
                new Vector2(-1.05f,6.33f),new Vector2(-.5f,6.42f),new Vector2(0,6.66f),
                new Vector2(.5f,6.42f),new Vector2(1.05f,6.33f),new Vector2(1.9f,6.35f),
                new Vector2(2.65f,6.48f),new Vector2(3.25f,6.7f),new Vector2(3.18f,6.02f),
                new Vector2(2.9f,5.12f),new Vector2(2.5f,4.08f),new Vector2(2.15f,3.0f),
                new Vector2(1.8f,2.02f),new Vector2(1.45f,1.23f),new Vector2(1.03f,.58f),
                new Vector2(.55f,.18f),new Vector2(0,0),new Vector2(-.55f,.18f),
                new Vector2(-1.03f,.58f),new Vector2(-1.45f,1.23f),new Vector2(-1.8f,2.02f),
                new Vector2(-2.15f,3.0f),new Vector2(-2.5f,4.08f),new Vector2(-2.9f,5.12f),new Vector2(-3.18f,6.02f)
            };
            Array.Reverse(contour); // Counter-clockwise contour gives outward side normals.
            MeshAsset("Escudo_Contorno_Guardian", MedievalMeshes.Extrude("Escudo_Contorno_Guardian", contour, .38f));
            MeshAsset("Cetro_Hoja_Ornamental", MedievalMeshes.Extrude("Cetro_Hoja_Ornamental", new[] {
                new Vector2(0,-.65f),new Vector2(.17f,-.3f),new Vector2(.16f,0),new Vector2(0,.25f),new Vector2(-.16f,0),new Vector2(-.17f,-.3f)}, .14f));
            MeshAsset("Cetro_Voluta", MedievalMeshes.Arc(.23f,.085f,180,.16f));
            MaterialAsset("Escudo_Fondo_Gris", new Color(.36f,.38f,.42f), 0, true);
            MaterialAsset("Escudo_Marco_Gris", new Color(.42f,.46f,.52f), .2f);
            MaterialAsset("Cetro_Plata_Gris", new Color(.72f,.77f,.84f), .3f);
            MaterialAsset("Cetro_Zafiro", new Color(.035f,.17f,.48f), .15f);
            var root = Group(null, "Escudo_Guardian_Cetro");
            try
            {
                var shield = Group(root.transform, "01_Escudo_Fondo_Y_Marco").transform;
                Part(shield,"Marco_Contorno","Escudo_Contorno_Guardian","Escudo_Marco_Gris",Vector3.zero,Vector3.one);
                Part(shield,"Panel_Piedra_Oscura","Escudo_Contorno_Guardian","Escudo_Fondo_Gris",V(0,.26f,.27f),V(.90f,.92f,.65f));
                BuildBust(Group(root.transform,"02_Guardian_En_Relieve").transform);
                BuildArms(Group(root.transform,"03_Brazos_Y_Dos_Manos").transform);
                BuildScepter(Group(root.transform,"04_Cetro_Hacia_Abajo").transform);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("GUARDIAN_SHIELD_BUILT: " + PrefabPath);
        }
        static void BuildBust(Transform parent)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(F + "/Prefabs/Landmark/Estatua_Gran_Guardian.prefab");
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            obj.name = "Busto_Armadura_Gris";
            string[] removed = {"Bota_","Pierna_","Rodillera_","Cintura","Cinturon","Hebilla","Brazo_","Antebrazo_","Codo_","Guante_","Dedos_","Mango_","Cabeza_Martillo","Refuerzo_Martillo","Escudo"};
            foreach (Transform part in obj.transform.Cast<Transform>().ToArray())
                if (removed.Any(prefix => part.name.StartsWith(prefix, StringComparison.Ordinal))) Object.DestroyImmediate(part.gameObject);
            foreach (var collider in obj.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            foreach (var renderer in obj.GetComponentsInChildren<MeshRenderer>(true))
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => Mat(m.name == "Negro" ? "Negro" : m.name == "Oro" ? "Hierro_Gris" : "Piedra_Gris_Clara")).ToArray();
            obj.transform.localPosition = V(0,-1.2f,.60f); obj.transform.localScale = V(.38f,.38f,.12f);
        }
        static void BuildArms(Transform p)
        {
            Vector3[] shoulder = { V(-1.3f,4.0f,.63f), V(1.3f,4.0f,.63f) };
            Vector3[] elbow = { V(-1.15f,3.15f,.79f), V(1.1f,3.43f,.79f) };
            Vector3[] hand = { V(-.15f,3.10f,1.06f), V(.15f,3.46f,1.06f) };
            for (int i=0;i<2;i++)
            {
                string side = i == 0 ? "Izquierda" : "Derecha";
                Beam(p,"Brazo_"+side,shoulder[i],elbow[i],.27f,"Piedra_Gris_Clara");
                Part(p,"Codo_"+side,"Esfera","Piedra_Gris_Clara",elbow[i],Vector3.one*.3f);
                Beam(p,"Antebrazo_"+side,elbow[i],hand[i],.25f,"Piedra_Gris_Clara");
                var grip = Group(p,"Mano_"+side).transform; grip.localPosition=hand[i];
                Box(grip,"Palma_Sujetando_Cetro","Piedra_Gris_Clara",Vector3.zero,V(.42f,.30f,.32f));
                for(int finger=0;finger<3;finger++)
                    Box(grip,"Dedo_"+(finger+1),"Piedra_Gris",V(i==0?.085f:-.085f,-.085f+finger*.082f,.18f),V(.27f,.062f,.095f));
                Box(grip,"Pulgar","Piedra_Gris_Clara",V(i==0?.11f:-.11f,.17f,.035f),V(.16f,.16f,.23f));
            }
        }
        static void BuildScepter(Transform p)
        {
            Beam(p,"Vara_Vertical",V(0,.92f,1.08f),V(0,3.88f,1.08f),.066f,"Cetro_Plata_Gris");
            Part(p,"Remate_Superior","Esfera","Cetro_Plata_Gris",V(0,3.91f,1.08f),Vector3.one*.095f);
            var ornament = Group(p,"Ornamento_Inferior_Flor_De_Lis").transform;
            ornament.localPosition = V(0,.9f,1.08f);
            Part(ornament,"Hoja_Punta_Hacia_Abajo","Cetro_Hoja_Ornamental","Cetro_Plata_Gris",Vector3.zero,Vector3.one);
            var top = Part(ornament,"Hoja_Central_Superior","Cetro_Hoja_Ornamental","Cetro_Plata_Gris",V(0,.38f,0),V(.65f,.68f,1));
            top.transform.localRotation = Quaternion.Euler(0,0,180);
            foreach (int sign in new[]{-1,1})
            {
                var scroll = Part(ornament,"Voluta_"+sign,"Cetro_Voluta","Cetro_Plata_Gris",V(sign*.27f,0,0),Vector3.one);
                scroll.transform.localRotation = Quaternion.Euler(0,0,180);
                var leaf = Part(ornament,"Hoja_Lateral_"+sign,"Cetro_Hoja_Ornamental","Cetro_Plata_Gris",V(sign*.19f,.34f,0),V(.57f,.55f,1));
                leaf.transform.localRotation = Quaternion.Euler(0,0,180-sign*42);
            }
            Box(ornament,"Anillo_Central","Cetro_Plata_Gris",V(0,.045f,.015f),V(.36f,.13f,.20f));
            Part(ornament,"Gema_Azul","Esfera","Cetro_Zafiro",V(0,.075f,.17f),V(.09f,.13f,.065f));
        }
        [MenuItem("Tools/Assets de Mazmorra/Escudo del guardián/Mostrar prefab")]
        public static void ShowPrefab()
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            EditorUtility.FocusProjectWindow(); EditorGUIUtility.PingObject(Selection.activeObject);
        }
        [MenuItem("Tools/Assets de Mazmorra/Escudo del guardián/Agregar a escena")]
        public static void AddToScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(obj,"Agregar escudo del guardián");
            Selection.activeGameObject=obj; SceneView.lastActiveSceneView?.FrameSelected();
        }
        public static void BuildAndCapture()
        {
            Build(); CaptureAndExport();
        }
        public static void CaptureAndExport()
        {
            string output=Path.GetFullPath("../../outputs");Directory.CreateDirectory(output);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.52f,.57f);RenderSettings.fog=false;
            var key=Group(null,"Luz_Principal").AddComponent<Light>();key.type=LightType.Directional;key.intensity=2;
            key.color=new Color(.94f,.97f,1);key.transform.rotation=Quaternion.Euler(35,155,0);
            var fill=Group(null,"Luz_Relleno").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.7f;
            fill.transform.rotation=Quaternion.Euler(15,-140,0);
            var camera=LavaLandmarkBuilder.PreviewCamera(scene,V(0,3.35f,14),V(0,3.35f,0));
            camera.orthographic=true;camera.orthographicSize=3.9f;
            LavaLandmarkBuilder.Render(camera,output+"/Escudo_Guardian_Cetro_Frontal.png",1400,1400);
            LavaLandmarkBuilder.Render(camera,output+"/Escudo_Guardian_Cetro_Frontal.png",1400,1400);
            camera.transform.position=V(8,5.6f,14);camera.transform.LookAt(V(0,3.35f,.3f));camera.orthographicSize=3.9f;
            LavaLandmarkBuilder.Render(camera,output+"/Escudo_Guardian_Cetro_Relieve.png",1400,1400);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            var paths=AssetDatabase.GetDependencies(PrefabPath,true).Where(p=>p.StartsWith(F+"/",StringComparison.Ordinal)).ToArray();
            AssetDatabase.ExportPackage(paths,output+"/Escudo_Guardian_Cetro.unitypackage",ExportPackageOptions.Default);
            Debug.Log("GUARDIAN_SHIELD_PREVIEWS_READY: "+paths.Length+" included assets");
        }
    }
}

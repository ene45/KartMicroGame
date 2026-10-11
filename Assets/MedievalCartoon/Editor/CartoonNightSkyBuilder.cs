using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MedievalCartoon.Editor
{
    public static class CartoonNightSkyBuilder
    {
        public const string Folder = "Assets/MedievalCartoon/Skies";
        public const string MaterialPath = Folder + "/Cielo_Nocturno_Cartoon.mat";
        public const string PrefabPath = Folder + "/Noche_Cartoon_Con_Luna.prefab";
        const string RequestPath = Folder + "/AplicarNoche.request";
        const string ExamplePath = "Assets/DungeonTrack/DungeonCircuit_AnilloLava 1.unity";

        [MenuItem("Tools/Assets de Mazmorra/Cielo nocturno/Aplicar a la escena actual")]
        public static void ApplyCurrent()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Salí de Play Mode para aplicar el cielo.");
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Cielo nocturno y luz de luna");
            Apply(SceneManager.GetActiveScene()); Undo.CollapseUndoOperations(group);
        }

        public static void BuildExample()
        {
            var scene = EditorSceneManager.OpenScene(ExamplePath,OpenSceneMode.Single);
            var sky = Apply(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("No se pudo guardar el circuito nocturno.");
            AssetDatabase.SaveAssets();
            Debug.Log("CARTOON_NIGHT_READY: " + scene.path + "; moon=" + sky.moonLight.transform.eulerAngles);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }

        public static CartoonNightSky Apply(Scene scene)
        {
            if (scene != SceneManager.GetActiveScene()) throw new InvalidOperationException("Activá primero la escena a la que querés aplicar el cielo.");
            var roots = scene.GetRootGameObjects();
            var existing = roots.SelectMany(g=>g.GetComponentsInChildren<CartoonNightSky>(true)).FirstOrDefault();
            if (existing != null)
            {
                HideDaySunDisc(existing.moonLight);ConfigureCameras(existing,roots);existing.Apply();RecordCameraOverrides(existing);
                PrefabUtility.RecordPrefabInstancePropertyModifications(existing);
                EditorSceneManager.MarkSceneDirty(scene);Selection.activeGameObject=existing.gameObject;return existing;
            }
            var oldSun = RenderSettings.sun;
            if (oldSun == null || oldSun.gameObject.scene != scene || !oldSun.isActiveAndEnabled || oldSun.type != LightType.Directional)
                oldSun = roots.SelectMany(g=>g.GetComponentsInChildren<Light>()).FirstOrDefault(l=>l.isActiveAndEnabled && l.type==LightType.Directional);
            EnsureAssets();
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
            root.name="CIELO_NOCTURNO_LUNA"; Undo.RegisterCreatedObjectUndo(root,"Agregar cielo nocturno");
            var sky=root.GetComponent<CartoonNightSky>();
            string profileFolder=Folder+"/Perfiles";
            if(!AssetDatabase.IsValidFolder(profileFolder))AssetDatabase.CreateFolder(Folder,"Perfiles");
            string sceneName=string.Join("_",scene.name.Split(Path.GetInvalidFileNameChars())).Replace(' ','_');
            string profile=AssetDatabase.GenerateUniqueAssetPath(profileFolder+"/Noche_"+sceneName+".mat");
            var material=new Material(AssetDatabase.LoadAssetAtPath<Material>(MaterialPath)) {name="Noche_"+sceneName};
            AssetDatabase.CreateAsset(material,profile); sky.skyboxMaterial=material;
            if(oldSun!=null)
            {
                var temporary=sky.moonLight; sky.moonLight=oldSun;
                Undo.DestroyObjectImmediate(temporary.gameObject);
            }
            Undo.RecordObjects(new Object[]{sky.moonLight,sky.moonLight.transform,sky.moonLight.gameObject},"Configurar luz lunar");
            sky.moonLight.name="Luna_Directional_Light";
            sky.moonLight.transform.rotation=Quaternion.Euler(38,145,0);
            sky.moonLight.color=new Color(.62f,.73f,.95f);
            sky.moonLight.intensity=1.0f; sky.moonLight.shadows=LightShadows.Soft; sky.moonLight.shadowStrength=.75f;
            HideDaySunDisc(sky.moonLight);ConfigureCameras(sky,roots);sky.Apply();RecordCameraOverrides(sky);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sky);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sky.moonLight);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sky.moonLight.transform);
            EditorSceneManager.MarkSceneDirty(scene); Selection.activeGameObject=root;
            return sky;
        }

        static void HideDaySunDisc(Light light)
        {
            if(light==null)return;
            // The Microgame's sun is an emissive mesh attached to the light, not its skybox.
            foreach(var renderer in light.GetComponentsInChildren<Renderer>(true))
                if(renderer.sharedMaterials.Any(m=>m!=null && AssetDatabase.GetAssetPath(m)=="Assets/Karting/Art/Materials/Level/Sun.mat"))
                {
                    Undo.RecordObject(renderer,"Reemplazar disco de sol por luna");renderer.enabled=false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
        }
        static void ConfigureCameras(CartoonNightSky sky,GameObject[] roots)
        {
            var cameraSkies=new System.Collections.Generic.List<Skybox>();
            foreach(var camera in roots.SelectMany(g=>g.GetComponentsInChildren<Camera>(true)))
                if(camera.cameraType==CameraType.Game && camera.clearFlags!=CameraClearFlags.Depth && camera.clearFlags!=CameraClearFlags.Nothing)
                {
                    Undo.RecordObject(camera,"Mostrar cielo nocturno"); camera.clearFlags=CameraClearFlags.Skybox;
                    var cameraSky=camera.GetComponent<Skybox>();
                    if(cameraSky==null)cameraSky=Undo.AddComponent<Skybox>(camera.gameObject);
                    Undo.RecordObject(cameraSky,"Asignar cielo nocturno a la cámara"); cameraSkies.Add(cameraSky);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
                }
            Undo.RecordObject(sky,"Configurar cámaras del cielo nocturno");sky.cameraSkyboxes=cameraSkies.ToArray();
        }
        static void RecordCameraOverrides(CartoonNightSky sky)
        {
            foreach(var box in sky.cameraSkyboxes)if(box!=null)PrefabUtility.RecordPrefabInstancePropertyModifications(box);
        }

        public static void EnsureAssets()
        {
            var shader=Shader.Find("Medieval Cartoon/Night Sky");
            if(shader==null)throw new InvalidOperationException("No se importó el shader del cielo nocturno.");
            var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if(material==null){material=new Material(shader){name="Cielo_Nocturno_Cartoon"};AssetDatabase.CreateAsset(material,MaterialPath);}
            if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)!=null)return;
            var root=new GameObject("Noche_Cartoon_Con_Luna"); root.SetActive(false);
            try
            {
                var child=new GameObject("Luna_Directional_Light");child.transform.SetParent(root.transform,false);
                child.transform.localRotation=Quaternion.Euler(38,145,0);
                var light=child.AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(.62f,.73f,.95f);
                light.intensity=1;light.shadows=LightShadows.Soft;light.shadowStrength=.75f;
                var sky=root.AddComponent<CartoonNightSky>();sky.skyboxMaterial=material;sky.moonLight=light;
                root.SetActive(true);PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally{Object.DestroyImmediate(root);}
            AssetDatabase.SaveAssets();
        }

        [InitializeOnLoadMethod]
        static void QueueRequestedInstall(){EditorApplication.delayCall+=InstallRequested;}
        static void InstallRequested()
        {
            if(!File.Exists(RequestPath))return;
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {EditorApplication.delayCall+=InstallRequested;return;}
            // Apply to the loaded scene, preserving even unsaved geometry edits.
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=ExamplePath){Debug.Log("Abrí DungeonCircuit_AnilloLava 1 y usá Tools > Assets de Mazmorra > Cielo nocturno > Aplicar a la escena actual.");return;}
            AssetDatabase.DeleteAsset(RequestPath);
            ApplyCurrent();EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("CARTOON_NIGHT_INSTALLED: cielo nocturno y Directional Light lunar configurables.");
        }

        public static void Capture()
        {
            var scene=EditorSceneManager.OpenScene(ExamplePath,OpenSceneMode.Single);
            var sky=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CartoonNightSky>(true)).Single();sky.Apply();
            string output=Path.GetFullPath("../../outputs");Directory.CreateDirectory(output);
            var camera=LavaLandmarkBuilder.PreviewCamera(scene,new Vector3(-148,18,135),new Vector3(-35,14,68),70);
            camera.clearFlags=CameraClearFlags.Skybox;
            camera.gameObject.AddComponent<Skybox>().material=sky.AppliedMaterial;
            Debug.Log("NIGHT_CAPTURE_SKY: "+sky.AppliedMaterial.shader.name+"; radius="+sky.AppliedMaterial.GetFloat("_MoonSize")+"; moon="+sky.AppliedMaterial.GetColor("_MoonColor"));
            LavaLandmarkBuilder.Render(camera,output+"/Circuito_Noche_Cartoon.png");LavaLandmarkBuilder.Render(camera,output+"/Circuito_Noche_Cartoon.png");
            Vector3 moon=-sky.moonLight.transform.forward;
            camera.transform.position=new Vector3(-100,3,100);camera.transform.LookAt(camera.transform.position+moon);camera.fieldOfView=80;
            LavaLandmarkBuilder.Render(camera,output+"/Cielo_Noche_Cartoon.png");LavaLandmarkBuilder.Render(camera,output+"/Cielo_Noche_Cartoon.png");
            Object.DestroyImmediate(camera.gameObject);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }

        public static void CaptureSkyAlone()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
            var sky=obj.GetComponent<CartoonNightSky>();sky.Apply();
            var camera=LavaLandmarkBuilder.PreviewCamera(scene,Vector3.zero,-sky.moonLight.transform.forward,80);
            camera.clearFlags=CameraClearFlags.Skybox;camera.gameObject.AddComponent<Skybox>().material=sky.AppliedMaterial;
            string output=Path.GetFullPath("../../outputs/Cielo_Noche_Cartoon_Limpio.png");
            LavaLandmarkBuilder.Render(camera,output);LavaLandmarkBuilder.Render(camera,output);
            Object.DestroyImmediate(camera.gameObject);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
    }
}

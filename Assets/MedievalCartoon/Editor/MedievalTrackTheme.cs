using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MedievalCartoon.Editor
{
    public static class MedievalTrackTheme
    {
        public const string ScenePath="Assets/DungeonTrack/DungeonCircuit_MedievalCartoon.unity";
        const string F=MedievalPackBuilder.Folder;

        public static void CreateExample()
        {
            string source="Assets/DungeonTrack/DungeonCircuit_Rocas.unity";
            if(!File.Exists(source))throw new FileNotFoundException("Falta la escena con rocas.",source);
            string destination=AssetDatabase.GenerateUniqueAssetPath(ScenePath);
            if(!AssetDatabase.CopyAsset(source,destination))throw new IOException("No se pudo copiar la escena.");
            string content=Regex.Replace(File.ReadAllText(destination),@"(m_Id:\s*)[0-9a-fA-F-]{36}",m=>m.Groups[1].Value+Guid.NewGuid());
            File.WriteAllText(destination,content);AssetDatabase.ImportAsset(destination,ImportAssetOptions.ForceUpdate);
            var scene=EditorSceneManager.OpenScene(destination,OpenSceneMode.Single);
            Apply(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("MEDIEVAL_TRACK_CREATED: "+destination);
        }
        public static void UpdateGeneratedExample()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var group=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
                .First(t=>t.name=="10_Decoracion_MedievalCartoon");
            UnityEngine.Object.DestroyImmediate(group.gameObject);
            Apply(scene);EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Tools/Dungeon Track/Medieval Cartoon/Aplicar tema a escena actual")]
        public static void ApplyToCurrent()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Sali de Play Mode para decorar.");
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Aplicar tema medieval cartoon");
            Apply(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(group);
        }
        public static void Apply(Scene scene)
        {
            var root=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t=>t.name=="CIRCUITO_MAZMORRA_PROBUILDER");
            if(!root)throw new InvalidOperationException("Esta escena no tiene el circuito de mazmorra.");
            var old=root.Find("10_Decoracion_MedievalCartoon");
            if(old)throw new InvalidOperationException("El tema ya esta agregado. Modifica sus objetos desde Hierarchy o usa Undo.");
            MaterialAssign(root,"01_Pista_Editable_18_24_32m","Suelo");
            MaterialAssign(root,"02_Paredes_Sala_60m","Muro");
            MaterialAssign(root,"03_Techos_Ocultar_Para_Editar","Techo");
            MaterialAssign(root,"04_Pasto_Exterior","Pasto");
            MaterialAssign(root,"05_Arcos_Entrada_Salida","Piedra");
            MaterialAssign(root,"08_Bordes_Contencion_1_4m","Piedra");
            MaterialAssign(root,"09_Suelo_Sala_Y_Landmark","Suelo");
            var group=new GameObject("10_Decoracion_MedievalCartoon");
            Undo.RegisterCreatedObjectUndo(group,"Agregar decoracion");SceneManager.MoveGameObjectToScene(group,scene);group.transform.SetParent(root,false);
            Add("Landmark/Landmark_Gran_Guardian",group.transform,new Vector3(0,-4.5f,-5),0,1.8f);
            Add("Arquitectura/Arco_Abierto_18m",group.transform,new Vector3(-28,0,82),90);
            Add("Arquitectura/Arco_Abierto_18m",group.transform,new Vector3(-101,0,35),-26);
            // Placed outside the drivable surface and outside the rock routes.
            foreach(Vector3 pos in new[]{
                new Vector3(101,0,91),new Vector3(125,0,42),new Vector3(124,0,7),
                new Vector3(121,0,-30),new Vector3(120,0,-57),new Vector3(69,3,-116),
                new Vector3(20,8,-125),new Vector3(-31,15,-124),new Vector3(-73,22,-115),
                new Vector3(-119,24,-65),new Vector3(-112,29,-26),new Vector3(-101,22,9)})
            {
                var support=Add("Arquitectura/Columna_Cuadrada",group.transform,new Vector3(pos.x,-4.5f,pos.z));
                support.name="Soporte_Brasero";
                support.transform.localScale=new Vector3(.9f,(pos.y+4.5f)/6,.9f);
                Add("Iluminacion/Brasero_Pedestal_Alto",group.transform,pos,0,1.4f);
            }
            foreach(Vector3 pos in new[]{new Vector3(-37,-4.5f,-24),new Vector3(34,-4.5f,-27),
                new Vector3(-35,-4.5f,25),new Vector3(37,-4.5f,24)})
                Add("Guia_Sin_Flechas/Jardinera_Borde",group.transform,pos,0,2);
            foreach(float z in new[]{-65f,-20f,25f,65f})
            {
                Add("Decoracion/Estandarte_Pared",group.transform,new Vector3(137,12,z),-90,3);
                Add("Arquitectura/Columna_Cuadrada",group.transform,new Vector3(135,-4.5f,z),0,3);
            }
            foreach(Vector3 pos in new[]{new Vector3(-147,0,95),new Vector3(-70,0,104),
                new Vector3(-144,0,57),new Vector3(-78,0,56)})
                Add("Naturaleza/Cipres",group.transform,pos,0,2);
            foreach(Vector3 pos in new[]{new Vector3(-145,0,85),new Vector3(-71,0,108),new Vector3(-148,0,43)})
                Add("Naturaleza/Rocas_Grupo",group.transform,pos,0,1.7f);
            EditorSceneManager.MarkSceneDirty(scene);Selection.activeGameObject=group;
        }
        static void MaterialAssign(Transform root,string group,string material)
        {
            var part=root.Find(group);if(!part)return;
            var original=AssetDatabase.LoadAssetAtPath<Material>(F+"/Materials/"+material+".mat");
            string path=F+"/Materials/Pista_"+material+".mat";
            var asset=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!asset)
            {
                asset=new Material(original){name="Pista_"+material};
                asset.SetTextureScale("_BaseMap",new Vector2(.25f,.25f));
                AssetDatabase.CreateAsset(asset,path);
            }
            foreach(var renderer in part.GetComponentsInChildren<MeshRenderer>(true))
            {
                // Preserve the finish-line checkerboard.
                if(renderer.name.Contains("Meta_Cuadro"))continue;
                Undo.RecordObject(renderer,"Aplicar material");
                var assigned=renderer.sharedMaterials;
                for(int i=0;i<assigned.Length;i++)assigned[i]=asset;
                renderer.sharedMaterials=assigned;EditorUtility.SetDirty(renderer);
            }
        }
        static GameObject Add(string path,Transform parent,Vector3 position,float yaw=0,float scale=1)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(F+"/Prefabs/"+path+".prefab");
            if(!asset)throw new FileNotFoundException(path);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);
            Undo.RegisterCreatedObjectUndo(instance,"Agregar asset");
            instance.transform.position=position;instance.transform.rotation=Quaternion.Euler(0,yaw,0);
            instance.transform.localScale=Vector3.one*scale;
            foreach(var collider in instance.GetComponentsInChildren<Collider>(true))collider.gameObject.layer=10;
            return instance;
        }
    }
}

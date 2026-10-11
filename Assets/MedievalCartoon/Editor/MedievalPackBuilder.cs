using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MedievalCartoon.Editor
{
    public static class MedievalPackBuilder
    {
        public const string Folder = "Assets/MedievalCartoon";
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        static readonly List<string> inventory = new List<string>();
        static Vector3 V(float x,float y,float z) { return new Vector3(x,y,z); }

        // Explicit build command only; nothing regenerates on refresh or overwrites edited scenes.
        public static void Build()
        {
            materials.Clear(); meshes.Clear(); prefabs.Clear(); inventory.Clear();
            foreach(string sub in new[]{"Materials","Meshes","Prefabs","Scenes","Documentation"})
                Directory.CreateDirectory(Folder+"/"+sub);
            AssetDatabase.Refresh();
            foreach(string texture in new[]{"Pared_Piedra","Techo_Piedra","Piso_Adoquines","Pasto","Madera","Piedra_Escultura"})
            {
                string path=Folder+"/Textures/"+texture+".png";
                if (!File.Exists(path)) throw new FileNotFoundException("Falta textura del paquete",path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.wrapMode=TextureWrapMode.Repeat; importer.sRGBTexture=true;
                importer.maxTextureSize=2048; importer.textureCompression=TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
            Mat("Piedra",Color.white,"Piedra_Escultura");
            Mat("Muro",Color.white,"Pared_Piedra"); Mat("Techo",Color.white,"Techo_Piedra");
            Mat("Suelo",new Color(.83f,.82f,.81f),"Piso_Adoquines");
            Mat("Camino_Claro",new Color(1,.98f,.85f),"Piso_Adoquines");
            Mat("Pasto",Color.white,"Pasto"); Mat("Madera",Color.white,"Madera");
            Mat("Hierro",new Color(.15f,.19f,.20f),null,.65f,.3f);
            Mat("Oro",new Color(.94f,.61f,.18f),null,.45f,.35f);
            Mat("Tela",new Color(.46f,.055f,.10f)); Mat("Tierra",new Color(.23f,.14f,.07f));
            Mat("Hoja",new Color(.19f,.42f,.06f)); Mat("Hoja_Clara",new Color(.40f,.61f,.09f));
            Mat("Negro",new Color(.08f,.075f,.06f)); Mat("Terracota",new Color(.64f,.27f,.13f));
            Mat("Llama",new Color(1,.24f,.015f),null,0,0,3);
            Mat("Llama_Centro",new Color(1,.74f,.12f),null,0,0,2);
            MeshAsset("Bloque",MedievalMeshes.RoundedBox());
            MeshAsset("Esfera",MedievalMeshes.Sphere());
            MeshAsset("Cilindro",MedievalMeshes.Lathe("Cilindro",new[]{new Vector2(0,-.5f),new Vector2(1,-.5f),new Vector2(1,.5f),new Vector2(0,.5f)}));
            MeshAsset("Anillo",MedievalMeshes.Lathe("Anillo",new[]{new Vector2(.86f,-.5f),new Vector2(1,-.5f),new Vector2(1,.5f),new Vector2(.86f,.5f),new Vector2(.86f,-.5f)}));
            MeshAsset("Barril",MedievalMeshes.Lathe("Barril",new[]{new Vector2(0,0),new Vector2(.48f,0),new Vector2(.56f,.15f),new Vector2(.63f,.7f),new Vector2(.56f,1.25f),new Vector2(.48f,1.4f),new Vector2(0,1.4f)}));
            MeshAsset("Barril_Abierto",MedievalMeshes.Lathe("Barril_Abierto",new[]{new Vector2(0,0),new Vector2(.48f,0),new Vector2(.56f,.15f),new Vector2(.63f,.7f),new Vector2(.56f,1.25f),new Vector2(.48f,1.4f),new Vector2(.40f,1.4f),new Vector2(.47f,.7f),new Vector2(.35f,.12f),new Vector2(0,.12f)}));
            MeshAsset("Cuenco",MedievalMeshes.Lathe("Cuenco",new[]{new Vector2(0,0),new Vector2(.36f,0),new Vector2(.55f,.16f),new Vector2(.82f,.46f),new Vector2(.72f,.48f),new Vector2(.42f,.17f),new Vector2(0,.15f)}));
            MeshAsset("Maceta",MedievalMeshes.Lathe("Maceta",new[]{new Vector2(0,0),new Vector2(.36f,0),new Vector2(.55f,.9f),new Vector2(.62f,.9f),new Vector2(.62f,1.04f),new Vector2(.50f,1.04f),new Vector2(.43f,.15f),new Vector2(0,.15f)}));
            MeshAsset("Escudo",MedievalMeshes.Extrude("Escudo",new[]{new Vector2(-.8f,1),new Vector2(.8f,1),new Vector2(1,.6f),new Vector2(.8f,-.5f),new Vector2(0,-1.2f),new Vector2(-.8f,-.5f),new Vector2(-1,.6f)},.25f));
            MeshAsset("Estandarte",MedievalMeshes.Banner(true)); MeshAsset("Bandera",MedievalMeshes.Banner(false));
            MeshAsset("Llama",MedievalMeshes.Lathe("Llama",new[]{new Vector2(0,0),new Vector2(.20f,.04f),new Vector2(.31f,.30f),new Vector2(.21f,.55f),new Vector2(.12f,.77f),new Vector2(.16f,.95f),new Vector2(0,1.25f)},10));
            BuildArchitecture(); BuildLighting(); BuildDecor(); BuildGuidance(); BuildLandmark();
            File.WriteAllText(Folder+"/Documentation/INVENTARIO.md","# Prefabs medieval cartoon\n\n"+string.Join("\n",inventory)+"\n");
            AssetDatabase.Refresh(); AssetDatabase.SaveAssets();
            BuildCatalog(); BuildGuidanceScene();
            Debug.Log("MEDIEVAL_PACK_BUILT: "+prefabs.Count+" prefabs");
        }

        static Material Mat(string name,Color color,string texture=null,float metallic=0,float smoothness=.15f,float emission=0)
        {
            string path=Folder+"/Materials/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) { material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path); }
            material.SetColor("_BaseColor",color);
            material.SetFloat("_Metallic",metallic); material.SetFloat("_Smoothness",smoothness);
            if(texture!=null) material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/"+texture+".png"));
            if(emission>0)
            {
                material.SetColor("_EmissionColor",color*emission);
                material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;
                MaterialEditor.FixupEmissiveFlag(material);material.EnableKeyword("_EMISSION");
            }
            material.enableInstancing=true;
            EditorUtility.SetDirty(material);materials[name]=material;return material;
        }
        static Mesh MeshAsset(string name,Mesh mesh)
        {
            string path=Folder+"/Meshes/"+name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing) {EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}
            else AssetDatabase.CreateAsset(mesh,path);
            meshes[name]=mesh; return mesh;
        }
        static GameObject Root(string name) {return new GameObject(name);}
        static GameObject Part(Transform parent,string name,string mesh,string mat,Vector3 pos,Vector3 scale,bool solid=false)
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);
            obj.transform.localPosition=pos;obj.transform.localScale=scale;
            obj.AddComponent<MeshFilter>().sharedMesh=meshes[mesh];
            obj.AddComponent<MeshRenderer>().sharedMaterial=materials[mat];
            if(solid)obj.AddComponent<MeshCollider>().sharedMesh=meshes[mesh];
            return obj;
        }
        static GameObject Box(Transform p,string name,Vector3 pos,Vector3 size,string mat="Piedra",bool solid=true)
        {return Part(p,name,"Bloque",mat,pos,size,solid);}
        static GameObject TiledBox(Transform p,string name,Vector3 pos,Vector3 size,string mat)
        {
            string key="PanelUV_"+size.x+"_"+size.y+"_"+size.z;
            if(!meshes.ContainsKey(key))
            {
                var mesh=Object.Instantiate(meshes["Bloque"]);var uv=mesh.uv;var points=mesh.vertices;var normals=mesh.normals;
                for(int i=0;i<uv.Length;i++)
                {
                    Vector3 n=normals[i],q=Vector3.Scale(points[i],size)/4;
                    if(Mathf.Abs(n.y)>Mathf.Abs(n.x)&&Mathf.Abs(n.y)>Mathf.Abs(n.z))uv[i]=new Vector2(q.x,q.z);
                    else if(Mathf.Abs(n.x)>Mathf.Abs(n.z))uv[i]=new Vector2(q.z,q.y);
                    else uv[i]=new Vector2(q.x,q.y);
                }
                mesh.uv=uv;MeshAsset(key,mesh);
            }
            return Part(p,name,key,mat,pos,size,true);
        }
        static GameObject Ball(Transform p,string name,Vector3 pos,Vector3 radii,string mat="Piedra",bool solid=false)
        {return Part(p,name,"Esfera",mat,pos,radii,solid);}
        static GameObject Cyl(Transform p,string name,Vector3 pos,float radius,float height,string mat="Piedra",bool solid=true)
        {return Part(p,name,"Cilindro",mat,pos,V(radius,height,radius),solid);}
        static GameObject Beam(Transform p,string name,Vector3 a,Vector3 b,float radius,string mat="Piedra",bool solid=true)
        {
            var obj=Cyl(p,name,(a+b)/2,radius,(b-a).magnitude,mat,solid);
            obj.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);return obj;
        }
        static GameObject Save(string category,GameObject obj)
        {
            string dir=Folder+"/Prefabs/"+category;Directory.CreateDirectory(dir);
            var asset=PrefabUtility.SaveAsPrefabAsset(obj,dir+"/"+obj.name+".prefab");
            var bounds=BoundsOf(obj);
            inventory.Add("- **"+obj.name+"** · "+category+" · "+bounds.size.x.ToString("0.0")+" × "+bounds.size.y.ToString("0.0")+" × "+bounds.size.z.ToString("0.0")+" m");
            prefabs[obj.name]=asset;Object.DestroyImmediate(obj);return asset;
        }
        static GameObject Instance(string name,Transform parent,Vector3 pos,float yaw=0,float scale=1)
        {
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[name]);
            obj.transform.SetParent(parent,false); obj.transform.localPosition=pos;
            obj.transform.localRotation=Quaternion.Euler(0,yaw,0);obj.transform.localScale=Vector3.one*scale;return obj;
        }
        public static Bounds BoundsOf(GameObject obj)
        {
            var renderers=obj.GetComponentsInChildren<Renderer>(true);
            Bounds b=new Bounds(obj.transform.position,Vector3.zero);
            if(renderers.Length>0) {b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);}
            return b;
        }

        static void Column(Transform p,Vector3 pos,bool square=false,float height=6)
        {
            var root=Root("Columna");root.transform.SetParent(p,false);root.transform.localPosition=pos;
            if(square)
            {
                Box(root.transform,"Base",V(0,.3f,0),V(2,.6f,2));
                Box(root.transform,"Fuste",V(0,height/2,0),V(1.35f,height-.7f,1.35f),"Muro");
                Box(root.transform,"Capitel",V(0,height-.25f,0),V(2,.5f,2));
            }
            else
            {
                Cyl(root.transform,"Base",V(0,.23f,0),1.15f,.46f);
                Cyl(root.transform,"Zocalo",V(0,.62f,0),.94f,.34f);
                Cyl(root.transform,"Fuste",V(0,height/2,0),.78f,height-1.2f,"Muro");
                for(int i=1;i<4;i++)Cyl(root.transform,"Junta_"+i,V(0,i*height/4,0),.81f,.08f);
                Cyl(root.transform,"Collarin",V(0,height-.65f,0),.95f,.25f);
                Cyl(root.transform,"Capitel",V(0,height-.25f,0),1.15f,.5f);
            }
        }
        static void Arch(Transform p,float opening,float jambHeight=4)
        {
            float r=opening/2,t=.8f;
            foreach(int sign in new[]{-1,1})
            {
                for(int row=0;row<4;row++)
                    Box(p,"Pilar_"+sign+"_"+row,V(sign*(r+t/2),jambHeight*(row+.5f)/4,0),V(t,jambHeight/4-.035f,1.2f));
                Box(p,"Base_"+sign,V(sign*(r+t/2),.2f,0),V(1.15f,.4f,1.55f));
                Box(p,"Imposta_"+sign,V(sign*(r+t/2),jambHeight,0),V(1.15f,.3f,1.55f));
            }
            string key="Arco_"+opening;
            if(!meshes.ContainsKey(key))MeshAsset(key,MedievalMeshes.Arc(r,t,180,1.2f));
            Part(p,"Dovelas",key,"Piedra",V(0,jambHeight,0),Vector3.one,true);
            Box(p,"Clave",V(0,jambHeight+r+t/2,0),V(.75f,1.05f,1.4f));
        }
        static void Wall(Transform p,float width,float height=6)
        {
            Box(p,"Mamposteria",V(0,height/2,0),V(width,height,1),"Muro");
            Box(p,"Zocalo",V(0,.3f,0),V(width,.6f,1.3f));
            Box(p,"Cornisa",V(0,height-.2f,0),V(width,.4f,1.35f));
        }
        static void BuildArchitecture()
        {
            foreach(float width in new[]{4f,8f})
            {var g=Root("Pared_"+width+"m");Wall(g.transform,width);Save("Arquitectura",g);}
            var corner=Root("Pared_Esquina");Wall(corner.transform,4);
            var wing=Root("Ala");wing.transform.SetParent(corner.transform,false);wing.transform.localPosition=V(1.5f,0,1.5f);wing.transform.localRotation=Quaternion.Euler(0,90,0);Wall(wing.transform,4);Save("Arquitectura",corner);
            var ruin=Root("Pared_Ruina");Wall(ruin.transform,4,2.4f);Box(ruin.transform,"Resto_Alto",V(-1.25f,3,0),V(1.45f,1.3f,1),"Muro");Save("Arquitectura",ruin);
            foreach(bool square in new[]{false,true})
            {var g=Root(square?"Columna_Cuadrada":"Columna_Redonda");Column(g.transform,Vector3.zero,square);Save("Arquitectura",g);}
            foreach(float width in new[]{4f,8f,18f})
            {var g=Root("Arco_Abierto_"+width+"m");Arch(g.transform,width,width==18?8:4);Save("Arquitectura",g);}
            var twin=Root("Arcada_Doble");Arch(twin.transform,4);
            var second=Root("Arco_Segundo");second.transform.SetParent(twin.transform,false);second.transform.localPosition=V(5.6f,0,0);Arch(second.transform,4);Save("Arquitectura",twin);
            var gate=Root("Reja_Cerrada");Arch(gate.transform,4);
            for(int i=-4;i<=4;i++)Beam(gate.transform,"Barrote_"+i,V(i*.46f,0,0),V(i*.46f,4+Mathf.Sqrt(4-i*i*.2116f),0),.065f,"Hierro");
            Box(gate.transform,"Travesano",V(0,2.3f,0),V(4,.18f,.18f),"Hierro");
            // Intentional full collider for the CLOSED gate only.
            var bc=gate.AddComponent<BoxCollider>();bc.center=V(0,2.5f,0);bc.size=V(4,5,.3f);Save("Arquitectura",gate);
            var door=Root("Puerta_Cerrada");Arch(door.transform,4);
            for(int i=-3;i<=3;i++)Box(door.transform,"Tablon_"+i,V(i*.52f,2.5f,0),V(.50f,5,.25f),"Madera");
            foreach(float y in new[]{1.3f,3.5f})Box(door.transform,"Herraje",V(0,y,.22f),V(3.9f,.17f,.13f),"Hierro");
            Cyl(door.transform,"Cerradura",V(.35f,2.6f,.26f),.12f,.12f,"Oro",false).transform.localRotation=Quaternion.Euler(90,0,0);Save("Arquitectura",door);
            foreach(string mat in new[]{"Suelo","Pasto","Techo","Camino_Claro"})
            {var g=Root("Baldosa_"+mat+"_4m");Box(g.transform,"Panel",V(0,-.12f,0),V(4,.24f,4),mat);Save("Arquitectura",g);}
            var vault=Root("Boveda_8m");MeshAsset("Boveda",MedievalMeshes.Arc(4,.45f,180,4));
            Part(vault.transform,"Boveda","Boveda","Techo",V(0,4,0),Vector3.one,true);Save("Arquitectura",vault);
            var beam=Root("Viga_Madera_4m");Box(beam.transform,"Viga",V(0,.2f,0),V(4,.4f,.4f),"Madera");foreach(float x in new[]{-1.5f,1.5f})Box(beam.transform,"Banda",V(x,.2f,0),V(.18f,.44f,.44f),"Hierro");Save("Arquitectura",beam);
            var stairs=Root("Escalera_6_Peldanos");
            for(int i=0;i<6;i++)Box(stairs.transform,"Peldano_"+i,V(0,(i+1)*.15f,i*.5f),V(4,(i+1)*.3f,.5f));Save("Arquitectura",stairs);
            var buttress=Root("Contrafuerte");Box(buttress.transform,"Base",V(0,.4f,0),V(2,.8f,2.4f));
            Box(buttress.transform,"Pilar",V(0,2.5f,-.5f),V(1.4f,4.2f,1.2f),"Muro");
            Box(buttress.transform,"Apoyo",V(0,1.5f,.35f),V(1.4f,2.4f,1.2f)).transform.localRotation=Quaternion.Euler(-16,0,0);Save("Arquitectura",buttress);
        }

        static void Fire(Transform parent,Vector3 pos,float size=1)
        {
            var source=Root("Fuego");source.transform.SetParent(parent,false);source.transform.localPosition=pos;
            var flame=Root("Llama_Animada");flame.transform.SetParent(source.transform,false);flame.transform.localScale=Vector3.one*size;
            Part(flame.transform,"Exterior","Llama","Llama",Vector3.zero,Vector3.one);
            Part(flame.transform,"Centro","Llama","Llama_Centro",V(0,.01f,.03f),V(.55f,.75f,.55f));
            var lightObj=Root("Luz_Calida");lightObj.transform.SetParent(source.transform,false);lightObj.transform.localPosition=V(0,.55f*size,0);
            var light=lightObj.AddComponent<Light>();light.type=LightType.Point;light.shadows=LightShadows.None;
            var torch=source.AddComponent<CartoonTorch>();torch.pointLight=light;torch.flame=flame.transform;
            torch.intensity=2.5f;torch.range=9*size;torch.color=new Color(1,.57f,.16f);
            light.intensity=torch.intensity;light.range=torch.range;light.color=torch.color;
        }
        static void Torch(Transform p,Vector3 pos,bool wall=false)
        {
            var root=Root("Antorcha");root.transform.SetParent(p,false);root.transform.localPosition=pos;
            Beam(root.transform,"Mango",V(0,.1f,0),V(0,1.25f,.20f),.09f,"Madera");
            Part(root.transform,"Copa","Cuenco","Hierro",V(0,1.1f,.18f),V(.45f,.60f,.45f));
            for(int i=0;i<6;i++)
            {
                float a=i*60*Mathf.Deg2Rad;
                Beam(root.transform,"Diente_"+i,V(Mathf.Cos(a)*.30f,1.32f,.18f+Mathf.Sin(a)*.30f),V(Mathf.Cos(a)*.34f,1.72f,.18f+Mathf.Sin(a)*.34f),.035f,"Hierro",false);
            }
            if(wall)
            {
                Box(root.transform,"Placa",V(0,.65f,-.27f),V(.38f,.75f,.12f),"Hierro");
                Beam(root.transform,"Soporte",V(0,.48f,-.27f),V(0,.48f,0),.055f,"Hierro");
                foreach(float y in new[]{.4f,.9f})Ball(root.transform,"Remache",V(0,y,-.18f),Vector3.one*.055f,"Oro");
            }
            Fire(root.transform,V(0,1.44f,.18f),.7f);
        }
        static void Brazier(Transform p,float height,bool tripod=false)
        {
            if(tripod)
            {for(int i=0;i<3;i++){float a=i*120*Mathf.Deg2Rad;Beam(p,"Pata_"+i,V(Mathf.Cos(a)*.65f,0,Mathf.Sin(a)*.65f),V(0,height,0),.07f,"Hierro");}}
            else
            {Cyl(p,"Base",V(0,.18f,0),.72f,.36f);Cyl(p,"Fuste",V(0,height/2,0),.34f,height);}
            Part(p,"Cuenco","Cuenco","Hierro",V(0,height,0),Vector3.one);
            Fire(p,V(0,height+.25f,0),1);
        }
        static void BuildLighting()
        {
            var hand=Root("Antorcha_Suelta");Torch(hand.transform,Vector3.zero);Save("Iluminacion",hand);
            var wall=Root("Antorcha_Pared");Torch(wall.transform,V(0,0,.1f),true);Save("Iluminacion",wall);
            var doubleTorch=Root("Antorcha_Pared_Doble");Torch(doubleTorch.transform,V(-.5f,0,0),true);Torch(doubleTorch.transform,V(.5f,0,0),true);Save("Iluminacion",doubleTorch);
            foreach(float height in new[]{1.2f,3f})
            {var g=Root(height<2?"Brasero_Pedestal_Bajo":"Brasero_Pedestal_Alto");Brazier(g.transform,height);Save("Iluminacion",g);}
            var tripod=Root("Brasero_Tripode");Brazier(tripod.transform,1.25f,true);Save("Iluminacion",tripod);
            var bowl=Root("Brasero_Suelo");Part(bowl.transform,"Cuenco","Cuenco","Hierro",Vector3.zero,Vector3.one,true);Fire(bowl.transform,V(0,.25f,0));Save("Iluminacion",bowl);
            var hanging=Root("Brasero_Colgante");Part(hanging.transform,"Cuenco","Cuenco","Hierro",Vector3.zero,Vector3.one,false);Fire(hanging.transform,V(0,.25f,0));
            for(int i=0;i<3;i++){float a=i*120*Mathf.Deg2Rad;Beam(hanging.transform,"Cadena_"+i,V(Mathf.Cos(a)*.62f,.45f,Mathf.Sin(a)*.62f),V(0,2.8f,0),.035f,"Hierro",false);}
            Save("Iluminacion",hanging);
            var post=Root("Poste_Antorcha");Cyl(post.transform,"Base",V(0,.15f,0),.5f,.3f);Cyl(post.transform,"Poste",V(0,1.15f,0),.16f,2,"Madera");Torch(post.transform,V(0,1.6f,0));Save("Iluminacion",post);
        }

        static void Crest(Transform p,Vector3 pos,float scale=1)
        {
            var c=Root("Emblema_Flor");c.transform.SetParent(p,false);c.transform.localPosition=pos;c.transform.localScale=Vector3.one*scale;
            Ball(c.transform,"Petalo_Central",V(0,.14f,0),V(.11f,.25f,.035f),"Oro");
            foreach(int s in new[]{-1,1})Ball(c.transform,"Petalo_"+s,V(s*.14f,.05f,0),V(.16f,.10f,.035f),"Oro").transform.localRotation=Quaternion.Euler(0,0,-s*30);
            Box(c.transform,"Tallo",V(0,-.15f,0),V(.07f,.30f,.06f),"Oro",false);
            Box(c.transform,"Cinta",V(0,-.05f,.01f),V(.34f,.065f,.07f),"Oro",false);
        }
        static void Banner(Transform p,bool longBanner=true)
        {
            Part(p,"Tela",longBanner?"Estandarte":"Bandera","Tela",V(0,longBanner?3.4f:2,0),Vector3.one);
            Beam(p,"Travesano",V(-.82f,longBanner?3.45f:2.05f,0),V(.82f,longBanner?3.45f:2.05f,0),.05f,"Hierro",false);
            Crest(p,V(0,longBanner?1.9f:1.1f,.1f),2);
        }
        static void Crate(Transform p,Vector3 pos,float size=1)
        {
            var g=Root("Caja");g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=Vector3.one*size;
            Box(g.transform,"Caja",V(0,.55f,0),V(1.1f,1.1f,1.1f),"Madera");
            foreach(float x in new[]{-.46f,.46f})foreach(float z in new[]{-.57f,.57f})Box(g.transform,"Marco",V(x,.55f,z),V(.15f,1.1f,.10f),"Madera",false);
            foreach(float y in new[]{.10f,1f})foreach(float z in new[]{-.58f,.58f})Box(g.transform,"Marco",V(0,y,z),V(1.1f,.15f,.1f),"Madera",false);
            foreach(float z in new[]{-.60f,.60f})Box(g.transform,"Diagonal",V(0,.55f,z),V(.13f,1.25f,.08f),"Madera",false).transform.localRotation=Quaternion.Euler(0,0,40);
        }
        static void Barrel(Transform p,Vector3 pos,bool open=false)
        {
            Part(p,"Barril",open?"Barril_Abierto":"Barril","Madera",pos,Vector3.one,true);
            foreach(float y in new[]{.12f,.38f,1.03f,1.28f})Part(p,"Aro","Anillo","Hierro",pos+V(0,y,0),V(y<.2f||y>1.2f?.55f:.615f,.085f,y<.2f||y>1.2f?.55f:.615f));
        }
        static void Bush(Transform p,Vector3 pos,float size=1)
        {
            for(int i=0;i<5;i++)
            {float a=i*72*Mathf.Deg2Rad;Ball(p,"Hoja_"+i,pos+V(Mathf.Cos(a)*.38f,.5f+i%2*.12f,Mathf.Sin(a)*.38f)*size,V(.60f,.62f,.52f)*size,i%2==0?"Hoja":"Hoja_Clara");}
        }
        static void Rock(Transform p,Vector3 pos,Vector3 size,int seed=0)
        {var g=Ball(p,"Roca",pos+V(0,size.y*.72f,0),size,"Piedra",true);g.transform.localRotation=Quaternion.Euler(9+seed*11,seed*39,12+seed*7);}
        static void BuildDecor()
        {
            foreach(bool open in new[]{false,true}) {var g=Root(open?"Barril_Abierto":"Barril_Cerrado");Barrel(g.transform,Vector3.zero,open);Save("Decoracion",g);}
            var barrelStack=Root("Barriles_Grupo");Barrel(barrelStack.transform,V(-.65f,0,0));Barrel(barrelStack.transform,V(.65f,0,.2f));Barrel(barrelStack.transform,V(0,1.4f,.1f));Save("Decoracion",barrelStack);
            var crate=Root("Caja_Madera");Crate(crate.transform,Vector3.zero);Save("Decoracion",crate);
            var stack=Root("Cajas_Grupo");Crate(stack.transform,V(-.6f,0,0));Crate(stack.transform,V(.6f,0,0));Crate(stack.transform,V(0,1.1f,0),.85f);Save("Decoracion",stack);
            foreach(bool open in new[]{false,true})
            {
                var g=Root(open?"Cofre_Abierto":"Cofre_Cerrado");Box(g.transform,"Base",V(0,.4f,0),V(1.8f,.8f,1),"Madera");
                var lid=Root("Tapa");lid.transform.SetParent(g.transform,false);lid.transform.localPosition=V(0,.8f,-.48f);
                Box(lid.transform,"Tapa",V(0,.13f,.48f),V(1.8f,.35f,1),"Madera");
                foreach(float x in new[]{-.6f,.6f})Box(lid.transform,"Herraje",V(x,.30f,.48f),V(.14f,.06f,1.05f),"Hierro",false);
                lid.transform.localRotation=Quaternion.Euler(open?-65:0,0,0);
                Box(g.transform,"Cerradura",V(0,.65f,.55f),V(.2f,.26f,.08f),"Oro",false);Save("Decoracion",g);
            }
            foreach(bool longBanner in new[]{false,true})
            {var g=Root(longBanner?"Estandarte_Pared":"Bandera_Corta");Banner(g.transform,longBanner);Save("Decoracion",g);}
            var bannerPost=Root("Estandarte_Poste");Cyl(bannerPost.transform,"Base",V(0,.18f,0),.6f,.36f);Cyl(bannerPost.transform,"Poste",V(0,1.85f,-.1f),.065f,3.7f,"Hierro");Banner(bannerPost.transform);Save("Decoracion",bannerPost);
            var shield=Root("Escudo_Decorativo");Part(shield.transform,"Borde","Escudo","Oro",V(0,1.2f,0),Vector3.one);Part(shield.transform,"Interior","Escudo","Tela",V(0,1.2f,.15f),V(.85f,.85f,.4f));Crest(shield.transform,V(0,1.3f,.25f),2);Save("Decoracion",shield);
            var hammer=Root("Martillo_Decorativo");Cyl(hammer.transform,"Mango",V(0,1.5f,0),.12f,3,"Madera");Box(hammer.transform,"Cabeza",V(0,2.7f,0),V(1.4f,.8f,.7f));Save("Decoracion",hammer);
            foreach(bool planted in new[]{false,true})
            {var g=Root(planted?"Maceta_Arbusto":"Maceta_Vacia");Part(g.transform,"Maceta","Maceta","Terracota",Vector3.zero,Vector3.one,true);if(planted){Cyl(g.transform,"Tierra",V(0,.94f,0),.48f,.05f,"Tierra",false);Bush(g.transform,V(0,.75f,0),.8f);}Save("Naturaleza",g);}
            for(int i=0;i<3;i++){var g=Root("Roca_"+(i+1));Rock(g.transform,Vector3.zero,V(.65f+i*.45f,.6f+i*.25f,.55f+i*.35f),i);Save("Naturaleza",g);}
            var rocks=Root("Rocas_Grupo");for(int i=0;i<5;i++)Rock(rocks.transform,V((i%3-1)*1.1f,0,(i/3)*.9f),V(.55f+i*.08f,.5f,.55f),i);Save("Naturaleza",rocks);
            var bush=Root("Arbusto");Bush(bush.transform,Vector3.zero);Save("Naturaleza",bush);
            var tree=Root("Cipres");Cyl(tree.transform,"Tronco",V(0,.65f,0),.15f,1.3f,"Madera");for(int i=0;i<4;i++)Ball(tree.transform,"Copa_"+i,V(0,1.6f+i*.65f,0),V(.90f-i*.17f,1,.90f-i*.17f),i%2==0?"Hoja":"Hoja_Clara");Save("Naturaleza",tree);
            var grass=Root("Matas_Pasto");for(int i=0;i<7;i++)Ball(grass.transform,"Mata_"+i,V((i%3-1)*.18f,.25f,(i/3-1)*.16f),V(.10f,.34f,.10f),"Hoja_Clara").transform.localRotation=Quaternion.Euler(i*8,0,i*12-30);Save("Naturaleza",grass);
            var rubble=Root("Escombros");for(int i=0;i<7;i++)Box(rubble.transform,"Piedra_"+i,V((i%3-1)*.55f,.15f+i%2*.20f,(i/3-1)*.4f),V(.6f,.3f,.45f)).transform.localRotation=Quaternion.Euler(i*7,i*42,i*13);Save("Decoracion",rubble);
        }

        static void Curb(Transform p,float length,float height=.65f)
        {
            Box(p,"Borde",V(0,height/2,0),V(length,height,.65f),"Muro");
            Box(p,"Coronacion",V(0,height,0),V(length,.14f,.78f));
        }
        static void BuildGuidance()
        {
            foreach(float length in new[]{2f,4f})
            {var g=Root("Borde_Bajo_"+length+"m");Curb(g.transform,length);Save("Guia_Sin_Flechas",g);}
            var curve=Root("Borde_Curvo_90");MeshAsset("BordeCurvo",MedievalMeshes.Arc(4,.65f,90,.65f));
            var arc=Part(curve.transform,"Curva","BordeCurvo","Piedra",V(0,.325f,0),Vector3.one,true);arc.transform.localRotation=Quaternion.Euler(90,0,0);Save("Guia_Sin_Flechas",curve);
            var rail=Root("Valla_Madera_4m");foreach(float x in new[]{-1.8f,1.8f})Box(rail.transform,"Poste",V(x,.7f,0),V(.22f,1.4f,.22f),"Madera");
            foreach(float y in new[]{.5f,1.1f})Box(rail.transform,"Baranda",V(0,y,0),V(4,.18f,.18f),"Madera");Save("Guia_Sin_Flechas",rail);
            var marker=Root("Mojon_Luz");Cyl(marker.transform,"Base",V(0,.12f,0),.5f,.24f);Box(marker.transform,"Pilar",V(0,.75f,0),V(.55f,1.3f,.55f));
            Part(marker.transform,"Cuenco","Cuenco","Hierro",V(0,1.4f,0),Vector3.one*.5f);Fire(marker.transform,V(0,1.6f,0),.5f);Save("Guia_Sin_Flechas",marker);
            var pair=Root("Paso_Iluminado_8m");Instance("Brasero_Pedestal_Alto",pair.transform,V(-4.8f,0,0));Instance("Brasero_Pedestal_Alto",pair.transform,V(4.8f,0,0));Save("Guia_Sin_Flechas",pair);
            var path=Root("Banda_Pavimento_Claro");Box(path.transform,"Pavimento",V(0,-.05f,0),V(3,.10f,8),"Camino_Claro");Save("Guia_Sin_Flechas",path);
            var groove=Root("Huellas_Ruedas");foreach(float x in new[]{-.6f,.6f})Box(groove.transform,"Desgaste",V(x,.005f,0),V(.26f,.01f,4),"Camino_Claro",false);Save("Guia_Sin_Flechas",groove);
            var chain=Root("Mojones_Repeticion_Recta");for(int i=0;i<4;i++)Instance("Mojon_Luz",chain.transform,V(0,0,i*4));Save("Guia_Sin_Flechas",chain);
            var turn=Root("Mojones_Repeticion_Curva");for(int i=0;i<5;i++){float a=i*22.5f*Mathf.Deg2Rad;Instance("Mojon_Luz",turn.transform,V(Mathf.Cos(a)*10,0,Mathf.Sin(a)*10));}Save("Guia_Sin_Flechas",turn);
            var open=Root("Portal_Iluminado_8m");Instance("Arco_Abierto_8m",open.transform,Vector3.zero);Instance("Antorcha_Pared",open.transform,V(-4.45f,2.6f,.7f));Instance("Antorcha_Pared",open.transform,V(4.45f,2.6f,.7f));Save("Guia_Sin_Flechas",open);
            var blocker=Root("Bloqueo_Cajas_Barriles");Instance("Cajas_Grupo",blocker.transform,V(-1.1f,0,0));Instance("Barriles_Grupo",blocker.transform,V(1.1f,0,.15f));Save("Guia_Sin_Flechas",blocker);
            var planter=Root("Jardinera_Borde");Box(planter.transform,"Base",V(0,.25f,0),V(4,.5f,1.4f));
            Box(planter.transform,"Tierra",V(0,.5f,0),V(3.5f,.08f,1),"Tierra",false);
            foreach(float x in new[]{-1.4f,0,1.4f})Bush(planter.transform,V(x,.4f,0),.7f);Save("Guia_Sin_Flechas",planter);
        }

        static void Pedestal(Transform p)
        {
            Cyl(p,"Escalon_1",V(0,.35f,0),10,.7f);
            Cyl(p,"Escalon_2",V(0,1,0),8.7f,.6f,"Muro");
            Cyl(p,"Plinto",V(0,1.9f,0),7.6f,1.2f,"Muro");
            Cyl(p,"Cornisa",V(0,2.7f,0),8.1f,.4f);
            for(int i=0;i<8;i++){float a=i*45*Mathf.Deg2Rad;var emblem=Root("Medallon_"+i);emblem.transform.SetParent(p,false);emblem.transform.localPosition=V(Mathf.Sin(a)*7.7f,1.8f,Mathf.Cos(a)*7.7f);emblem.transform.localRotation=Quaternion.Euler(0,i*45,0);Crest(emblem.transform,Vector3.zero,1.6f);}
        }
        static void Knight(Transform p)
        {
            foreach(int s in new[]{-1,1})
            {
                Box(p,"Bota_"+s,V(s*1.65f,3.65f,.60f),V(2.6f,1.5f,3.6f));
                Beam(p,"Pierna_"+s,V(s*1.65f,4.2f,0),V(s*1.4f,8.5f,0),1.05f);
                Ball(p,"Rodillera_"+s,V(s*1.65f,6.4f,.85f),V(1.25f,1.3f,.75f));
            }
            Box(p,"Cintura",V(0,9.1f,0),V(5,1.9f,3.2f));
            Box(p,"Cinturon",V(0,9.7f,0),V(5.35f,.50f,3.4f));
            Box(p,"Hebilla",V(0,9.7f,1.78f),V(.9f,.6f,.18f),"Oro",false);
            Box(p,"Torso",V(0,12.1f,0),V(5.4f,4.8f,3.3f));
            var plate=Box(p,"Pechera",V(0,12.7f,1.7f),V(4.6f,3.1f,.60f));plate.transform.localRotation=Quaternion.Euler(-8,0,0);
            Crest(p,V(0,12.7f,2.1f),4);
            foreach(int s in new[]{-1,1})Ball(p,"Hombrera_"+s,V(s*3.4f,13.7f,0),V(1.9f,1.25f,1.8f));
            Cyl(p,"Cuello",V(0,15,0),.9f,1.3f);
            if(!meshes.ContainsKey("Casco"))MeshAsset("Casco",MedievalMeshes.Lathe("Casco",new[]{
                new Vector2(0,14.8f),new Vector2(1.35f,14.8f),new Vector2(1.7f,15.1f),
                new Vector2(1.7f,17.5f),new Vector2(1.5f,18.15f),new Vector2(.75f,18.6f),new Vector2(0,18.7f)},16));
            Part(p,"Casco","Casco","Piedra",Vector3.zero,V(1,1,1),true);
            Box(p,"Visor_Oscuro",V(0,16.9f,1.60f),V(2.75f,.42f,.12f),"Negro",false);
            Box(p,"Protector_Nariz",V(0,16.35f,1.71f),V(.35f,1.8f,.28f));
            foreach(int s in new[]{-1,1})
                for(int i=0;i<3;i++)Box(p,"Respiradero",V(s*(.5f+i*.30f),15.7f,Mathf.Sqrt(1.7f*1.7f-Mathf.Pow(.5f+i*.30f,2))+.03f),V(.08f,.43f,.08f),"Negro",false);
            Box(p,"Cresta_Casco",V(0,18.75f,-.1f),V(.35f,1.5f,2.2f));
            Beam(p,"Brazo_Martillo",V(-3.5f,13.4f,0),V(-5.5f,12.5f,.35f),.9f);
            Ball(p,"Codo_Martillo",V(-5.5f,12.5f,.35f),Vector3.one*1.05f);
            Beam(p,"Antebrazo_Martillo",V(-5.5f,12.5f,.35f),V(-6.7f,16.7f,1),.82f);
            Box(p,"Guante_Martillo",V(-6.7f,16.7f,1),V(1.9f,1.6f,1.8f));
            for(int i=0;i<3;i++)Box(p,"Dedos_"+i,V(-6.85f,16.2f+i*.38f,1.94f),V(1.1f,.28f,.35f));
            Beam(p,"Mango_Martillo",V(-6.8f,11.5f,1),V(-7.3f,23,1),.43f);
            Box(p,"Cabeza_Martillo",V(-7.3f,23,1),V(5,3.15f,3.3f));
            foreach(float x in new[]{-9.1f,-5.5f})Box(p,"Refuerzo_Martillo",V(x,23,1),V(.32f,3.3f,3.5f));
            Beam(p,"Brazo_Escudo",V(3.5f,13.4f,0),V(4.4f,11.9f,.9f),.9f);
            Ball(p,"Codo_Escudo",V(4.4f,11.9f,.9f),Vector3.one*1.05f);
            Beam(p,"Antebrazo_Escudo",V(4.4f,11.9f,.9f),V(4.9f,11.3f,2),.8f);
            var shield=Root("Escudo");shield.transform.SetParent(p,false);shield.transform.localPosition=V(5,11.8f,2.1f);shield.transform.localRotation=Quaternion.Euler(-8,-18,-10);
            Part(shield.transform,"Borde","Escudo","Piedra",Vector3.zero,V(2.2f,2.9f,3),true);
            Part(shield.transform,"Interior","Escudo","Muro",V(0,0,.48f),V(1.9f,2.6f,.8f));
            Crest(shield.transform,V(0,.15f,.67f),5);
        }
        static void BuildLandmark()
        {
            var plinth=Root("Pedestal_Gran_Guardian");Pedestal(plinth.transform);Save("Landmark",plinth);
            var statue=Root("Estatua_Gran_Guardian");Knight(statue.transform);Save("Landmark",statue);
            var landmark=Root("Landmark_Gran_Guardian");Instance("Pedestal_Gran_Guardian",landmark.transform,Vector3.zero);Instance("Estatua_Gran_Guardian",landmark.transform,Vector3.zero);
            foreach(int s in new[]{-1,1})
            {
                Instance("Brasero_Pedestal_Alto",landmark.transform,V(s*7.2f,.7f,6),0,1.3f);
                Instance("Estandarte_Pared",landmark.transform,V(s*5,0,7.6f));
                Instance("Cipres",landmark.transform,V(s*7.5f,.7f,-3),0,1.4f);
                Instance("Arbusto",landmark.transform,V(s*5.8f,.7f,4),0,1.4f);
            }
            Save("Landmark",landmark);
        }

        static Camera SceneCamera(Scene scene,Vector3 pos,Vector3 target,float fov=45)
        {
            var obj=Root("Camara_Preview");SceneManager.MoveGameObjectToScene(obj,scene);var camera=obj.AddComponent<Camera>();
            camera.scene=scene;camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);
            camera.transform.position=pos;camera.transform.LookAt(target);camera.fieldOfView=fov;camera.farClipPlane=1000;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.10f,.14f,.19f);
            obj.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;return camera;
        }
        static void Lighting(Scene scene)
        {
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.45f,.48f,.51f);RenderSettings.fog=false;
            var obj=Root("Sol_Suave");SceneManager.MoveGameObjectToScene(obj,scene);var light=obj.AddComponent<Light>();
            light.type=LightType.Directional;light.intensity=1.5f;light.color=new Color(1,.91f,.78f);
            light.shadows=LightShadows.Soft;obj.transform.rotation=Quaternion.Euler(45,-35,0);
        }
        static void BuildCatalog()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Lighting(scene);
            var gallery=Root("CATALOGO_PREFABS");int i=0;
            foreach(var item in prefabs.Where(kv=>!kv.Key.Contains("Gran_Guardian")))
            {
                var instance=Instance(item.Key,gallery.transform,V((i%8)*12,0,-(i/8)*14));
                var b=BoundsOf(instance);float max=Mathf.Max(b.size.x,b.size.z);
                if(max>9)instance.transform.localScale*=9/max;
                // This is a gallery arrangement, not a playable scene.
                Box(gallery.transform,"Expositor_"+item.Key,instance.transform.localPosition+V(0,-.20f,0),V(10,.35f,10),"Suelo");
                Label(gallery.transform,item.Key.Replace("_"," "),instance.transform.localPosition+V(0,.03f,-5.1f),.13f);
                i++;
            }
            Instance("Landmark_Gran_Guardian",gallery.transform,V(-24,0,-28));
            var camera=SceneCamera(scene,V(108,110,89),V(32,0,-45),47);
            FitCamera(camera,BoundsOf(gallery),1.5f);
            EditorSceneManager.SaveScene(scene,Folder+"/Scenes/Catalogo_MedievalCartoon.unity");
        }
        static void Label(Transform parent,string text,Vector3 pos,float size)
        {
            var obj=Root("Etiqueta");obj.transform.SetParent(parent,false);obj.transform.localPosition=pos;
            obj.transform.localRotation=Quaternion.Euler(90,0,0);
            var label=obj.AddComponent<TextMesh>();label.text=text;label.anchor=TextAnchor.MiddleCenter;label.fontSize=45;label.characterSize=size;
            label.color=new Color(1,.95f,.76f);
        }
        static void BuildGuidanceScene()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Lighting(scene);
            var root=Root("EJEMPLO_GUIA_SIN_FLECHAS");
            TiledBox(root.transform,"Suelo_Pasto",V(0,-.42f,0),V(65,.8f,65),"Pasto");
            TiledBox(root.transform,"Camino_Entrada",V(0,-.1f,13),V(8,.2f,25),"Suelo");
            TiledBox(root.transform,"Camino_Abierto",V(-10,-.08f,-7),V(8,.2f,28),"Camino_Claro").transform.localRotation=Quaternion.Euler(0,42,0);
            TiledBox(root.transform,"Camino_Cerrado",V(12,-.09f,-5),V(8,.2f,25),"Suelo").transform.localRotation=Quaternion.Euler(0,-50,0);
            Instance("Portal_Iluminado_8m",root.transform,V(-18,0,-16),42);
            Instance("Reja_Cerrada",root.transform,V(17,0,-10),-50,1.5f);
            Instance("Bloqueo_Cajas_Barriles",root.transform,V(14,0,-7),-50,1.8f);
            for(int j=0;j<5;j++)
            {
                float angle=42*Mathf.Deg2Rad,along=12-j*5.5f;
                Vector3 center=V(-10+Mathf.Sin(angle)*along,0,-7+Mathf.Cos(angle)*along);
                Vector3 side=V(Mathf.Cos(angle),0,-Mathf.Sin(angle))*4.8f;
                Instance("Mojon_Luz",root.transform,center+side);
                Instance("Borde_Bajo_4m",root.transform,center-side,132);
            }
            for(int j=0;j<3;j++)Instance("Jardinera_Borde",root.transform,V(5+j*5,0,8-j*4),-50);
            Instance("Landmark_Gran_Guardian",root.transform,V(-23,0,-27),42,.55f);
            foreach(Vector3 pos in new[]{V(-24,0,12),V(26,0,-17),V(-27,0,-2),V(22,0,20)})
            {Instance("Cipres",root.transform,pos,0,2);Instance("Rocas_Grupo",root.transform,pos+V(2,0,1));}
            SceneCamera(scene,V(42,46,51),V(-2,2,-6),47);
            EditorSceneManager.SaveScene(scene,Folder+"/Scenes/Ejemplo_Guia_Sin_Flechas.unity");
        }

        public static void CreateDemoAndCapture()
        {
            MedievalTrackTheme.CreateExample();
            CapturePreviews();
        }
        public static void FinalizeAppearance()
        {
            Build();
            MedievalTrackTheme.UpdateGeneratedExample();
            CapturePreviews();Export();
        }
        static void FitCamera(Camera camera,Bounds bounds,float aspect)
        {
            camera.transform.position=bounds.center+V(85,100,110);camera.transform.LookAt(bounds.center);
            camera.orthographic=true;float maxX=0,maxY=0;
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
            {
                var q=camera.transform.InverseTransformPoint(bounds.center+Vector3.Scale(bounds.extents,V(x,y,z)));
                maxX=Mathf.Max(maxX,Mathf.Abs(q.x));maxY=Mathf.Max(maxY,Mathf.Abs(q.y));
            }
            camera.orthographicSize=Mathf.Max(maxY,maxX/aspect)*1.04f;
        }
        public static void CapturePreviews()
        {
            string output=Path.GetFullPath("../../outputs");
            Directory.CreateDirectory(output);
            var catalog=EditorSceneManager.OpenScene(Folder+"/Scenes/Catalogo_MedievalCartoon.unity");
            // Warm the pipeline and texture uploads before keeping the first capture.
            Render(Object.FindFirstObjectByType<Camera>(),output+"/Catalogo_MedievalCartoon.png",1800,1200);
            Render(Object.FindFirstObjectByType<Camera>(),output+"/Catalogo_MedievalCartoon.png",1800,1200);
            var camera=Object.FindFirstObjectByType<Camera>();
            var gallery=catalog.GetRootGameObjects().First(g=>g.name=="CATALOGO_PREFABS");
            foreach(Transform child in gallery.transform)if(child.name!="Landmark_Gran_Guardian")child.gameObject.SetActive(false);
            camera.orthographic=false;
            camera.transform.position=V(-49,24,10);camera.transform.LookAt(V(-24,12,-28));camera.fieldOfView=40;
            Render(camera,output+"/Landmark_Gran_Guardian.png",1500,1500);
            EditorSceneManager.OpenScene(Folder+"/Scenes/Ejemplo_Guia_Sin_Flechas.unity");
            Render(Object.FindFirstObjectByType<Camera>(),output+"/Guia_Sin_Flechas.png",1600,1200);
            if(File.Exists(MedievalTrackTheme.ScenePath))
            {
                var track=EditorSceneManager.OpenScene(MedievalTrackTheme.ScenePath);
                var roof=track.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
                    .First(t=>t.name=="03_Techos_Ocultar_Para_Editar");
                bool active=roof.gameObject.activeSelf;roof.gameObject.SetActive(false);
                try
                {
                    var preview=SceneCamera(track,V(-26,24,59),V(3,12,-10),62);
                    Render(preview,output+"/Circuito_MedievalCartoon.png",1800,1200);
                }
                finally{roof.gameObject.SetActive(active);}
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            Debug.Log("MEDIEVAL_PREVIEWS_CAPTURED");
        }
        static void Render(Camera camera,string path,int width,int height)
        {
            var rt=new RenderTexture(width,height,24);var previous=RenderTexture.active;
            try
            {
                rt.Create();camera.targetTexture=rt;
                if(GraphicsSettings.currentRenderPipeline!=null)
                    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                else camera.Render();
                RenderTexture.active=rt;
                var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
                texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
                File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
            }
            finally{camera.targetTexture=null;RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);}
        }
        public static void Export()
        {
            AssetDatabase.ExportPackage(Folder,Path.GetFullPath("../../outputs/MedievalCartoon_Assets.unitypackage"),ExportPackageOptions.Recurse);
            Debug.Log("MEDIEVAL_PACKAGE_EXPORTED");
        }
        public static void CaptureAndExport() { CapturePreviews(); Export(); }
        public static void FinalizeTrackPlacement()
        { MedievalTrackTheme.UpdateGeneratedExample();CapturePreviews();Export(); }
        [MenuItem("Tools/Dungeon Track/Medieval Cartoon/Abrir catalogo")]
        public static void OpenCatalog()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(Folder+"/Scenes/Catalogo_MedievalCartoon.unity");
        }
        [MenuItem("Tools/Dungeon Track/Medieval Cartoon/Agregar landmark a escena")]
        public static void AddLandmark()
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Prefabs/Landmark/Landmark_Gran_Guardian.prefab");
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset);
            Undo.RegisterCreatedObjectUndo(instance,"Agregar landmark");Selection.activeGameObject=instance;
            SceneView.lastActiveSceneView?.FrameSelected();
        }
    }
}

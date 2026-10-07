using System.Linq;
using System.Collections.Generic;
using KartGame.Traps;
using MedievalCartoon;
using MedievalCartoon.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;

public class MedievalAssetsTests
{
    const string F=MedievalPackBuilder.Folder;
    static GameObject Prefab(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(F+"/Prefabs/"+path+".prefab");
    [Test]
    public void LibraryContainsUsableNativeMeshesMaterialsAndNoExternalGameplayDependency()
    {
        var paths=AssetDatabase.FindAssets("t:Prefab",new[]{F+"/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        Assert.That(paths.Length,Is.EqualTo(67));
        foreach(string path in paths)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab.GetComponentsInChildren<MeshRenderer>(true).Length,Is.GreaterThan(0),path);
            Assert.That(prefab.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),Is.True,path);
            foreach(var mf in prefab.GetComponentsInChildren<MeshFilter>(true))
                Assert.That(mf.sharedMesh && AssetDatabase.Contains(mf.sharedMesh),Is.True,path);
            foreach(var r in prefab.GetComponentsInChildren<MeshRenderer>(true))
                Assert.That(r.sharedMaterials.All(m=>m && m.shader && m.shader.name=="Universal Render Pipeline/Lit"),Is.True,path);
            foreach(string dependency in AssetDatabase.GetDependencies(path,true))
                Assert.That(dependency.StartsWith(F+"/") || dependency.StartsWith("Packages/") || dependency.StartsWith("Resources/"),Is.True,path+" -> "+dependency);
        }
    }
    [Test]
    public void MeshesHaveFiniteUvsNormalsAndNondegenerateTriangles()
    {
        foreach(string guid in AssetDatabase.FindAssets("t:Mesh",new[]{F+"/Meshes"}))
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));
            Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount),mesh.name);
            Assert.That(mesh.normals.Length,Is.EqualTo(mesh.vertexCount),mesh.name);
            Assert.That(mesh.vertices.All(v=>!float.IsNaN(v.x+v.y+v.z)&&!float.IsInfinity(v.x+v.y+v.z)),Is.True,mesh.name);
            Assert.That(mesh.uv.All(v=>!float.IsNaN(v.x+v.y)&&!float.IsInfinity(v.x+v.y)),Is.True,mesh.name);
            var p=mesh.vertices;var t=mesh.triangles;
            for(int i=0;i<t.Length;i+=3)Assert.That(Vector3.Cross(p[t[i+1]]-p[t[i]],p[t[i+2]]-p[t[i]]).sqrMagnitude,Is.GreaterThan(.000000001f),mesh.name);
        }
    }
    [Test]
    public void OpenArchesLeaveFullKartHeightClearWhileClosedGateBlocks()
    {
        foreach(float width in new[]{4f,8f,18f})
        {
            var obj=Object.Instantiate(Prefab("Arquitectura/Arco_Abierto_"+width+"m"));
            try
            {
                Physics.SyncTransforms();
                foreach(float x in new[]{-width/2+.55f,0,width/2-.55f})
                    foreach(var c in obj.GetComponentsInChildren<Collider>())
                        Assert.That(c.Raycast(new Ray(new Vector3(x,1.2f,-5),Vector3.forward),out _,10),Is.False,c.name);
            }
            finally{Object.DestroyImmediate(obj);}
        }
        var gate=Object.Instantiate(Prefab("Arquitectura/Reja_Cerrada"));
        try{Physics.SyncTransforms();Assert.That(gate.GetComponentsInChildren<Collider>().Any(c=>c.Raycast(new Ray(new Vector3(0,1.2f,-5),Vector3.forward),out _,10)),Is.True);}
        finally{Object.DestroyImmediate(gate);}
    }
    [Test]
    public void LandmarkFitsCentralReserveAndLightingHasNonphysicalFlames()
    {
        var landmark=Prefab("Landmark/Landmark_Gran_Guardian");
        var bounds=MedievalPackBuilder.BoundsOf(landmark);
        Assert.That(bounds.size.x,Is.LessThan(110));Assert.That(bounds.size.z,Is.LessThan(100));
        Assert.That(bounds.size.y,Is.InRange(24,30));
        foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{F+"/Prefabs"}))
        {
            var p=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            Assert.That(p.GetComponentsInChildren<Rigidbody>(true).Length,Is.EqualTo(0));
            foreach(var torch in p.GetComponentsInChildren<CartoonTorch>(true))
            {
                Assert.That(torch.pointLight,Is.Not.Null);Assert.That(torch.flame,Is.Not.Null);
                Assert.That(torch.pointLight.shadows,Is.EqualTo(LightShadows.None));
                Assert.That(torch.flame.GetComponentsInChildren<Collider>(true).Length,Is.EqualTo(0));
            }
        }
        foreach(string name in new[]{"Llama","Llama_Centro"})
            Assert.That(AssetDatabase.LoadAssetAtPath<Material>(F+"/Materials/"+name+".mat").IsKeywordEnabled("_EMISSION"),Is.True,name);
    }
    [Test]
    public void TextureSetIsRepeatableAndMaterialsReferenceIncludedTextures()
    {
        var textures=AssetDatabase.FindAssets("t:Texture2D",new[]{F+"/Textures"});
        Assert.That(textures.Length,Is.EqualTo(6));
        foreach(string guid in textures)
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.wrapMode,Is.EqualTo(TextureWrapMode.Repeat));
            Assert.That(importer.sRGBTexture,Is.True);
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(texture.width,Is.EqualTo(texture.height));Assert.That(texture.width,Is.GreaterThanOrEqualTo(1024));
        }
    }
    static string PathOf(Transform t)=> t.parent ? PathOf(t.parent)+"/"+t.name:t.name;
    [Test]
    public void ThemedExamplePreservesProBuilderRoadsLapTrapsAndRockPaths()
    {
        var source=EditorSceneManager.OpenScene("Assets/DungeonTrack/DungeonCircuit_Rocas.unity",OpenSceneMode.Additive);
        var result=EditorSceneManager.OpenScene(MedievalTrackTheme.ScenePath,OpenSceneMode.Additive);
        try
        {
            var before=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ProBuilderMesh>(true)).ToDictionary(p=>PathOf(p.transform));
            var roots=result.GetRootGameObjects();
            var after=roots.SelectMany(g=>g.GetComponentsInChildren<ProBuilderMesh>(true)).ToDictionary(p=>PathOf(p.transform));
            Assert.That(after.Count,Is.EqualTo(before.Count));
            foreach(var pair in before)
            {
                Assert.That(after[pair.Key].positions,Is.EqualTo(pair.Value.positions),pair.Key);
                Assert.That(after[pair.Key].transform.position,Is.EqualTo(pair.Value.transform.position),pair.Key);
            }
            var controller=roots.SelectMany(g=>g.GetComponentsInChildren<LapTrapController>(true)).Single();
            Assert.That(controller.traps.Count,Is.EqualTo(4));
            var rocks=controller.traps.OfType<RockTrap>().Single();
            Assert.That(rocks.routes.Count,Is.EqualTo(3));
            var originalRock=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RockTrap>(true)).Single();
            for(int i=0;i<3;i++)
            {Assert.That(rocks.routes[i].TryGetPath(out var a),Is.True);originalRock.routes[i].TryGetPath(out var b);Assert.That(a,Is.EqualTo(b));}
            Assert.That(roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Count(t=>t.name=="10_Decoracion_MedievalCartoon"),Is.EqualTo(1));
            var decor=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="10_Decoracion_MedievalCartoon");
            var circuit=decor.parent;
            var roadColliders=circuit.Find("01_Pista_Editable_18_24_32m").GetComponentsInChildren<Collider>();
            Physics.SyncTransforms();
            var overlaps=new List<string>();
            foreach(Transform prop in decor)
            {
                if(prop.name.StartsWith("Arco_Abierto"))continue;
                foreach(var collider in prop.GetComponentsInChildren<Collider>())
                {
                    var b=collider.bounds;
                    foreach(var point in new[]{b.center,new Vector3(b.min.x,0,b.min.z),new Vector3(b.max.x,0,b.max.z),new Vector3(b.min.x,0,b.max.z),new Vector3(b.max.x,0,b.min.z)})
                        if(roadColliders.Any(r=>r.Raycast(new Ray(new Vector3(point.x,100,point.z),Vector3.down),out _,200)))
                            overlaps.Add(prop.name+" "+point);
                }
            }
            Assert.That(overlaps,Is.Empty,"Decoracion sobre pista: "+string.Join("; ",overlaps));
        }
        finally{EditorSceneManager.CloseScene(result,true);EditorSceneManager.CloseScene(source,true);}
    }
}

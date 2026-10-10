using System.Linq;
using MedievalCartoon;
using MedievalCartoon.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CartoonNightSkyEditorTests
{
    [Test] public void CircuitUsesOneNightControllerCameraOverridesAndHidesTheOldSunDisc()
    {
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.OpenScene("Assets/DungeonTrack/DungeonCircuit_AnilloLava.unity",OpenSceneMode.Additive);
        const string testPath="Assets/Tests/Editor/NightSkyTestScene.unity";
        string profile=null;
        try
        {
            SceneManager.SetActiveScene(scene);
            var light=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>()).First(l=>l.type==LightType.Directional);
            RenderSettings.sun=light;
            var sky=CartoonNightSkyBuilder.Apply(scene);profile=AssetDatabase.GetAssetPath(sky.skyboxMaterial);
            Assert.That(sky.moonLight,Is.SameAs(light));
            Assert.That(CartoonNightSkyBuilder.Apply(scene),Is.SameAs(sky));
            Assert.That(EditorSceneManager.SaveScene(scene,testPath),Is.True);
            EditorSceneManager.CloseScene(scene,true);
            scene=EditorSceneManager.OpenScene(testPath,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
            var roots=scene.GetRootGameObjects();sky=roots.SelectMany(g=>g.GetComponentsInChildren<CartoonNightSky>(true)).Single();sky.Apply();
            Assert.That(RenderSettings.skybox,Is.SameAs(sky.skyboxMaterial));Assert.That(RenderSettings.sun,Is.SameAs(sky.moonLight));
            Assert.That(sky.cameraSkyboxes.Length,Is.GreaterThan(0));
            Assert.That(sky.cameraSkyboxes.All(b=>b!=null && b.material==sky.skyboxMaterial),Is.True);
            foreach(var r in sky.moonLight.GetComponentsInChildren<Renderer>(true))
                if(r.sharedMaterials.Any(m=>m!=null && AssetDatabase.GetAssetPath(m)=="Assets/Karting/Art/Materials/Level/Sun.mat"))Assert.That(r.enabled,Is.False);
            Assert.That(AssetDatabase.Contains(sky.skyboxMaterial),Is.True);
        }
        finally
        {
            if(scene.IsValid() && scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
            if(previous.IsValid() && previous.isLoaded)SceneManager.SetActiveScene(previous);
            AssetDatabase.DeleteAsset(testPath);if(profile!=null)AssetDatabase.DeleteAsset(profile);
        }
    }
    [Test] public void NightShaderAndPrefabHaveValidPersistentReferences()
    {
        var shader=Shader.Find("Medieval Cartoon/Night Sky");Assert.That(shader,Is.Not.Null);
        Assert.That(ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),Is.Empty);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(CartoonNightSkyBuilder.PrefabPath);Assert.That(prefab,Is.Not.Null);
        var sky=prefab.GetComponent<CartoonNightSky>();Assert.That(sky.skyboxMaterial.shader,Is.SameAs(shader));
        Assert.That(sky.moonLight.type,Is.EqualTo(LightType.Directional));Assert.That(sky.moonLight.intensity,Is.GreaterThan(0));
        Assert.That(AssetDatabase.Contains(sky.skyboxMaterial),Is.True);
    }
    [Test] public void RotatingDirectionalLightMovesVisibleMoonInTheOppositeRayDirection()
    {
        var root=new GameObject("Test sky");root.SetActive(false);
        var light=root.AddComponent<Light>();light.type=LightType.Directional;
        var material=new Material(Shader.Find("Medieval Cartoon/Night Sky"));
        var sky=root.AddComponent<CartoonNightSky>();sky.moonLight=light;sky.skyboxMaterial=material;
        try
        {
            root.SetActive(true);light.transform.rotation=Quaternion.Euler(35,127,0);sky.SyncMoonDirection();
            Vector3 direction=material.GetVector("_MoonDirection");
            Assert.That(Vector3.Dot(direction,-light.transform.forward),Is.EqualTo(1).Within(.00001f));
            Assert.That(direction.y,Is.GreaterThan(0));Assert.That(RenderSettings.sun,Is.SameAs(light));
            light.transform.rotation=Quaternion.Euler(60,45,0);sky.SyncMoonDirection();
            Assert.That(Vector3.Dot((Vector3)material.GetVector("_MoonDirection"),-light.transform.forward),Is.EqualTo(1).Within(.00001f));
        }
        finally{Object.DestroyImmediate(root);Object.DestroyImmediate(material);}
    }
}

using System.Collections;
using MedievalCartoon;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CartoonNightSkyTests
{
    [UnityTest] public IEnumerator RuntimeMoonUsesPrivateMaterialAndDisablingRestoresTheSky()
    {
        var shader=Shader.Find("Medieval Cartoon/Night Sky");Assert.That(shader,Is.Not.Null);
        var source=new Material(shader);source.SetVector("_MoonDirection",Vector3.up);
        var previous=RenderSettings.skybox;var originalSun=RenderSettings.sun;
        var originalLightObject=new GameObject("Previous directional light");var previousSun=originalLightObject.AddComponent<Light>();previousSun.type=LightType.Directional;
        RenderSettings.sun=previousSun;
        var root=new GameObject("Runtime night sky");root.SetActive(false);
        var light=root.AddComponent<Light>();light.type=LightType.Directional;
        var camera=root.AddComponent<Camera>();camera.enabled=false;var cameraSky=root.AddComponent<Skybox>();cameraSky.material=previous;
        var sky=root.AddComponent<CartoonNightSky>();sky.skyboxMaterial=source;sky.moonLight=light;
        sky.cameraSkyboxes=new[]{cameraSky};
        try
        {
            root.SetActive(true);light.transform.rotation=Quaternion.Euler(42,115,0);yield return null;
            Assert.That(RenderSettings.skybox,Is.Not.SameAs(source));
            Assert.That(cameraSky.material,Is.SameAs(sky.AppliedMaterial));
            Assert.That(Vector3.Dot((Vector3)sky.AppliedMaterial.GetVector("_MoonDirection"),-light.transform.forward),Is.EqualTo(1).Within(.00001f));
            Assert.That((Vector3)source.GetVector("_MoonDirection"),Is.EqualTo(Vector3.up));
            sky.enabled=false;Assert.That(RenderSettings.skybox,Is.SameAs(previous));Assert.That(RenderSettings.sun,Is.SameAs(previousSun));
            Assert.That(cameraSky.material,Is.SameAs(previous));
            yield return null;
        }
        finally{Object.DestroyImmediate(root);Object.DestroyImmediate(source);Object.DestroyImmediate(originalLightObject);RenderSettings.sun=originalSun;}
    }
}

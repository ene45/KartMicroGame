using System.Collections;
using MedievalCartoon;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CartoonTorchTests
{
    [UnityTest]
    public IEnumerator TorchCanDisableLightIndependentlyAndRestoresAfterReenable()
    {
        var root=new GameObject("TorchTest");root.SetActive(false);
        var lightObj=new GameObject("Light");lightObj.transform.SetParent(root.transform);
        var light=lightObj.AddComponent<Light>();
        var flame=new GameObject("Flame");flame.transform.SetParent(root.transform);flame.transform.localScale=new Vector3(2,3,4);
        var torch=root.AddComponent<CartoonTorch>();torch.pointLight=light;torch.flame=flame.transform;
        torch.intensity=3;torch.range=11;torch.flicker=.2f;root.SetActive(true);
        try
        {
            yield return null;
            Assert.That(light.enabled,Is.True);Assert.That(light.range,Is.EqualTo(11));
            Assert.That(light.intensity,Is.InRange(2.4f,3.6f));Assert.That(light.shadows,Is.EqualTo(LightShadows.None));
            Assert.That(flame.transform.localScale.x,Is.EqualTo(2));Assert.That(flame.transform.localScale.z,Is.EqualTo(4));
            torch.lightEnabled=false;yield return null;
            Assert.That(light.enabled,Is.False);Assert.That(flame.activeSelf,Is.True);
            torch.enabled=false;torch.lightEnabled=true;torch.enabled=true;yield return null;
            Assert.That(light.enabled,Is.True);
        }
        finally{Object.Destroy(root);}
    }
}

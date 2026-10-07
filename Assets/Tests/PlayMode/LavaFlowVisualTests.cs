using System.Collections;
using MedievalCartoon;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class LavaFlowVisualTests
{
    [UnityTest]
    public IEnumerator FlowScrollsEachInstanceAndRestoresItsOriginalOverrides()
    {
        var obj = new GameObject("Lava_Test");
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        try
        {
            var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            int stId = Shader.PropertyToID("_BaseMap_ST");
            int colorId = Shader.PropertyToID("_BaseColor");
            var original = new MaterialPropertyBlock();
            var offset = new Vector4(2,3,.4f,.6f);
            original.SetVector(stId, offset); original.SetColor(colorId, Color.magenta); renderer.SetPropertyBlock(original);
            Vector4 sharedBefore = material.GetVector(stId);
            var flow = obj.AddComponent<LavaFlowVisual>(); flow.enabled = false;
            flow.surfaces = new Renderer[] { renderer }; flow.flowSpeed = .5f; flow.enabled = true;
            yield return null; yield return null; yield return null;
            var current = new MaterialPropertyBlock(); renderer.GetPropertyBlock(current);
            Assert.That(current.GetVector(stId).w, Is.GreaterThan(offset.w));
            Assert.That(current.GetColor(colorId), Is.EqualTo(Color.magenta));
            Assert.That(material.GetVector(stId), Is.EqualTo(sharedBefore));
            Assert.That(renderer.sharedMaterial, Is.SameAs(material));
            flow.enabled = false; renderer.GetPropertyBlock(current);
            Assert.That(current.GetVector(stId), Is.EqualTo(offset));
            Assert.That(current.GetColor(colorId), Is.EqualTo(Color.magenta));
        }
        finally { Object.Destroy(obj); Object.Destroy(material); }
    }
}

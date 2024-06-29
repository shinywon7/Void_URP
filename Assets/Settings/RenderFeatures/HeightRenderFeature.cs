using System.Reflection;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HeightRenderFeature : ScriptableRendererFeature
{
    public LayerMask originMask, flipMask;
    public string[] frontCullShaderTagIds = new string[]{};
    public string[] backCullShaderTagIds = new string[]{};

    MyRenderPass frontCullPass, backCullPass;

    public override void Create()
    {
        RenderPassEvent evt = RenderPassEvent.AfterRenderingOpaques;

        frontCullPass = new MyRenderPass(evt++, originMask, flipMask, frontCullShaderTagIds, RenderQueueRange.opaque, ClearFlag.All);
        backCullPass = new MyRenderPass(evt++, originMask, flipMask, backCullShaderTagIds, RenderQueueRange.opaque, ClearFlag.None);
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(frontCullPass);
        renderer.EnqueuePass(backCullPass);
    }
}



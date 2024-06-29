using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutMergePass : ScriptableRenderPass
{
    Material material;
    public OutMergePass(RenderPassEvent evt, Material mat)
    {
        renderPassEvent = evt;
        material = mat;
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();
        cmd.Clear();
        
        cmd.Blit("_CameraColorAttachmentA","_CameraColorAttachmentA", material);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}

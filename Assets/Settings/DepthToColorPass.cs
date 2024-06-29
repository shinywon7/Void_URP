using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

public class DepthToColorPass : ScriptableRenderPass
{
    Material material;
    RenderTargetHandle destination;
    public DepthToColorPass(RenderPassEvent evt, Material mat)
    {
        renderPassEvent = evt;
        material = mat;
    }
    public void Setup(RenderTargetHandle source,RenderTargetHandle destination){
        this.destination = destination;
    }
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.msaaSamples = 1;
        descriptor.depthBufferBits = 0;
        cmd.GetTemporaryRT(destination.id, descriptor, FilterMode.Point);
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();
        
        cmd.Blit("_CameraColorAttachmentA",destination.Identifier(), material);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(destination.id);
        //destination = RenderTargetHandle.CameraTarget;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

public class MyCopyColorPass : ScriptableRenderPass
{
    private RTHandle source;
    private RTHandle destination;

    public MyCopyColorPass(RenderPassEvent evt)
    {
        renderPassEvent = evt;
    }

    public void Setup(RTHandle source, RTHandle destination)
    {
        this.source = source;
        this.destination = destination;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.colorFormat = RenderTextureFormat.ARGBFloat;
        descriptor.msaaSamples = 1;
        cmd.GetTemporaryRT(Shader.PropertyToID(destination.name), descriptor, FilterMode.Point);
    }

    /// <inheritdoc/>
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {

        CommandBuffer cmd = CommandBufferPool.Get();
        cmd.SetGlobalTexture("_CameraDepthAttachment", source.nameID);

        Blitter.BlitCameraTexture(cmd,source,destination);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(Shader.PropertyToID(destination.name));
    }
}

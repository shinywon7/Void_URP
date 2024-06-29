using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RecalcDepthPass : ScriptableRenderPass
{
    private RenderTargetHandle source { get; set; }
    private RenderTargetHandle destination { get; set; }
    internal bool AllocateRT { get; set; }
    internal int MssaSamples { get; set; }
    Material m_CopyDepthMaterial;
    int index;
    public RecalcDepthPass(RenderPassEvent evt, Material copyDepthMaterial, int i)
    {
        //base.profilingSampler = new ProfilingSampler(nameof(MyCopyDepthPass));
        AllocateRT = true;
        m_CopyDepthMaterial = copyDepthMaterial;
        renderPassEvent = evt;
        index = i;
    }

    public void Setup(RenderTargetHandle source, RenderTargetHandle destination)
    {
        this.source = source;
        this.destination = destination;
        //this.AllocateRT = !destination.HasInternalRenderTargetId();
        this.MssaSamples = -1;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.colorFormat = RenderTextureFormat.Depth;
        //descriptor.depthBufferBits = UniversalRenderer.k_DepthStencilBufferBits;
        descriptor.msaaSamples = 1;
        if (this.AllocateRT)
            cmd.GetTemporaryRT(destination.id, descriptor, FilterMode.Point);

        // On Metal iOS, prevent camera attachments to be bound and cleared during this pass.
        ConfigureTarget(destination.Identifier());
        ConfigureClear(ClearFlag.None, Color.black);
    }

    /// <inheritdoc/>
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        
        cmd.SetGlobalTexture("_CameraDepthAttachment", source.Identifier());

        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_CopyDepthMaterial,0,index);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);

    }

    /// <inheritdoc/>
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        if (cmd == null)
            throw new ArgumentNullException("cmd");

        cmd.ReleaseTemporaryRT(destination.id);
        destination = RenderTargetHandle.CameraTarget;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

public class FinalPass : ScriptableRenderPass
{
    private RTHandle source, destination;
    private Material _material;
    private Shader _shader;

    public FinalPass(RenderPassEvent passEvent, Shader shader)
    {
        renderPassEvent = passEvent;
        _shader = shader;

    }


    public void Setup(RTHandle source, RTHandle destination)
    {
        this.source = source;
        this.destination = destination;

        if(!_material && _shader)
        {
            _material = CoreUtils.CreateEngineMaterial(_shader);
        }
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.msaaSamples = 1;
        cmd.GetTemporaryRT(Shader.PropertyToID(destination.name), descriptor, FilterMode.Point);
    }

    public void Destroy()
    {
        if (_material)
        {
            CoreUtils.Destroy(_material);
            _material = null;
        }
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!_material || source == null) return;

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, new ProfilingSampler("Final Pass")))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            Blitter.BlitCameraTexture(cmd,source,destination);
            Blitter.BlitTexture(cmd,destination,source,_material,0);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(Shader.PropertyToID(destination.name));
    }
}

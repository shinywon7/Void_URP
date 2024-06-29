using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Copy the given depth buffer into the given destination depth buffer.
///
/// You can use this pass to copy a depth buffer to a destination,
/// so you can use it later in rendering. If the source texture has MSAA
/// enabled, the pass uses a custom MSAA resolve. If the source texture
/// does not have MSAA enabled, the pass uses a Blit or a Copy Texture
/// operation, depending on what the current platform supports.
/// </summary>
public class MyCopyDepthPass : ScriptableRenderPass
{
    private RTHandle source;
    private RTHandle destination;
    internal int MssaSamples;
    Material copyDepthMaterial;
    bool outputDepth;
    ClearFlag _clearFlag;
    int pass;
    public MyCopyDepthPass(RenderPassEvent evt, Material copyDepthMaterial, bool outputDepth, ClearFlag clearFlag, int pass = 0)
    {   
        //base.profilingSampler = new ProfilingSampler(nameof(MyCopyDepthPass));
        this._clearFlag = clearFlag;
        this.copyDepthMaterial = copyDepthMaterial;
        this.outputDepth = outputDepth;
        this.pass = pass;
        renderPassEvent = evt;
    }

    public void Setup(RTHandle source, RTHandle destination)
    {
        this.source = source;
        this.destination = destination;
        this.MssaSamples = -1;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.colorFormat = RenderTextureFormat.ARGBFloat;
        descriptor.msaaSamples = 1;
        //descriptor.depthBufferBits = 0;
        cmd.GetTemporaryRT(Shader.PropertyToID(destination.name), descriptor, FilterMode.Point);
        
        // On Metal iOS, prevent camera attachments to be bound and cleared during this pass.
        //ConfigureTarget(destination);
        //ConfigureClear(_clearFlag, new Color(0,0,0,0));
    }

    /// <inheritdoc/>
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        
        CommandBuffer cmd = CommandBufferPool.Get();
        cmd.SetGlobalTexture("_CameraDepthAttachment", source.nameID);
        
        using (new ProfilingScope(cmd, new ProfilingSampler("My Copy Depth Pass")))
        {
            int cameraSamples = 0;
            if (MssaSamples == -1)
            {
                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                cameraSamples = descriptor.msaaSamples;
            }
            else
                cameraSamples = MssaSamples;

            // When auto resolve is supported or multisampled texture is not supported, set camera samples to 1
            if (SystemInfo.supportsMultisampledTextures == 0)
                cameraSamples = 1;

            //CameraData cameraData = renderingData.cameraData;

            switch (cameraSamples)
            {
                case 8:
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                    cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                    break;

                case 4:
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                    cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                    break;

                case 2:
                    cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                    break;

                // MSAA disabled, auto resolve supported or ms textures not supported
                default:
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                    break;
            }
            if(outputDepth){
                cmd.EnableShaderKeyword("_OUTPUT_DEPTH");
            }
            else{
                cmd.DisableShaderKeyword("_OUTPUT_DEPTH");
            }
            //cmd.SetGlobalTexture("_CameraDepthAttachment", source.Identifier());

            //cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, copyDepthMaterial);
        //}
            var cameraData = renderingData.cameraData;
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
            bool yflip = cameraData.IsHandleYFlipped(source) != cameraData.IsHandleYFlipped(destination);
            Vector4 scaleBias = yflip ? new Vector4(viewportScale.x, -viewportScale.y, 0, viewportScale.y) : new Vector4(viewportScale.x, viewportScale.y, 0, 0);
            //Blitter.BlitTexture(cmd, source, scaleBias, copyDepthMaterial, pass);
            Blitter.BlitCameraTexture(cmd, source, destination, copyDepthMaterial, pass);
        }
        //cmd.SetGlobalTexture(name, destination.Identifier());
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    /// <inheritdoc/>
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(Shader.PropertyToID(destination.name));
    }
}

using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HeightSimulateFeature : ScriptableRendererFeature
{
    public Material frontMat, backMat;
    public RenderTexture FrontHeightRenderTexture, FrontTempHeightRenderTexture,FrontRelativeVelocityTexture,BackHeightRenderTexture, BackTempHeightRenderTexture, BackRelativeVelocityTexture;
    public string FrontHeightRenderTextureName, FrontTempHeightRenderTextureName,FrontRelativeVelocityTextureName,BackHeightRenderTextureName, BackTempHeightRenderTextureName, BackRelativeVelocityTextureName;

    public RTHandle FrontHeightTexture, FrontTempHeightTexture,BackHeightTexture, BackTempHeightTexture;
    public HeightSimulatePass HeightSimulatePass;
    public override void Create()
    {
        RenderPassEvent evt = RenderPassEvent.BeforeRenderingOpaques;
        
        FrontHeightTexture = RTHandles.Alloc(FrontHeightRenderTexture, name: FrontHeightRenderTextureName);
        FrontTempHeightTexture = RTHandles.Alloc(FrontTempHeightRenderTexture, name:FrontTempHeightRenderTextureName);
        BackHeightTexture = RTHandles.Alloc(BackHeightRenderTexture, name: BackHeightRenderTextureName);
        BackTempHeightTexture = RTHandles.Alloc(BackTempHeightRenderTexture, name:BackTempHeightRenderTextureName);
        Shader.SetGlobalTexture(FrontHeightRenderTextureName,FrontHeightRenderTexture);
        Shader.SetGlobalTexture(FrontTempHeightRenderTextureName,FrontTempHeightRenderTexture);
        Shader.SetGlobalTexture(FrontRelativeVelocityTextureName,FrontRelativeVelocityTexture);
        Shader.SetGlobalTexture(BackHeightRenderTextureName,BackHeightRenderTexture);
        Shader.SetGlobalTexture(BackTempHeightRenderTextureName,BackTempHeightRenderTexture);
        Shader.SetGlobalTexture(BackRelativeVelocityTextureName,BackRelativeVelocityTexture);
        HeightSimulatePass = new HeightSimulatePass(evt, frontMat,backMat, FrontHeightTexture,FrontTempHeightTexture,BackHeightTexture,BackTempHeightTexture);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        HeightSimulatePass.Setup(renderer.cameraColorTargetHandle);
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(HeightSimulatePass);
    }
}

using System.Reflection;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VoidRenderFeature : ScriptableRendererFeature
{
    public LayerMask backMask, voidMask, frontMask, sectionMask, stencilMask, flipMask;
    public string[] backShaderTagIds = new string[]{};
    public string[] frontShaderTagIds = new string[]{};
    public string[] voidShaderTagIds = new string[]{};
    public string[] sectionShaderTagIds = new string[]{};
    public string[] frontDepthShaderTagIds = new string[]{};
    public string[] backDepthShaderTagIds = new string[]{};
    public string[] stencilShaderTagIds = new string[]{};
    public int blurIteration;
    //BackRenderPass backRenderPass;
    MyCopyColorPass frontCopyColorPass, voidCopyColorPass, backCopyColorPass, stencilCopyPass, depthCopyPass;
    MyRenderPass frontOpaquePass,voidOpaquePass, backOpaquePass,frontTransparentPass,voidTransparentPass, backTransparentPass, sectionPass;
    MyRenderPass frontDepthPass, backDepthPass, stencilPass;
    BloomPass bloomPass;
    FinalPass finalPass;

    Matrix4x4 backViewMatrix = Matrix4x4.identity, frontViewMatrix = Matrix4x4.identity;
    ScriptableCullingParameters backCullingparameters, frontCullingparameters;
    RTHandle copyDepth;
    RTHandle backSide, voidSide, frontSide;
    RTHandle sectionTemp, stencilTemp;

    public override void Create()
    {
        copyDepth = RTHandles.Alloc("_CopyDepthTexture","_CopyDepthTexture");
        frontSide = RTHandles.Alloc("_FrontSideTexture","_FrontSideTexture");
        voidSide = RTHandles.Alloc("_VoidSideTexture","_VoidSideTexture");
        backSide = RTHandles.Alloc("_BackSideTexture","_BackSideTexture");
        sectionTemp = RTHandles.Alloc("_SectionTempTexture","_SectionTempTexture");
        stencilTemp = RTHandles.Alloc("_StencilTempTexture","_StencilTempTexture");
        RenderPassEvent evt = RenderPassEvent.AfterRenderingOpaques;

        stencilPass = new MyRenderPass(evt++, stencilMask,0, stencilShaderTagIds, RenderQueueRange.transparent, ClearFlag.All, 1);
        stencilCopyPass = new MyCopyColorPass(evt++);

        backDepthPass = new MyRenderPass(evt++, backMask, flipMask, backDepthShaderTagIds, RenderQueueRange.all, ClearFlag.All, 2);
        frontDepthPass = new MyRenderPass(evt++, frontMask|voidMask, flipMask, frontDepthShaderTagIds, RenderQueueRange.all, ClearFlag.Depth, 1);
        depthCopyPass = new MyCopyColorPass(evt++);
        //backRenderPass = new BackRenderPass(evt, backMask, backShaderTagIds);
        backOpaquePass = new MyRenderPass(evt++, backMask,flipMask, backShaderTagIds, RenderQueueRange.opaque, ClearFlag.All, 2);
        backTransparentPass = new MyRenderPass(evt++, backMask,flipMask, backShaderTagIds, RenderQueueRange.transparent, ClearFlag.None, 2);
        backCopyColorPass = new MyCopyColorPass(evt);
        //backCopyDepthPass = new MyCopyDepthPass(evt++, new Material(Shader.Find("Hidden/Void/Blit/MyCopyDepth")), false, ClearFlag.None, 0);

        voidOpaquePass = new MyRenderPass(evt++, voidMask,flipMask, voidShaderTagIds, RenderQueueRange.opaque, ClearFlag.All, 1);
        voidTransparentPass = new MyRenderPass(evt++, voidMask,flipMask, voidShaderTagIds, RenderQueueRange.transparent, ClearFlag.None, 1);
        voidCopyColorPass = new MyCopyColorPass(evt);
        //voidCopyDepthPass = new MyCopyDepthPass(evt++, new Material(Shader.Find("Hidden/Void/Blit/MyCopyDepth")), false, ClearFlag.None, 1);


        frontOpaquePass = new MyRenderPass(evt++, frontMask,flipMask, frontShaderTagIds, RenderQueueRange.opaque, ClearFlag.All, 1);
        frontTransparentPass = new MyRenderPass(evt++, frontMask,flipMask, frontShaderTagIds, RenderQueueRange.transparent, ClearFlag.None, 1);
        frontCopyColorPass = new MyCopyColorPass(evt);
        //frontCopyDepthPass = new MyCopyDepthPass(evt++, new Material(Shader.Find("Hidden/Void/Blit/MyCopyDepth")), false, ClearFlag.None, 2);


        bloomPass = new BloomPass(evt++,Shader.Find("Hidden/Bloom"));
        sectionPass = new MyRenderPass(evt++, sectionMask,0, sectionShaderTagIds, RenderQueueRange.opaque, ClearFlag.All, 1);
        evt = RenderPassEvent.AfterRenderingTransparents;
        finalPass = new FinalPass(evt++, Shader.Find("Hidden/Void/Blit/FinalBlit"));
    }
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        Matrix4x4 projectionMatrix = renderingData.cameraData.GetProjectionMatrix();
        backViewMatrix = Player.backCamTransform.worldToLocalMatrix;
        frontViewMatrix = Player.headTransform.worldToLocalMatrix;
        Player.mainCam.TryGetCullingParameters(out backCullingparameters);
        var planes = GeometryUtility.CalculateFrustumPlanes(projectionMatrix*backViewMatrix);
        for (int i = 0; i <6;i++){
            backCullingparameters.SetCullingPlane(i,planes[i]);
        }

        stencilPass.Setup(frontViewMatrix);
        stencilCopyPass.Setup(renderer.cameraColorTargetHandle, stencilTemp);

        backDepthPass.Setup(backViewMatrix, backCullingparameters);
        frontDepthPass.Setup(frontViewMatrix);
        depthCopyPass.Setup(renderer.cameraColorTargetHandle, copyDepth);
        //backRenderPass.Setup(backViewMatrix, backCullingparameters);

        backOpaquePass.Setup(backViewMatrix, backCullingparameters);
        backTransparentPass.Setup(backViewMatrix, backCullingparameters);
        backCopyColorPass.Setup(renderer.cameraColorTargetHandle, backSide);
        //backCopyDepthPass.Setup(renderer.cameraDepthTargetHandle, copyDepth);

        voidOpaquePass.Setup(frontViewMatrix);
        voidTransparentPass.Setup(frontViewMatrix);
        voidCopyColorPass.Setup(renderer.cameraColorTargetHandle, voidSide);
        //voidCopyDepthPass.Setup(renderer.cameraDepthTargetHandle, copyDepth);


        frontOpaquePass.Setup(frontViewMatrix);
        frontTransparentPass.Setup(frontViewMatrix);
        frontCopyColorPass.Setup(renderer.cameraColorTargetHandle, frontSide);
        //frontCopyDepthPass.Setup(renderer.cameraDepthTargetHandle, copyDepth);


        bloomPass.Setup(copyDepth, blurIteration);
        sectionPass.Setup(frontViewMatrix);
        finalPass.Setup(renderer.cameraColorTargetHandle,sectionTemp);
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(stencilPass);
        renderer.EnqueuePass(stencilCopyPass);

        renderer.EnqueuePass(backDepthPass);
        renderer.EnqueuePass(frontDepthPass);
        renderer.EnqueuePass(depthCopyPass);

        renderer.EnqueuePass(backOpaquePass);
        renderer.EnqueuePass(backTransparentPass);
        renderer.EnqueuePass(backCopyColorPass);
        //renderer.EnqueuePass(backCopyDepthPass);

        renderer.EnqueuePass(voidOpaquePass);
        renderer.EnqueuePass(voidTransparentPass);
        renderer.EnqueuePass(voidCopyColorPass);
        //renderer.EnqueuePass(voidCopyDepthPass);

        renderer.EnqueuePass(frontOpaquePass);
        renderer.EnqueuePass(frontTransparentPass);
        renderer.EnqueuePass(frontCopyColorPass);
        //renderer.EnqueuePass(frontCopyDepthPass);


        renderer.EnqueuePass(bloomPass);
        renderer.EnqueuePass(sectionPass);
        renderer.EnqueuePass(finalPass);
    }
}



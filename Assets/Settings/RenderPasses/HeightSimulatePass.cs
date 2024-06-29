using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HeightSimulatePass : ScriptableRenderPass
{
    RTHandle FrontHeightTexture,FrontTempHeightTexture,BackHeightTexture,BackTempHeightTexture, source;
    Material frontMat, backMat;
    public HeightSimulatePass(RenderPassEvent evt, Material frontMat,Material backMat, RTHandle FrontHeightTexture,RTHandle FrontTempHeightTexture, RTHandle BackHeightTexture,RTHandle BackTempHeightTexture){
        renderPassEvent = evt;
        this.frontMat = frontMat;
        this.backMat = backMat;
        this.FrontHeightTexture = FrontHeightTexture;
        this.FrontTempHeightTexture = FrontTempHeightTexture;
        this.BackHeightTexture = BackHeightTexture;
        this.BackTempHeightTexture = BackTempHeightTexture;
    }
    public void Setup(RTHandle Source){
        this.source = Source;
    }
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        //cmd.GetTemporaryRT(Shader.PropertyToID(HeightTexture.name), descriptor, FilterMode.Point);
        //ConfigureTarget(HeightTexture);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();

        //cmd.SetGlobalTexture("_HeightTexture",HeightTexture);
        using (new ProfilingScope(cmd, new ProfilingSampler("Wave Render Pass"))){
            Blitter.BlitCameraTexture(cmd, source, FrontHeightTexture,frontMat,Player.isFlipped?1:0);
            Blitter.BlitCameraTexture(cmd, source, BackHeightTexture,backMat,Player.isFlipped?1:0);
            Blitter.BlitCameraTexture(cmd, source,FrontTempHeightTexture,frontMat,2);
            Blitter.BlitCameraTexture(cmd, source,BackTempHeightTexture,backMat,2);
            //cmd.Blit(Source,HeightTexture);
            //cmd.Blit(Source,Source,mat,0);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        //cmd.ReleaseTemporaryRT(Shader.PropertyToID(HeightTexture.name));
    }
}

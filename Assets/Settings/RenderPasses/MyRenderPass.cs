using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MyRenderPass : ScriptableRenderPass
{
    List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

    FilteringSettings OriginFilteringSettings, FlippedFilteringSettings;
    RenderStateBlock m_RenderStateBlock;
    Matrix4x4 viewMatrix;
    ScriptableCullingParameters cullingParameters;

    int side;
    bool renderOpaque;
    ClearFlag _clearFlag;

    public static CullingResults backCullResult;
    public static bool flipped = false;
    public MyRenderPass(RenderPassEvent evt, LayerMask layerMask, LayerMask flipMask, string[] shaderTagIds, RenderQueueRange renderQueueRange, ClearFlag clearFlag, int side = 0)
    {
        Initialize(evt, layerMask, flipMask, shaderTagIds, renderQueueRange, clearFlag, side);
    }
    public void Initialize(RenderPassEvent evt, LayerMask layerMask, LayerMask flipMask, string[] shaderTagIds, RenderQueueRange renderQueueRange, ClearFlag clearFlag, int side){
        this.side = side;
        this.renderOpaque = renderQueueRange != RenderQueueRange.transparent;
        this._clearFlag = clearFlag;
        renderPassEvent = evt;

        m_ShaderTagIdList = new List<ShaderTagId>();
        for (int i = 0; i < shaderTagIds.Length; i++)
        {
            m_ShaderTagIdList.Add(new ShaderTagId(shaderTagIds[i]));
        }
        OriginFilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
        FlippedFilteringSettings = new FilteringSettings(renderQueueRange, layerMask ^ flipMask);

        m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
        m_RenderStateBlock.mask |= RenderStateMask.Depth;
    }
    public void Setup(Matrix4x4 viewMatrix){
        this.viewMatrix = viewMatrix;
    }
    public void Setup(Matrix4x4 viewMatrix, ScriptableCullingParameters cullingParameters){
        this.cullingParameters = cullingParameters;
        Setup(viewMatrix);
    }
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        ConfigureClear(_clearFlag, new Color(0,0,0,0));
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();
        
        using (new ProfilingScope(cmd, new ProfilingSampler("My Render Pass"))){
            CullingResults cullResults = renderingData.cullResults;
            if(side != 0)cmd.SetViewMatrix(viewMatrix);
            if(side == 2){
                cullResults = context.Cull(ref cullingParameters);
            }
            
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            DrawingSettings drawSettings;
            if(!renderOpaque) m_RenderStateBlock.depthState = new DepthState(false, CompareFunction.LessEqual);
            drawSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, renderOpaque?renderingData.cameraData.defaultOpaqueSortFlags:SortingCriteria.CommonTransparent);
            if(flipped) context.DrawRenderers(cullResults, ref drawSettings, ref FlippedFilteringSettings, ref m_RenderStateBlock);
            else context.DrawRenderers(cullResults, ref drawSettings, ref OriginFilteringSettings, ref m_RenderStateBlock);
        }
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        //if(source != null)cmd.ReleaseTemporaryRT(Shader.PropertyToID(source.name));
    }
}

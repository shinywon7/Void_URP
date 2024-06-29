Shader "Hidden/Void/Blit/MyCopyDepth"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            Name "CopyDepth1"
            ZTest Always ZWrite On ColorMask R
            BlendOp Max
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #pragma multi_compile _ _DEPTH_MSAA_2 _DEPTH_MSAA_4 _DEPTH_MSAA_8
            #pragma multi_compile _ _OUTPUT_DEPTH

            #include "MyCopyDepthPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "CopyDepth2"
            ZTest Always ZWrite On ColorMask G
            BlendOp Max
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #pragma multi_compile _ _DEPTH_MSAA_2 _DEPTH_MSAA_4 _DEPTH_MSAA_8
            #pragma multi_compile _ _OUTPUT_DEPTH

            #include "MyCopyDepthPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "CopyDepth3"
            ZTest Always ZWrite On ColorMask B
            BlendOp Max
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #pragma multi_compile _ _DEPTH_MSAA_2 _DEPTH_MSAA_4 _DEPTH_MSAA_8
            #pragma multi_compile _ _OUTPUT_DEPTH

            #include "MyCopyDepthPass.hlsl"

            ENDHLSL
        }
    }
}

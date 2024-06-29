Shader "Hidden/OutMergePass"
{
    Properties{
        _Lerp("Lerp", float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
				float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                return o;
            }
            TEXTURE2D_X_FLOAT(_IColorTexture);
            SAMPLER(sampler_IColorTexture);
            TEXTURE2D_X_FLOAT(_COColorTexture);
            SAMPLER(sampler_COColorTexture);
            TEXTURE2D_X_FLOAT(_IDepthTexture);
            SAMPLER(sampler_IDepthTexture);
            TEXTURE2D_X_FLOAT(_SectionDepthTexture2);
            SAMPLER(sampler_SectionDepthTexture2);
            float _Lerp;
            
            float4 frag (v2f i) : SV_Target
            {
                float4 inColor = SAMPLE_TEXTURE2D_X(_IColorTexture, sampler_IColorTexture, UnityStereoTransformScreenSpaceTex(i.uv));
                float4 outColor = SAMPLE_TEXTURE2D_X(_COColorTexture, sampler_COColorTexture, UnityStereoTransformScreenSpaceTex(i.uv));
                float inDepth = Linear01Depth(SAMPLE_TEXTURE2D_X(_IDepthTexture, sampler_IDepthTexture, UnityStereoTransformScreenSpaceTex(i.uv)).r, _ZBufferParams);
                float inVoidDepth = Linear01Depth(SAMPLE_TEXTURE2D_X(_SectionDepthTexture2, sampler_SectionDepthTexture2, UnityStereoTransformScreenSpaceTex(i.uv)).r, _ZBufferParams);
                if(inDepth < inVoidDepth) return inColor;
                else return outColor;
				return 0;
            }
            ENDHLSL
        }
    }
}

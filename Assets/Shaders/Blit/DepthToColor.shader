Shader "Hidden/DepthToColor"
{
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
            TEXTURE2D_X_FLOAT(_ODepthTexture);
            SAMPLER(sampler_ODepthTexture);
            
            float4 frag (v2f i) : SV_Target
            {
                float4 Color = SAMPLE_TEXTURE2D_X(_ODepthTexture, sampler_ODepthTexture, UnityStereoTransformScreenSpaceTex(i.uv));
				return Color;
            }
            ENDHLSL
        }
    }
}

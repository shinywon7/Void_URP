Shader "Hidden/Bloom"
{
    Properties
    { 
       _blurOffset ("BlurOffset", float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue"="Geometry" "RenderPipeline" = "UniversalPipeline" }
       
        Pass
        {
            Name  "LayerFilterBlurRT"
            Tags {"LightMode" = "LayerFilterBlurRT"}
    
            HLSLPROGRAM
            #pragma target 4.5
            
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            TEXTURE2D(_DownsampleTex); SAMPLER(sampler_linear_clamp);
            float4 _DownsampleTex_TexelSize;

            float _blurOffset;
            

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            }; 
            

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float4 positionOS = input.positionOS;
                float3 positionWS = TransformObjectToWorld(positionOS.xyz);
                float4 positionCS = TransformWorldToHClip(positionWS);

                output.positionCS = positionCS;
                output.uv = input.uv;
                return output;
            } 
            half4 Sample (float2 uv) {
                return SAMPLE_TEXTURE2D(_DownsampleTex, sampler_linear_clamp, uv);
            }
            half4 SampleBox (float2 uv, float delta) {
                float4 o = _DownsampleTex_TexelSize.xyxy * float2(-delta, delta).xxyy;
                half4 s =
                    Sample(uv + o.xy) + Sample(uv + o.zy) +
                    Sample(uv + o.xw) + Sample(uv + o.zw);
                return s * 0.25f;
            }
            float4 frag(Varyings input) : SV_Target
            {
                return max(SampleBox(input.uv, 1), Sample(input.uv));
            }
            
            ENDHLSL
        }
        Pass
        {
            Name  "LayerFilterBlurRT"
            Tags {"LightMode" = "LayerFilterBlurRT"}
            //BlendOp Max
            HLSLPROGRAM
            #pragma target 4.5
            
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_DownsampleTex); SAMPLER(sampler_linear_clamp);
            float4 _DownsampleTex_TexelSize;

            float _blurOffset;
            

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            }; 
            

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float4 positionOS = input.positionOS;
                float3 positionWS = TransformObjectToWorld(positionOS.xyz);
                float4 positionCS = TransformWorldToHClip(positionWS);

                output.positionCS = positionCS;
                output.uv = input.uv;
                return output;
            } 
            half4 Sample (float2 uv) {
                return SAMPLE_TEXTURE2D(_DownsampleTex, sampler_linear_clamp, uv);
            }
            half4 SampleBox (float2 uv, float delta) {
                float4 o = _DownsampleTex_TexelSize.xyxy * float2(-delta, delta).xxyy;
                half4 s =
                    Sample(uv + o.xy) + Sample(uv + o.zy) +
                    Sample(uv + o.xw) + Sample(uv + o.zw);
                return s * 0.25f;
            }
            float4 frag(Varyings input) : SV_Target
            {
                //return SampleBox(input.uv, 0.5);
                return max(Sample(input.uv), Sample(input.uv)+SampleBox(input.uv, 0.5));
            }
            
            ENDHLSL
        }
    }
}

  
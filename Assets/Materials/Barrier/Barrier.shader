Shader "Void/Barrier"
{
    Properties
    {
        _LineTexture("LineTexture", 2D) = "white" {}
        [HDR] _Color("Color", Color) = (1,1,1,1)
        _LineProp("LinePorp", Vector) = (1,1,1,1)
        _NoiseProp("NoiseProp", Vector) = (1,1,1,1)
        _GradientScale("GradientScale", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalRenderPipeline" }

        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForwardVoid"
            }

            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl" 

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _LineProp;
            float4 _NoiseProp;
            float _GradientScale;
            CBUFFER_END

            TEXTURE2D(_LineTexture);
            SAMPLER(sampler_LineTexture);

            float _HalfVoidWidth;

            float Unity_SimpleNoise_ValueNoise_LegacySine_float (float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                uv = abs(frac(uv) - 0.5);
                float2 c0 = i + float2(0.0, 0.0);
                float2 c1 = i + float2(1.0, 0.0);
                float2 c2 = i + float2(0.0, 1.0);
                float2 c3 = i + float2(1.0, 1.0);
                float r0; Hash_LegacySine_2_1_float(c0, r0);
                float r1; Hash_LegacySine_2_1_float(c1, r1);
                float r2; Hash_LegacySine_2_1_float(c2, r2);
                float r3; Hash_LegacySine_2_1_float(c3, r3);
                float bottomOfGrid = lerp(r0, r1, f.x);
                float topOfGrid = lerp(r2, r3, f.x);
                float t = lerp(bottomOfGrid, topOfGrid, f.y);
                return t;
            }
            
            void Unity_SimpleNoise_LegacySine_float(float2 UV, float Scale, out float Out)
            {
                float freq, amp;
                Out = 0.0f;
                freq = pow(2.0, float(0));
                amp = pow(0.5, float(3-0));
                Out += Unity_SimpleNoise_ValueNoise_LegacySine_float(float2(UV.xy*(Scale/freq)))*amp;
                freq = pow(2.0, float(1));
                amp = pow(0.5, float(3-1));
                Out += Unity_SimpleNoise_ValueNoise_LegacySine_float(float2(UV.xy*(Scale/freq)))*amp;
                freq = pow(2.0, float(2));
                amp = pow(0.5, float(3-2));
                Out += Unity_SimpleNoise_ValueNoise_LegacySine_float(float2(UV.xy*(Scale/freq)))*amp;
            }

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 texcoord     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID                 
            };

            struct Varyings
            {
                    float3 positionWS               : TEXCOORD1;
                    float3 normalWS                 : TEXCOORD2;
                    float3 positionOS               : TEXCOORD3;
                    float4 positionCS               : SV_POSITION;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                    UNITY_VERTEX_OUTPUT_STEREO
            };            

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.normalWS = normalInput.normalWS;
                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;
                output.positionOS = input.positionOS.xyz;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 scale = half2(
                    length(unity_ObjectToWorld._m00_m10_m20),
                    length(unity_ObjectToWorld._m01_m11_m21)
                );
                float2 pos = input.positionOS.xy * scale;
                float dist = _HalfVoidWidth - abs(pos.y);
                float alpha = 1-saturate(dist/_GradientScale);

                float noise;
                float time = _Time.y;
                Unity_SimpleNoise_LegacySine_float(pos/_NoiseProp.xy+_NoiseProp.zw*time,1000,noise);
                noise = lerp(0,0.85,noise);
                noise *= alpha;
                float gradientAlpha = saturate(pow(alpha,5) - noise);

                float lineAlpha = SAMPLE_TEXTURE2D(_LineTexture, sampler_LineTexture, pos/_LineProp.xy+_LineProp.zw*time).a;
                lineAlpha = lerp(0,0.35,lineAlpha);

                return half4(_Color.rgb,max(gradientAlpha,lineAlpha));
            }
            ENDHLSL
        }
        pass{
            Tags
            {
                "LightMode" = "VoidDepth"
            }

            ZWrite On
            //ZTest LEqual
            //ColorMask G
            BlendOp Max
            Cull Off

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 position     : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.position.xyz);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                return float4(0,input.positionCS.z,0,0);
            }
            ENDHLSL
        }
    }
}
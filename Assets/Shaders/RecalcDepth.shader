Shader "Hidden/RecalcDepth"
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

            struct Attributes
            {
            #if _USE_DRAW_PROCEDURAL
                uint vertexID     : SV_VertexID;
            #else
                float4 positionHCS : POSITION;
                float2 uv         : TEXCOORD0;
            #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Note: CopyDepth pass is setup with a mesh already in CS
                // Therefore, we can just output vertex position

                // We need to handle y-flip in a way that all existing shaders using _ProjectionParams.x work.
                // Otherwise we get flipping issues like this one (case https://issuetracker.unity3d.com/issues/lwrp-depth-texture-flipy)

                // Unity flips projection matrix in non-OpenGL platforms and when rendering to a render texture.
                // If URP is rendering to RT:
                //  - Source Depth is upside down. We need to copy depth by using a shader that has flipped matrix as well so we have same orientaiton for source and copy depth.
                //  - This also guarantess to be standard across if we are using a depth prepass.
                //  - When shaders (including shader graph) render objects that sample depth they adjust uv sign with  _ProjectionParams.x. (https://docs.unity3d.com/Manual/SL-PlatformDifferences.html)
                //  - All good.
                // If URP is NOT rendering to RT neither rendering with OpenGL:
                //  - Source Depth is NOT fliped. We CANNOT flip when copying depth and don't flip when sampling. (ProjectionParams.x == 1)
            #if _USE_DRAW_PROCEDURAL
                output.positionCS = GetQuadVertexPosition(input.vertexID);
                output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
                output.uv = GetQuadTexCoord(input.vertexID);
            #else
                output.positionCS = float4(input.positionHCS.xyz, 1.0);
                output.uv = input.uv;
            #endif
                output.positionCS.y *= _ScaleBiasRt.x;
                return output;
            }
            TEXTURE2D_FLOAT(_SectionDepthTexture3);
            SAMPLER(sampler_SectionDepthTexture3);
            TEXTURE2D_FLOAT(_SectionDepthTexture4);
            SAMPLER(sampler_SectionDepthTexture4);
            
            float frag (Varyings i) : SV_Depth{
                float BackVoidDepth = SAMPLE_TEXTURE2D(_SectionDepthTexture3, sampler_SectionDepthTexture3, UnityStereoTransformScreenSpaceTex(i.uv)).r;
                float FrontVoidDepth = SAMPLE_TEXTURE2D(_SectionDepthTexture4, sampler_SectionDepthTexture4, UnityStereoTransformScreenSpaceTex(i.uv)).r;
                float BackEyeDepth = LinearEyeDepth(BackVoidDepth, _ZBufferParams);
                float FrontEyeDepth = LinearEyeDepth(FrontVoidDepth, _ZBufferParams);
                //return FrontVoidDepth;
                //return 1;
                if(BackEyeDepth > FrontEyeDepth) return FrontVoidDepth;
                else return 0;
            }
            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
            #if _USE_DRAW_PROCEDURAL
                uint vertexID     : SV_VertexID;
            #else
                float4 positionHCS : POSITION;
                float2 uv         : TEXCOORD0;
            #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Note: CopyDepth pass is setup with a mesh already in CS
                // Therefore, we can just output vertex position

                // We need to handle y-flip in a way that all existing shaders using _ProjectionParams.x work.
                // Otherwise we get flipping issues like this one (case https://issuetracker.unity3d.com/issues/lwrp-depth-texture-flipy)

                // Unity flips projection matrix in non-OpenGL platforms and when rendering to a render texture.
                // If URP is rendering to RT:
                //  - Source Depth is upside down. We need to copy depth by using a shader that has flipped matrix as well so we have same orientaiton for source and copy depth.
                //  - This also guarantess to be standard across if we are using a depth prepass.
                //  - When shaders (including shader graph) render objects that sample depth they adjust uv sign with  _ProjectionParams.x. (https://docs.unity3d.com/Manual/SL-PlatformDifferences.html)
                //  - All good.
                // If URP is NOT rendering to RT neither rendering with OpenGL:
                //  - Source Depth is NOT fliped. We CANNOT flip when copying depth and don't flip when sampling. (ProjectionParams.x == 1)
            #if _USE_DRAW_PROCEDURAL
                output.positionCS = GetQuadVertexPosition(input.vertexID);
                output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
                output.uv = GetQuadTexCoord(input.vertexID);
            #else
                output.positionCS = float4(input.positionHCS.xyz, 1.0);
                output.uv = input.uv;
            #endif
                output.positionCS.y *= _ScaleBiasRt.x;
                return output;
            }
            TEXTURE2D_FLOAT(_SectionDepthTexture3);
            SAMPLER(sampler_SectionDepthTexture3);
            TEXTURE2D_FLOAT(_SectionDepthTexture4);
            SAMPLER(sampler_SectionDepthTexture4);
            
            float frag (Varyings i) : SV_Depth{
                float BackVoidDepth = SAMPLE_TEXTURE2D(_SectionDepthTexture3, sampler_SectionDepthTexture3, UnityStereoTransformScreenSpaceTex(i.uv)).r;
                float FrontVoidDepth = SAMPLE_TEXTURE2D(_SectionDepthTexture4, sampler_SectionDepthTexture4, UnityStereoTransformScreenSpaceTex(i.uv)).r;
                float BackEyeDepth = LinearEyeDepth(BackVoidDepth, _ZBufferParams);
                float FrontEyeDepth = LinearEyeDepth(FrontVoidDepth, _ZBufferParams);
                //return BackVoidDepth;
                //return 1;
                if(BackEyeDepth < FrontEyeDepth) return BackVoidDepth;
                else return 1;
            }
            ENDHLSL
        }
    }
}

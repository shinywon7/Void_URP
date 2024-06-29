Shader "Void/CyberPath"
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
                "LightMode" = "UniversalForward"
            }

            ZWrite off
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
            float _GradientScale;
            CBUFFER_END
            float4x4 _w2fMatrix;
            float4x4 _w2bMatrix;
            float _VoidWidth;

            TEXTURE2D(_LineTexture);
            SAMPLER(sampler_LineTexture);
            TEXTURE2D_X_FLOAT(_FrontHeightTexture); SAMPLER(sampler_FrontHeightTexture);
            TEXTURE2D_X_FLOAT(_BackHeightTexture); SAMPLER(sampler_BackHeightTexture);
            TEXTURE2D_X_FLOAT(_StencilTempTexture); SAMPLER(sampler_StencilTempTexture);



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
                    float2 uv                       : TEXCOORD4;
                    float4 screenPos                : TEXCOORD5;
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
                output.uv = input.texcoord;

                output.screenPos = ComputeScreenPos(output.positionCS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.screenPos.xy/input.screenPos.w;
                float stencil = SAMPLE_TEXTURE2D_X(_StencilTempTexture, sampler_StencilTempTexture, uv).w;
                float3 vPos = mul(_w2fMatrix,float4(input.positionWS,1)).xyz;
                float height = SAMPLE_TEXTURE2D_X(_FrontHeightTexture, sampler_FrontHeightTexture, (vPos.xy+1)*0.5).x;
                if(stencil<1 && vPos.z < height) return 0;

                float2 pos = input.positionOS.xz;
                //float dist = _HalfVoidWidth - abs(pos.y);
                //float alpha = 1-saturate(dist/_GradientScale);

                float time = _Time.y;
                //float gradientAlpha = saturate(pow(alpha,5) - noise);

                float lineAlpha = SAMPLE_TEXTURE2D(_LineTexture, sampler_LineTexture, pos/_LineProp.xy+_LineProp.zw*time).a;
                float edgeAlpha = input.uv.g > 0 ? input.uv.r : 0;

                float alpha = lerp(0.25,_Color.a,lineAlpha);

                return half4(_Color.rgb, max(alpha,edgeAlpha));
            }
            ENDHLSL
        }
        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForwardVoid"
            }

            ZWrite off
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
            float _GradientScale;
            CBUFFER_END
            float4x4 _w2fMatrix;
            float4x4 _w2bMatrix;
            float _VoidWidth;

            TEXTURE2D(_LineTexture);
            SAMPLER(sampler_LineTexture);
            TEXTURE2D_X_FLOAT(_FrontHeightTexture); SAMPLER(sampler_FrontHeightTexture);
            TEXTURE2D_X_FLOAT(_BackHeightTexture); SAMPLER(sampler_BackHeightTexture);



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
                    float2 uv                       : TEXCOORD4;
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
                output.uv = input.texcoord;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 vPos1 = mul(_w2fMatrix,float4(input.positionWS,1)).xyz;
                float height1 = SAMPLE_TEXTURE2D_X(_FrontHeightTexture, sampler_FrontHeightTexture, (vPos1.xy+1)*0.5).x;

                float3 vPos2 = mul(_w2bMatrix,float4(input.positionWS,1)).xyz;
                float height2 = SAMPLE_TEXTURE2D_X(_BackHeightTexture, sampler_BackHeightTexture, (vPos2.xy+1)*0.5).x;
                if(vPos1.z > height1 || vPos2.z-_VoidWidth > height2) return 0;

                float2 pos = input.positionOS.xz;
                //float dist = _HalfVoidWidth - abs(pos.y);
                //float alpha = 1-saturate(dist/_GradientScale);

                float time = _Time.y;
                //float gradientAlpha = saturate(pow(alpha,5) - noise);

                float lineAlpha = SAMPLE_TEXTURE2D(_LineTexture, sampler_LineTexture, pos/_LineProp.xy+_LineProp.zw*time).a;
                float edgeAlpha = input.uv.g > 0 ? input.uv.r : 0;

                float alpha = lerp(0.25,_Color.a,lineAlpha);

                return half4(_Color.rgb, max(alpha,edgeAlpha));
            }
            ENDHLSL
        }
        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForwardBack"
            }

            ZWrite off
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
            float _GradientScale;
            CBUFFER_END
            float4x4 _w2fMatrix;
            float4x4 _w2bMatrix;
            float _VoidWidth;

            TEXTURE2D(_LineTexture);
            SAMPLER(sampler_LineTexture);
            TEXTURE2D_X_FLOAT(_FrontHeightTexture); SAMPLER(sampler_FrontHeightTexture);
            TEXTURE2D_X_FLOAT(_BackHeightTexture); SAMPLER(sampler_BackHeightTexture);



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
                    float2 uv                       : TEXCOORD4;
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
                output.uv = input.texcoord;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 vPos = mul(_w2fMatrix,float4(input.positionWS,1)).xyz;
                float height = SAMPLE_TEXTURE2D_X(_FrontHeightTexture, sampler_FrontHeightTexture, (vPos.xy+1)*0.5).x;
                if(vPos.z > height) return 0;

                float2 pos = input.positionOS.xz;
                //float dist = _HalfVoidWidth - abs(pos.y);
                //float alpha = 1-saturate(dist/_GradientScale);

                float time = _Time.y;
                //float gradientAlpha = saturate(pow(alpha,5) - noise);

                float lineAlpha = SAMPLE_TEXTURE2D(_LineTexture, sampler_LineTexture, pos/_LineProp.xy+_LineProp.zw*time).a;
                float edgeAlpha = input.uv.g > 0 ? input.uv.r : 0;

                float alpha = lerp(0.25,_Color.a,lineAlpha);

                return half4(_Color.rgb, max(alpha,edgeAlpha));
            }
            ENDHLSL
        }
        pass{
            Tags
            {
                "LightMode" = "VoidDepth"
            }

            ZWrite On
            BlendOp Max
            Cull Off

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            float4x4 _w2fMatrix;
            float4x4 _w2bMatrix;
            float _VoidWidth;

            TEXTURE2D_X_FLOAT(_FrontHeightTexture); SAMPLER(sampler_FrontHeightTexture);
            TEXTURE2D_X_FLOAT(_BackHeightTexture); SAMPLER(sampler_BackHeightTexture);

            struct Attributes
            {
                float4 positionOS     : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float3 positionWS               : TEXCOORD1;
                float4 positionCS               : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                //return float4(input.positionCS.z,0,input.positionCS.z,0);
                float3 vPos1 = mul(_w2fMatrix,float4(input.positionWS,1)).xyz;
                float height1 = SAMPLE_TEXTURE2D_X(_FrontHeightTexture, sampler_FrontHeightTexture, (vPos1.xy+1)*0.5).x;

                float3 vPos2 = mul(_w2bMatrix,float4(input.positionWS,1)).xyz;
                float height2 = SAMPLE_TEXTURE2D_X(_BackHeightTexture, sampler_BackHeightTexture, (vPos2.xy+1)*0.5).x;
                if(vPos1.z > height1) return float4(0,0,input.positionCS.z,0);
                else if (vPos2.z-_VoidWidth < height2) return float4(0,input.positionCS.z,0,0);
                else return float4(input.positionCS.z,0,0,0);
            }
            ENDHLSL
        }
    }
}
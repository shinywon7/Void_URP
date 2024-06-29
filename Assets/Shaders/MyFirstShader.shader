Shader "URPCustomLitShader"
{
    Properties
    {
        _BaseColor("BaseColor", Color) = (1,1,1,1)
        _BaseMap("BaseMap",2D) = "white"{}
        [NoScaleOffset] _NormalMap("Normals", 2D) = "bump"{}
        _BumpScale("Bump Scale",Float) = 1
        [Gamma]_Metallic("Metallic",Range(0,1)) = 0
        _Smoothness("Smoothness",Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True" "ShaderModel"="4.5"}

        Pass //포워드 패스입니다 
        {
            Name "ForwardLit" 
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE 
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT 
            #pragma multi_compile _ _CLUSTERED_RENDERING

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" 

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float4 _NormalMap_ST;
                float4 _NormalMap_TexelSize;
                half _Metallic;
                half _Smoothness;
                float _BumpScale;
            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
            UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DOTS_INSTANCED_PROP(float, _Metallic)
                UNITY_DOTS_INSTANCED_PROP(float, _Smoothness)
                UNITY_DOTS_INSTANCED_PROP(float, _BumpScale)
            UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

            #define _BaseColor              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4 , Metadata_BaseColor)
            #define _Smoothness              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4 , Metadata_Smoothness)
            #define _Metallic             UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4 , Metadata_Metallic)
            #define _BumpScale             UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4 , Metadata_BumpScale)
            #endif

            TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            


            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL; 
                float4 tangentOS    : TANGENT;
                float2 texcoord     : TEXCOORD0;
                float2 staticLightmapUV   : TEXCOORD1;
                float2 dynamicLightmapUV  : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;  
                float3 normalWS     : NORMAL;
                float4 tangentWS    : TANGENT;
            };


            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.texcoord, _BaseMap);
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionWS = vertexInput.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS); 
                OUT.tangentWS = float4(TransformObjectToWorldDir(IN.tangentOS.xyz), IN.tangentOS.w);
                return OUT; 
            }
            void InitializeFragmentNormal(inout Varyings IN){
                float3 mainNormal;
                mainNormal.xy = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, TRANSFORM_TEX(IN.uv, _NormalMap)).wy * 2 -1;
                mainNormal.xy *= _BumpScale;
                mainNormal.z = sqrt(1-saturate(dot(mainNormal.xy,mainNormal.xy)));

                float3 tangentSpaceNormal = mainNormal.xyz;
                float3 binormal = cross(IN.normalWS, IN.tangentWS.xyz) * IN.tangentWS.w;

                IN.normalWS = normalize(
                    tangentSpaceNormal.x * IN.tangentWS +
                    tangentSpaceNormal.y * binormal +
                    tangentSpaceNormal.z * IN.normalWS
                );
                //IN.normalWS = mainNormal.xzy;
                //IN.normalWS = normalize(IN.normalWS);
            }
            half4 frag(Varyings IN) : SV_Target
            {
                //라이트 받아오기 

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                InitializeFragmentNormal(IN);
                float3 normalWS = IN.normalWS;
                
                
                //텍스쳐 
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(IN.uv, _BaseMap)) * _BaseColor;
                //albedo += SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, TRANSFORM_TEX(IN.uv, _HeightMap));
                float3 viewDir = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float3 specularTint = albedo * _Metallic;
                float oneMinusReflectivity = 1 - _Metallic;
                albedo *= oneMinusReflectivity;
                BRDFData brdfData;
                InitializeBRDFData(albedo, _Metallic, specularTint, _Smoothness, albedo.a, brdfData);
                float3 color = 0;
                Light mainLight = GetMainLight(shadowCoord); 
                color = LightingPhysicallyBased(brdfData, mainLight, normalWS, viewDir);
                #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();

                #if USE_CLUSTERED_LIGHTING
                for (uint lightIndex = 0; lightIndex < min(_AdditionalLightsDirectionalCount, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    Light light = GetAdditionalLight(lightIndex, shadowCoord);

                    color += LightingPhysicallyBased(brdfData, light, normalWS, viewDir);
                }
                #endif
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, shadowCoord);

                    color += LightingPhysicallyBased(brdfData, light, normalWS, viewDir);
                LIGHT_LOOP_END
                #endif
                //최종 연산 
                return float4(color,1); 
            }
            ENDHLSL
        }
        
        Pass //그림자 패스입니다.
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}  

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Front

            HLSLPROGRAM

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass //깊이 패스입니다 .
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"} 

            ZWrite On
            ColorMask 0
            Cull Front

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }
}
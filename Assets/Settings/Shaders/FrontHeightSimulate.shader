Shader "Hidden/Void/Blit/FrontHeightSimulate"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off

        Pass
        {

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "HeightSimulateUtils.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            TEXTURE2D_X_FLOAT(_FrontTempHeightTexture);
            SAMPLER(sampler_FrontTempHeightTexture);
            TEXTURE2D_X_FLOAT(_FrontRelativeVelocityTexture);
            SAMPLER(sampler_FrontRelativeVelocityTexture);

            float getHeight(float2 uv){
                return SAMPLE_TEXTURE2D_X(_FrontTempHeightTexture, sampler_FrontTempHeightTexture, uv).x;
            }
            float getVelocity(float2 uv){
                return SAMPLE_TEXTURE2D_X(_FrontTempHeightTexture, sampler_FrontTempHeightTexture, uv).y;
            }
            float3 getRelatveVelocity(float2 uv){
                float2 p = float2(uv.x - uv.y * 0.5, uv.y * Sqrt3) * _VoidSize;
                float wind = pow(Unity_Voronoi_Deterministic_float(p * float2(1,0.3) + _Time.y *float2(0.1,1), _Time.y*3, 0.2),2)*_WindPower;
                return SAMPLE_TEXTURE2D_X(_FrontRelativeVelocityTexture, sampler_FrontRelativeVelocityTexture, uv).xyz + float3(wind*0.1,0,wind);
            }
            #include "HeightSimulate.hlsl"
            
            ENDHLSL
        }
        Pass
        {

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "HeightSimulateUtils.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            TEXTURE2D_X_FLOAT(_BackTempHeightTexture);
            SAMPLER(sampler_BackTempHeightTexture);
            TEXTURE2D_X_FLOAT(_FrontRelativeVelocityTexture);
            SAMPLER(sampler_FrontRelativeVelocityTexture);
            TEXTURE2D(_SpreadMap);        SAMPLER(sampler_SpreadMap);

            float getHeight(float2 uv){
                return SAMPLE_TEXTURE2D_X(_BackTempHeightTexture, sampler_BackTempHeightTexture, uv).x;
            }
            float getVelocity(float2 uv){
                return SAMPLE_TEXTURE2D_X(_BackTempHeightTexture, sampler_BackTempHeightTexture, uv).y;
            }
            float3 getRelatveVelocity(float2 uv){
                return SAMPLE_TEXTURE2D_X(_FrontRelativeVelocityTexture, sampler_FrontRelativeVelocityTexture, uv).xyz;
            }
            #include "HeightSimulate.hlsl"
            
            ENDHLSL
        }
        Pass
        {

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment frag
            TEXTURE2D_X_FLOAT(_FrontHeightTexture);
            SAMPLER(sampler_FrontHeightTexture);
            
            float4 frag (Varyings input) : SV_Target
            {
                //input.texcoord.y=1-input.texcoord.y;
                float4 Color = SAMPLE_TEXTURE2D_X(_FrontHeightTexture, sampler_FrontHeightTexture, input.texcoord);
				return Color;
            }
            ENDHLSL
        }
    }
}

Shader "Hidden/Void/Blit/FinalBlit"
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

            #pragma vertex Vert
            #pragma fragment frag

            float _FogDensity;
            float4 _FogColor;
            TEXTURE2D(_SectionTempTexture); SAMPLER(sampler_SectionTempTexture);
            TEXTURE2D(_FrontSideTexture); SAMPLER(sampler_FrontSideTexture);
            TEXTURE2D(_BackSideTexture); SAMPLER(sampler_BackSideTexture);
            TEXTURE2D(_VoidSideTexture); SAMPLER(sampler_VoidSideTexture);
            TEXTURE2D(_CopyDepthTexture); SAMPLER(sampler_CopyDepthTexture);
            TEXTURE2D(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);
            TEXTURE2D(_BloomDepthTexture); SAMPLER(sampler_BloomDepthTexture);

            float4 calcFogColor(float4 backColor, float depth){
                float viewZ = LinearEyeDepth(depth, _ZBufferParams);
                float nearToFarZ = min(viewZ, 20);
                half fogFactor = exp2(-nearToFarZ * _FogDensity);
                //fogFactor = exp2(-0.5);
                return(lerp(_FogColor,backColor, fogFactor));

            }
            
            half4 frag (Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                //return SAMPLE_TEXTURE2D_X(_SectionTempTexture, sampler_SectionTempTexture, uv);
                float4 sectionColor = SAMPLE_TEXTURE2D_X(_SectionTempTexture, sampler_SectionTempTexture, uv);
                half4 frontColor = SAMPLE_TEXTURE2D_X(_FrontSideTexture, sampler_FrontSideTexture, uv);
                half4 backColor = SAMPLE_TEXTURE2D_X(_BackSideTexture, sampler_BackSideTexture, uv);
                half4 voidColor = SAMPLE_TEXTURE2D_X(_VoidSideTexture, sampler_VoidSideTexture, uv);
                float4 backDepth = SAMPLE_TEXTURE2D_X(_CopyDepthTexture, sampler_CopyDepthTexture, uv);
                float sectionDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
                //return backDepth * 100;
                if(sectionColor.a > 1.5) {
                    float fogDepth = max(backDepth.g, sectionDepth);
                    if(backDepth.g > backDepth.b) return calcFogColor(lerp(sectionColor, voidColor, voidColor.a), fogDepth);
                    else return calcFogColor(sectionColor, fogDepth);
                }
                else {
                    //return lerp(sectionColor, frontColor, frontColor.a);
                    if(backDepth.g <= backDepth.b) return lerp(sectionColor, frontColor, frontColor.a);
                    else return sectionColor;
                }
            }
            
            ENDHLSL
        }
    }
}

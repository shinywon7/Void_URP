#ifndef UNIVERSAL_FORWARD_LIT_PASS_INCLUDED
#define UNIVERSAL_FORWARD_LIT_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes
{
    float4 positionOS     : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float3 positionWS               : TEXCOORD1;
    float4 positionCS               : SV_POSITION;
    float4 screenPos                : TEXCOORD2;

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
    output.screenPos = ComputeScreenPos(output.positionCS);
    return output;
}

half4 DepthOnlyFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.screenPos.xy/input.screenPos.w;
    float stencil = SAMPLE_TEXTURE2D_X(_StencilTempTexture, sampler_StencilTempTexture, uv).w;
    
    float3 vPos = mul(_w2fMatrix,float4(input.positionWS,1)).xyz;
    float height = SAMPLE_TEXTURE2D_X(_FrontHeightTexture, sampler_FrontHeightTexture, (vPos.xy+1)*0.5).x;

    if(stencil>0.5 || vPos.z > height) return float4(0,0,input.positionCS.z,0);
    clip(-1);
    return 0;
}
#endif


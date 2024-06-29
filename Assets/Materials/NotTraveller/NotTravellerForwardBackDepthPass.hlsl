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

    float3 vPos = mul(_w2bMatrix,float4(input.positionWS,1)).xyz;
    float height = SAMPLE_TEXTURE2D_X(_BackHeightTexture, sampler_BackHeightTexture, (vPos.xy+1)*0.5).x;

    if(vPos.z > height) return float4(input.positionCS.z,0,0,0);
    clip(-1);
    return 0;
}
#endif


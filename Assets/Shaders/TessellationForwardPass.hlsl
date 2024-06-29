#ifndef UNIVERSAL_FORWARD_CUSTOMLIT_PASS_INCLUDED
#define UNIVERSAL_FORWARD_CUSTOMLIT_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// GLES2 has limited amount of interpolators
#if defined(_PARALLAXMAP) && !defined(SHADER_API_GLES)
#define REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR
#endif

#if (defined(_NORMALMAP) || (defined(_PARALLAXMAP) && !defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR))) || defined(_DETAIL)
#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
#endif

// keep this file in sync with LitGBufferPass.hlsl

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
    float2 uv                       : TEXCOORD0;

    float3 positionWS               : TEXCOORD1;

    float3 normalWS                 : TEXCOORD2;

    half4 tangentWS                : TEXCOORD3;    // xyz: tangent, w: sign
    
    float3 viewDirWS                : TEXCOORD4;

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    half4 fogFactorAndVertexLight   : TEXCOORD5; // x: fogFactor, yzw: vertex light
#else
    half  fogFactor                 : TEXCOORD5;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD6;
#endif

#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS                : TEXCOORD7;
#endif

    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 8);
#ifdef DYNAMICLIGHTMAP_ON
    float2  dynamicLightmapUV : TEXCOORD9; // Dynamic lightmap UVs
#endif
    float2 barycentricCoordinates : TEXCOORD10;

    float4 positionCS               : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    inputData.positionWS = input.positionWS;
#endif

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
//#if defined(_NORMALMAP) || defined(_DETAIL)
    // float sgn = input.tangentWS.w;      // should be either +1 or -1
    // float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    // half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);

    // #if defined(_NORMALMAP)
    // inputData.tangentToWorld = tangentToWorld;
    // #endif
    // inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
//#else
    inputData.normalWS = input.normalWS;
//#endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#else
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
#endif

#if defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
#endif

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

    #if defined(DEBUG_DISPLAY)
    #if defined(DYNAMICLIGHTMAP_ON)
    inputData.dynamicLightmapUV = input.dynamicLightmapUV;
    #endif
    #if defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.staticLightmapUV;
    #else
    inputData.vertexSH = input.vertexSH;
    #endif
    #endif
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////
float3 GerstnerWave (
    float4 wave, float3 p, inout float3 tangent, inout float3 binormal
) {
    float steepness = wave.z;
    float wavelength = wave.w;
    float k = 2 * PI / wavelength;
    float c = sqrt(9.8 / k);
    float2 d = normalize(wave.xy);
    float f = k * (dot(d, p.xz) - c * _Time.y);
    float a = steepness / k;

    tangent += float3(
        -d.x * d.x * (steepness * sin(f)),
        d.x * (steepness * cos(f)),
        -d.x * d.y * (steepness * sin(f))
    );
    binormal += float3(
        -d.x * d.y * (steepness * sin(f)),
        d.y * (steepness * cos(f)),
        -d.y * d.y * (steepness * sin(f))
    );
    return float3(
        d.x * (a * cos(f)),
        a * sin(f),
        d.y * (a * cos(f))
    );
}
// Used in Standard (Physically Based) shader
Varyings LitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 gridPoint = input.positionOS.xyz;
    float3 tangent = float3(1,0,0);
    float3 binormal = float3(0,0,1);
    float3 p = gridPoint;
    gridPoint.xyz += _Offset.xyz;
    p+= GerstnerWave(_WaveA,gridPoint,tangent,binormal);
    p+= GerstnerWave(_WaveB,gridPoint,tangent,binormal);
    p+= GerstnerWave(_WaveC,gridPoint,tangent,binormal);
    float3 normal = normalize(cross(binormal,tangent));
    input.positionOS = float4(p,0);
    input.tangentOS = float4(tangent,0);
    input.normalOS = normal;
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex lighting and SH evaluation
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);

    half fogFactor = 0;
    #if !defined(_FOG_FRAGMENT)
        fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    #endif

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    // already normalized from normal transform to WS.
    output.normalWS = normalInput.normalWS;
    real sign = input.tangentOS.w * GetOddNegativeScale();
    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);

    output.tangentWS = tangentWS;

#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
    output.viewDirTS = viewDirTS;
#endif

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
#ifdef DYNAMICLIGHTMAP_ON
    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
#else
    output.fogFactor = fogFactor;
#endif

    output.positionWS = vertexInput.positionWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    output.positionCS = vertexInput.positionCS;

    return output;
}



// Used in Standard (Physically Based) shader
half4 LitPassFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if defined(_PARALLAXMAP)
#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS = input.viewDirTS;
#else
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, viewDirWS);
#endif
    ApplyPerPixelDisplacement(viewDirTS, input.uv);
#endif

    return float4(input.normalWS,1);


    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData, input.positionWS);

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, input.uv, _BaseMap);

#ifdef _DBUFFER
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
#endif

    half4 color = UniversalFragmentPBR(inputData, surfaceData);

    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a, _Surface);
    return color;
}

struct TessellationControlPoint
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float2 staticLightmapUV   : TEXCOORD1;
    float2 dynamicLightmapUV  : TEXCOORD2;
};

struct TesselationFactors{
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};

float TesselationEdgeFactor(TessellationControlPoint cp0, TessellationControlPoint cp1){
    float3 p0 = TransformObjectToWorld(cp0.positionOS.xyz).xyz;
    float3 p1 = TransformObjectToWorld(cp1.positionOS.xyz).xyz;
    float edgeLength = distance(p0, p1);
    float3 edgeCenter = (p0+p1) *0.5;
    float viewDistance = distance(edgeCenter,_WorldSpaceCameraPos);
    return clamp(edgeLength * _ScreenParams.y/ (_TessellationEdgeLength*viewDistance),0,10);
}
TesselationFactors PatchConstantFunction(InputPatch<TessellationControlPoint, 3> patch){
    float factor0 = TesselationEdgeFactor(patch[1], patch[2]);
    float factor1 = TesselationEdgeFactor(patch[0], patch[2]);
    float factor2 = TesselationEdgeFactor(patch[0], patch[1]);
    TesselationFactors f;
    f.edge[0] = factor0;
    f.edge[1] = factor1;
    f.edge[2] = factor2;
    f.inside = (factor0+factor1+factor2) *(1/3.0);
    return f;
}
TessellationControlPoint TessellationPassVertex (Attributes v){
    TessellationControlPoint p;
    p.positionOS = v.positionOS;
    p.normalOS = v.normalOS;
    p.tangentOS = v.tangentOS;
    p.texcoord = v.texcoord;
    p.staticLightmapUV = v.staticLightmapUV;
    p.dynamicLightmapUV = v.dynamicLightmapUV;
    return p;
}

[domain("tri")]
[outputcontrolpoints(3)]
[outputtopology("triangle_cw")]
[partitioning("fractional_odd")]
[patchconstantfunc("PatchConstantFunction")]
TessellationControlPoint LitPassHull(InputPatch<TessellationControlPoint, 3> patch, uint id : SV_OutputControlPointID){
    return patch[id];
}

[domain("tri")]
Varyings LitPassDomain(TesselationFactors factors, OutputPatch<TessellationControlPoint, 3> patch, float3 barycentricCoordinates : SV_DomainLocation){
    Attributes data;
    #define DOMAIN_INTERPOLATE(fieldName) data.fieldName = patch[0].fieldName * barycentricCoordinates.x + patch[1].fieldName * barycentricCoordinates.y +patch[2].fieldName * barycentricCoordinates.z;
    DOMAIN_INTERPOLATE(positionOS);
    DOMAIN_INTERPOLATE(normalOS);
    DOMAIN_INTERPOLATE(tangentOS);
    DOMAIN_INTERPOLATE(texcoord);
    DOMAIN_INTERPOLATE(staticLightmapUV);
    DOMAIN_INTERPOLATE(dynamicLightmapUV);

    return LitPassVertex(data);
}


// [maxvertexcount(3)]
// void LitPassGeometry(triangle Varyings i[3], inout TriangleStream<Varyings> stream){
//     float3 p0 = i[0].positionWS.xyz;
//     float3 p1 = i[1].positionWS.xyz;
//     float3 p2 = i[2].positionWS.xyz;
//     float3 normal = normalize(cross(p1-p0,p2-p0));
//     //i[0].normalWS = normal;
//     //i[1].normalWS = normal;
//     //i[2].normalWS = normal;

//     i[0].barycentricCoordinates = float2(1,0);
//     i[1].barycentricCoordinates = float2(0,1);
//     i[2].barycentricCoordinates = float2(0,0);

//     stream.Append(i[0]);
//     stream.Append(i[1]);
//     stream.Append(i[2]);
// }
#endif

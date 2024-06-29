#ifndef UNIVERSAL_FORWARD_LIT_PASS_INCLUDED
#define UNIVERSAL_FORWARD_LIT_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

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
    uint vertexIndex : SV_VertexID;
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
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    half4 tangentWS                : TEXCOORD3;    // xyz: tangent, w: sign
#endif

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

    float4 positionCS               : SV_POSITION;
    float4 screenPos                : TEXCOORD10;
    float2 barys                    : TEXCOORD11;
    float3 normal0                  : TEXCOORD12;
    float3 normal1                  : TEXCOORD13;
    float3 normal2                  : TEXCOORD14;
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
#if defined(_NORMALMAP) || defined(_DETAIL)
    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);

    #if defined(_NORMALMAP)
    inputData.tangentToWorld = tangentToWorld;
    #endif
    inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
#else
    inputData.normalWS = input.normalWS;
#endif

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

// Used in Standard (Physically Based) shader
Varyings LitPassVertex(Attributes input)
{
    
    uint vertexIndex = input.vertexIndex;
    Varyings output = (Varyings)0;
    if(vertexIndex < _Resolution*_Resolution){
        output.uv = float2(vertexIndex/_Resolution, vertexIndex%_Resolution);
        float4 height = LOAD_TEXTURE2D_X_LOD(_FrontHeightTexture, int2(output.uv),0);
        output.uv /= _Resolution;
        input.positionOS.y += height.x;
        input.normalOS = normalize(float3(height.z,1,height.w));
    }
    //input.normalOS = float3(0,1,0);

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

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

    //output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    // already normalized from normal transform to WS.
    output.normalWS = normalInput.normalWS;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    real sign = input.tangentOS.w * GetOddNegativeScale();
    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    output.tangentWS = tangentWS;
#endif

#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
    output.viewDirTS = viewDirTS;
#endif

    output.positionWS = vertexInput.positionWS;

    output.positionCS = vertexInput.positionCS;
    output.screenPos = ComputeScreenPos(output.positionCS);

    return output;
}
static const float Sqrt3 = 1.73205080757;
static const float Height = 1.4142135624;

[maxvertexcount(3)]
void LitPassGeometry (
	triangle Varyings i[3],
	inout TriangleStream<Varyings> stream
) {
    Varyings g0,g1,g2;
    g0 = i[0];
	g1 = i[1];
	g2 = i[2];
    g0.normal0 = g0.normalWS;
    g1.normal0 = g0.normalWS;
    g2.normal0 = g0.normalWS;

    g0.normal1 = g1.normalWS;
    g1.normal1 = g1.normalWS;
    g2.normal1 = g1.normalWS;

    g0.normal2 = g2.normalWS;
    g1.normal2 = g2.normalWS;
    g2.normal2 = g2.normalWS;

    g0.barys = float2(1, 0);
	g1.barys = float2(0, 1);
	g2.barys = float2(0, 0);

	stream.Append(g0);
	stream.Append(g1);
	stream.Append(g2);
}

float3 tanRefract(float3 i, float3 n, float eta){
    float3 project = dot(i,n)*n;
    return normalize(project*eta +(i-project));
}
float2 getCS2UV(float4 i){
    i.y = -i.y;
    return ((i.xy/i.w)+1)/2;
}
half4 CalcBackgroundColor(Varyings input, float3 viewDirWS, float facing, float refractionIndex, float2 uv){
    float3 surfaceWS = input.positionWS;
    float3 surfaceVS = TransformWorldToView(surfaceWS);
    float3 refractedDir = tanRefract(viewDirWS, input.normalWS, refractionIndex);
    float s = 0, e = 20;
    float2 targetUV = 0;
    float3 targetVS;
    for(int j = 0; j <10;j++){
        float m = (s+e)/2;
        float3 target = surfaceWS+refractedDir*m;
        float4 positionCS = TransformWorldToHClip(target);
        targetUV = getCS2UV(positionCS);
        targetVS = TransformWorldToView(target);
        //float4 localTarget = mul(WorldToVCamLocalMatrix,float4(target,1));
        float3 depth = SAMPLE_TEXTURE2D_X(_BloomDepthTexture, sampler_BloomDepthTexture, targetUV);
        float targetDepth = LinearEyeDepth(facing>0?max(depth.r,depth.g):max(depth.r,depth.b), _ZBufferParams);
        //float targetDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv));
        if(targetDepth < -targetVS.z || max(targetUV.x, targetUV.y) >1 ||min(targetUV.x, targetUV.y) <0) e = m;
        else s = m;
    }
    float depth = LinearEyeDepth(SAMPLE_TEXTURE2D_X(_CopyDepthTexture, sampler_CopyDepthTexture, targetUV).g, _ZBufferParams) + surfaceVS.z;
    float fogFactor = exp2(-_FogDensity * min(depth,_VoidWidth));
    
    if(facing > 0){
        half4 color1 = SAMPLE_TEXTURE2D_X(_BackSideTexture, sampler_BackSideTexture, targetUV);
        half4 color2 = SAMPLE_TEXTURE2D_X(_VoidSideTexture, sampler_VoidSideTexture, targetUV);
        half4 blendColor = color2 + color1 * saturate(0.5-color2.a);
        return lerp(_FogColor,blendColor,fogFactor);
    }
    else return max(SAMPLE_TEXTURE2D_X(_FrontSideTexture, sampler_FrontSideTexture, targetUV),SAMPLE_TEXTURE2D_X(_BackSideTexture, sampler_BackSideTexture, targetUV));
}

Light GetVoidLight(float facing)
{
    Light light;
    light.direction = _VoidNormal;
    if(facing > 0) {
        light.color = _FrontLightColor;
    }
    else {
        light.color = _BackLightColor;
    }
    light.distanceAttenuation = 1.0;
    light.shadowAttenuation = 1.0;
    light.layerMask = 0;
    return light;
}

half4 VoidFragmentPBR(InputData inputData, SurfaceData surfaceData, float facing)
{
    #if defined(_SPECULARHIGHLIGHTS_OFF)
    bool specularHighlightsOff = true;
    #else
    bool specularHighlightsOff = false;
    #endif
    BRDFData brdfData;

    // NOTE: can modify "surfaceData"...
    InitializeBRDFData(surfaceData, brdfData);

    // Clear-coat calculation...
    BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    uint meshRenderingLayers = GetMeshRenderingLayer();
    Light mainLight = GetVoidLight(facing);

    // NOTE: We don't apply AO to the GI here because it's done in the lighting calculation below...
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    LightingData lightingData = CreateLightingData(inputData, surfaceData);

    lightingData.giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
                                              inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
                                              inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);

    lightingData.mainLightColor = LightingPhysicallyBased(brdfData, brdfDataClearCoat,
                                                            mainLight,
                                                            inputData.normalWS, inputData.viewDirectionWS,
                                                            surfaceData.clearCoatMask, specularHighlightsOff);

    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
    lightingData.vertexLightingColor += inputData.vertexLighting * brdfData.diffuse;
    #endif

#if REAL_IS_HALF
    // Clamp any half.inf+ to HALF_MAX
    return min(CalculateFinalColor(lightingData, surfaceData.alpha), HALF_MAX);
#else
    return CalculateFinalColor(lightingData, surfaceData.alpha);
#endif
}

// Used in Standard (Physically Based) shader
void LitPassFragment(
    Varyings input
    , float facing : VFACE
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // float4 height = SAMPLE_TEXTURE2D_X(_FrontHeightTexture, sampler_FrontHeightTexture, input.uv);
    // float3 normalOS = normalize(float3(height.z,1,height.w));
    // input.normalWS = TransformObjectToWorldNormal(normalOS);
    float3 barys;
    barys.xy = input.barys;
    barys.z = 1 - barys.x - barys.y;
    // barys = pow(barys, 1.25);
    // float3 barys = float3(1/distance(input.barys, float2(2, 0)),
    //                     1/distance(input.barys, float2(-1, Sqrt3)),
    //                     1/distance(input.barys, float2(-1, -Sqrt3)));
    //barys = float3(barys.y*barys.z,barys.x*barys.z,barys.x*barys.y);
    barys = pow(barys, _PowRate);
    input.normalWS = normalize((input.normal0 * barys.x + input.normal1 * barys.y + input.normal2 * barys.z)/(barys.x+barys.y+barys.z));

    half3 viewDirWS = -GetWorldSpaceNormalizeViewDir(input.positionWS);
    SurfaceData surfaceData;
    
    float2 uv = input.screenPos.xy/input.screenPos.w;
    float stencil = SAMPLE_TEXTURE2D_X(_StencilTempTexture, sampler_StencilTempTexture, uv).w;
    InitializeStandardLitSurfaceData(input.uv, surfaceData, stencil);

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, input.uv, _BaseMap);

#ifdef _DBUFFER
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
#endif


    float borderWidth = 0.5;
    float edgeOffset = min(min(uv.x,uv.y),min(1-uv.x, 1-uv.y));
    float refractionIndex = _RefractionIndex;
    if(edgeOffset < borderWidth) refractionIndex = lerp(1,_RefractionIndex,smoothstep(0,borderWidth,edgeOffset));
    _BaseColor.a = lerp(_BaseColor.a,0,stencil);
    surfaceData.emission =  CalcBackgroundColor(input, viewDirWS, facing, refractionIndex, uv) * (1-_BaseColor.a);

    half4 color = VoidFragmentPBR(inputData, surfaceData,facing);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));

    outColor.rgb = color;
    outColor.a = facing>0 ?1:2;
    //outColor.rgb = input.normalWS;


    //outColor.rgb = float3(0,input.uv);
#ifdef _WRITE_RENDERING_LAYERS
    uint renderingLayers = GetMeshRenderingLayer();
    outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
#endif
}

#endif





#ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float , _MaxOffset)
    UNITY_DOTS_INSTANCED_PROP(float , _Damping)
    UNITY_DOTS_INSTANCED_PROP(float , _Precalculation)
    UNITY_DOTS_INSTANCED_PROP(float , _SpeedTweak)
    UNITY_DOTS_INSTANCED_PROP(float , _SampleSize)
    UNITY_DOTS_INSTANCED_PROP(float , _Resolution)
    UNITY_DOTS_INSTANCED_PROP(int   , _Substeps)
    UNITY_DOTS_INSTANCED_PROP(float , _VerticalPushScale)
    UNITY_DOTS_INSTANCED_PROP(float , _HorizontalPushScale)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _MaxOffset              UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _MaxOffset)
#define _Damping                UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Damping)
#define _Precalculation         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Precalculation)
#define _SpeedTweak             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _SpeedTweak)
#define _SampleSize             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _SampleSize)
#define _Resolution             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Resolution)
#define _Substeps               UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(int    , _Substeps)
#define _VerticalPushScale      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _VerticalPushScale)
#define _HorizontalPushScale    UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _HorizontalPushScale)
#endif

float getFixed(float2 uv){
    return max(SAMPLE_TEXTURE2D_X(_FrontRelativeVelocityTexture, sampler_FrontRelativeVelocityTexture, uv+float2(0,0)).w,
                SAMPLE_TEXTURE2D_X(_FrontRelativeVelocityTexture, sampler_FrontRelativeVelocityTexture, uv+float2(-0,0)).w);
}

float CalculateAcceleration(float2 uv, float height, float2 dx, float2 dy){
    float averageNeighborHeight =   (getHeight(uv+dx) + getHeight(uv-dx) + 
                                    getHeight(uv-dy-dx) + getHeight(uv-dy) +
                                    getHeight(uv+dy) + getHeight(uv+dy+dx)) /9;
    return averageNeighborHeight - height;
}
float ModifyHeightFromVelocity(float2 uv, float3 offset, float deltaTime){
    float3 velocity = getRelatveVelocity(uv);
    float len = length(velocity);
    if(len <0.000001) return 0;
    float magnitude = len * _HorizontalPushScale * deltaTime;
    velocity.y = 0;
    return dot(normalize(velocity), -offset) * magnitude;
}

float4 frag (Varyings input) : SV_Target
{
    float2 uv = input.texcoord;

    //uv.y= 1-uv.y;
    float texelSize = 1/_Resolution;
    float2 dx = float2(texelSize,0);
    float2 dy = float2(0,texelSize);
    float deltaTime = min(unity_DeltaTime.x,0.1);
    float height = getHeight(uv);
    float velocity = getVelocity(uv);
    if(uv.x < texelSize || 1-uv.x < texelSize || uv.y < texelSize || 1-uv.y < texelSize){
        return 0;
    }
    float accelerations = CalculateAcceleration(uv, height,dx,dy);
    float heightCorrection = 0;
    if(accelerations > _MaxOffset){
        heightCorrection += accelerations - _MaxOffset;
    }
    else if(accelerations < -_MaxOffset){
        heightCorrection += accelerations + _MaxOffset;
    }
    accelerations -= heightCorrection;
    velocity += deltaTime * (_Precalculation * accelerations - velocity * _Damping);
    float heightOffset = deltaTime * velocity + heightCorrection;
    height += heightOffset * _SpeedTweak;

    float3 relativeVelocity = getRelatveVelocity(uv);
    float3 change = relativeVelocity.y * _VerticalPushScale *deltaTime;
    
    height += change;

    height += ModifyHeightFromVelocity(uv+dx,float3(1,0,0), deltaTime);
    height += ModifyHeightFromVelocity(uv-dx,float3(-1,0,0), deltaTime);
    height += ModifyHeightFromVelocity(uv+dy,float3(-0.5,0,Sqrt3/2), deltaTime);
    height += ModifyHeightFromVelocity(uv+dy+dx,float3(0.5,0,Sqrt3/2), deltaTime);
    height += ModifyHeightFromVelocity(uv-dy,float3(0.5,0,-Sqrt3/2), deltaTime);
    height += ModifyHeightFromVelocity(uv-dy-dx,float3(-0.5,0,-Sqrt3/2), deltaTime);
    height = clamp(height,-1.5,1.5);
    float normalization = 1 / _SampleSize;
    float3 gradient =  (getHeight(uv+dx) - getHeight(uv-dx), 
                        getHeight(uv+dy+dx) - getHeight(uv-dy-dx),
                        getHeight(uv+dy) - getHeight(uv-dy)) * normalization * 2;
    float3 normal =  normalize(cross(float3(1,gradient.y,Sqrt3), float3(2,gradient.x,0)) +
                        cross(float3(-1,gradient.z,Sqrt3), float3(1,gradient.y,Sqrt3)) +
                        cross(-float3(2,gradient.x,0), float3(-1,gradient.z,Sqrt3)));
    // normal =  normalize(normalize(cross(float3(1,gradient.y,Sqrt3), float3(2,gradient.x,0))) +
    //                     normalize(cross(float3(-1,gradient.z,Sqrt3), float3(1,gradient.y,Sqrt3))) +
    //                     normalize(cross(-float3(2,gradient.x,0), float3(-1,gradient.z,Sqrt3))));
    //normal =  normalize(cross(float3(0,gradient.y + gradient.z,2), float3(2,gradient.x,0)));
    normal = normal/normal.y;
    //return velocity
    //return float4(0,0,normal.xz);
    if(getFixed(uv) > 0.5) return float4(0,0,0,0);
    //return float4(0,0,0,0);
    if(height < -_HalfVoidWidth) height = -_HalfVoidWidth+0.01;
    return float4(height,velocity,normal.xz);
}
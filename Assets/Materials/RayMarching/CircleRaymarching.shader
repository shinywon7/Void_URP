Shader "Unlit/CircleRaymarching"
{
    Properties
    {
        [HDR] _Color("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalRenderPipeline" }

        pass
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
            CBUFFER_END
            float3 Top;
            float3 Under;
            float D;
            float H;
            float R;
            float3 V1, V2 ,V3;
            float3 W1, W2 ,W3;
            float3 N1, N2 ,N3;
            float Grad;
            float Grad2;
            float4x4 w2lMatrix;

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

                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);

                return output;
            }
            static const float maxDst = 80;
            static const float epsilon = 0.001;

            float hash(float n){return frac(sin(n)*753.5453123);}
            float noise(in float3 x){
                float3 p = floor(x);
                float3 f = frac(x);
                f = f*f*(3.0-2.0*f);

                float n = p.x + p.y*157.0 + 113.0*p.z;
                return lerp(lerp(lerp( hash(n+  0.0), hash(n+  1.0),f.x),
                        lerp( hash(n+157.0), hash(n+158.0),f.x),f.y),
                    lerp(lerp( hash(n+113.0), hash(n+114.0),f.x),
                        lerp( hash(n+270.0), hash(n+271.0),f.x),f.y),f.z);
            }

            float SphereDistance(float3 eye, float3 center, float radius) {
                return distance(eye, center) - radius;
            }


            float CylinderDistance(float3 eye, float radius) {
                float3 p = mul(w2lMatrix, float4(eye,1)).xyz;
                if(p.z > H) return length(float3(0,0,H)-p);
                float angle = atan2(p.x,p.y);
                angle = (PI+angle)%(PI*2/3) - PI/3;

                float power = max(-0.5,(H-p.z)/H);
                radius = lerp(0.3, (0.5/(cos(angle)) - cos(angle)*0.1), power) * radius * power ;
                //radius = (0.5/(cos(angle))) * radius;
                
                float dist = length(p.xy);
                dist = dist - radius;
                return max(dist, -p.z);
            }

            float Width(float dist, float d){
                return abs(dist)-d;
            }
            float3 GetDist(float3 disp, float3 W, float d1,float d2){
                float x = (d2 -d1);
                float y = dot(disp, W);
                float radius = Grad * y;
                return float3(x,y, radius - d1);
            }
            float3 CastDistance(float3 eye){
                float3 disp = eye - Top;
                if(disp.z > 0) return (0,0,length(disp));
                float dist = 1000;
                if(dot(disp, N1) > 0) dist = min(dist, length(cross(disp, V1))); 
                if(dot(disp, N2) > 0) dist = min(dist, length(cross(disp, V2))); 
                if(dot(disp, N3) > 0) dist = min(dist, length(cross(disp, V3))); 
                if(dist < 999) return (0,0,dist-0.02);
                float3 d;
                d.x = length(cross(disp, W1));
                d.y = length(cross(disp, W2));
                d.z = length(cross(disp, W3));

                float3 W = W1;
                float3 TW = W2;
                if(d.y > d.z) {d.yz = d.zy; TW = W3;}
                if(d.x > d.y) {d.xy = d.yx; W = TW;}
                if(d.y > d.z) d.yz = d.zy;
                return GetDist(disp,W,d.x,d.y)-0.02;

                // if(d2 > d3) d2 = d3;
                // if(d1 > d2) return GetDist(disp,d2,d1);
                // else return GetDist(disp,d1,d2);
            }

            float CubeDistance(float3 eye, float3 center, float3 size, float radius) {
                float3 o = abs(eye-center) -size;
                float ud = length(max(o,0)) - radius;
                float n = max(max(min(o.x,0),min(o.y,0)), min(o.z,0));
                return ud+n;
            }

            float Blend( float a, float b, float k )
            {
                float h = clamp( 0.5+0.5*(b-a)/k, 0.0, 1.0 );
                float blendDst = lerp( b, a, h ) - k*h*(1.0-h);
                return blendDst;
            }

            float FresnelEffect(float3 Normal, float3 ViewDir, float Power)
            {
                return pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
            }

            float SceneInfo1(float3 p) {
                float globalDst = maxDst;

                //globalDst = min(globalDst, CylinderDistance(eye,D));
                float3 info = CastDistance(p);
                globalDst = min(globalDst, info.z);
                globalDst = max(globalDst, -SphereDistance(p, Under, R));
                float cubeDst = CubeDistance(p, float3(0,0,2), .3,0.05) + noise(p * 3.0 + _Time.y*1.5)*0.03;
                //globalDst = cubeDst;
                
                globalDst = Blend(globalDst, cubeDst, 0.2);

                // globalDst = min(globalDst, PlaneDistance(eye, Top, N1));
                // globalDst = min(globalDst, PlaneDistance(eye, Top, N2));
                // globalDst = min(globalDst, PlaneDistance(eye, Top, N3));

                return globalDst;
            }
            float SceneInfo2(float3 p) {
                float globalDst = maxDst;

                //globalDst = min(globalDst, CylinderDistance(eye,D));
                float3 info = CastDistance(p);
                globalDst = min(globalDst, info.z);
                globalDst = max(globalDst, -SphereDistance(p, Under, R));
                float cubeDst = CubeDistance(p, float3(0,0,2), 0.2,0.05) + noise(p * 5.0 + _Time.y*1.5)*0.01;
                globalDst = cubeDst;
                return globalDst;
            }
            float SceneInfo3(float3 p) {
                float globalDst = maxDst;

                //globalDst = min(globalDst, CylinderDistance(eye,D));
                float3 info = CastDistance(p);
                globalDst = min(globalDst, info.z);
                globalDst = max(globalDst, -SphereDistance(p, Under, R));
                float cubeDst = CubeDistance(p, float3(0,0,2), 0.0,0.25) + noise(p * 5.0 + _Time.y*1.5)*0.01;
                globalDst = cubeDst;
                return globalDst;
            }

            float3 CalcNormal1(float3 p) {
                float x = SceneInfo1(float3(p.x+epsilon,p.y,p.z)) - SceneInfo1(float3(p.x-epsilon,p.y,p.z));
                float y = SceneInfo1(float3(p.x,p.y+epsilon,p.z)) - SceneInfo1(float3(p.x,p.y-epsilon,p.z));
                float z = SceneInfo1(float3(p.x,p.y,p.z+epsilon)) - SceneInfo1(float3(p.x,p.y,p.z-epsilon));
                return normalize(float3(x,y,z));
            }
            float3 CalcNormal2(float3 p) {
                float x = SceneInfo2(float3(p.x+epsilon,p.y,p.z)) - SceneInfo2(float3(p.x-epsilon,p.y,p.z));
                float y = SceneInfo2(float3(p.x,p.y+epsilon,p.z)) - SceneInfo2(float3(p.x,p.y-epsilon,p.z));
                float z = SceneInfo2(float3(p.x,p.y,p.z+epsilon)) - SceneInfo2(float3(p.x,p.y,p.z-epsilon));
                return normalize(float3(x,y,z));
            }
            float3 CalcNormal3(float3 p) {
                float x = SceneInfo3(float3(p.x+epsilon,p.y,p.z)) - SceneInfo3(float3(p.x-epsilon,p.y,p.z));
                float y = SceneInfo3(float3(p.x,p.y+epsilon,p.z)) - SceneInfo3(float3(p.x,p.y-epsilon,p.z));
                float z = SceneInfo3(float3(p.x,p.y,p.z+epsilon)) - SceneInfo3(float3(p.x,p.y,p.z-epsilon));
                return normalize(float3(x,y,z));
            }

            void RenderColor2(float3 p, float3 dir, inout float4 color){
                //vec3 lightDir = normalize(vec3(1.0,0.4,0.0));
                float3 normal = CalcNormal2(p);
                float3 normal_distorted = CalcNormal2(p +  dir*noise(p*3 + _Time.y*2.0)*0.02);

                float ndotl = abs(dot( -dir, normal ));
                float ndotl_distorted = (dot( -dir, normal_distorted ));
                float rim = pow(1.0-ndotl, 3.0);
                float rim_distorted = pow(1.0-ndotl_distorted, 6.0);
                
                color = lerp( color, float4(normal*0.5+0.5,1), rim_distorted+0.1 );
                color = lerp( float4(0.2,0.2,1,1), color, rim_distorted+0.1 );
                color.rgb += rim*0.3;

                //color = mix( color, normal*0.5+vec3(0.5), rim_distorted+0.15 );
                //color = mix( vec3(0.0,0.1,0.6), color, rim*1.5 );
                //color = mix( refract(normal, dir, 0.5)*0.5+float3(0.5), color, rim );
                //color = mix( vec3(0.1), color, rim );
            }

            void RayMarch2(float3 p, float3 dir, inout float4 color){
                float rayDst = 0;
                float3 origin = p;
                float3 direction = dir;
                while (rayDst < maxDst) {
                    float dst = SceneInfo2(origin);
                    origin += direction * dst;
        
                    if (dst <= epsilon) {
                        float3 normal = CalcNormal2(origin);
                        
                        //return float4(sceneInfo.xy,0,1);
                        RenderColor2(origin, direction, color);
                        break;
                    }
                    rayDst += dst;
                }
            }
            void RenderColor1(float3 p, float3 dir, inout float4 color){
                //vec3 lightDir = normalize(vec3(1.0,0.4,0.0));
                float3 normal = CalcNormal1(p);
                float3 normal_distorted = CalcNormal1(p +  dir*noise(p*0.75 + _Time.y*2.0)*0.03);

                float ndotl = abs(dot( -dir, normal ));
                float ndotl_distorted = (dot( -dir, normal_distorted ));
                float rim = pow(1.0-ndotl, 6.0);
                float rim_distorted = pow(1.0-ndotl_distorted, 6.0);
                
                color = lerp( color, float4(normal*0.5+0.5,1), rim_distorted+0.1 );
                color += rim;
                RayMarch2(p, refract(dir, normal, 1.05), color);
                color = clamp(color,0,3);
                //outColor += rim;

                //color = mix( color, normal*0.5+vec3(0.5), rim_distorted+0.15 );
                //color = mix( vec3(0.0,0.1,0.6), color, rim*1.5 );
                //color = mix( refract(normal, dir, 0.5)*0.5+float3(0.5), color, rim );
                //color = mix( vec3(0.1), color, rim );
            }
            

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float rayDst = 0;
                float3 origin = mul(w2lMatrix, float4(_WorldSpaceCameraPos,1)).xyz;
                float3 direction = mul(w2lMatrix, float4(normalize(input.positionWS - _WorldSpaceCameraPos),0)).xyz;
                float col = 1;
                while (rayDst < maxDst) {
                    float dst = SceneInfo1(origin);
                    origin += direction * dst;
                    if (dst <= epsilon) {
                        float3 normal = CalcNormal1(origin);
                        
                        //return float4(sceneInfo.xy,0,1);
                        float4 color = _Color;
                        RenderColor1(origin, direction, color);
                        return color;
                        break;
                    }
                    rayDst += dst;
                }

                clip(-1);
                return 0;
            }
            ENDHLSL
        }
    }
}

Shader "Unlit/Texture"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _VoidPass;
            float4 _VoidPass_ST;
            sampler2D _CameraDepthTexture;
            float4 _CameraDepthTexture_ST;


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _VoidPass);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float backgroundDepth =
		            LinearEyeDepth(tex2D(_VoidPass, i.uv));
                //float surfaceDepth = UNITY_Z_0_FAR_FROM_CLIPSPACE(screenPos.z);
                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);
                return saturate(backgroundDepth);
            }
            ENDCG
        }
    }
}

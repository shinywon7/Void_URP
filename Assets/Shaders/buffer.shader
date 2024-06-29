Shader "Hidden/PP_DepthBuffer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_DepthDistance("DepthDistance", float) = 25
    }
    SubShader
    {
        Cull Off 
		ZWrite Off 
		ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
				float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            //Texture2DMS _MainTex;
            
            sampler2D _MainTex;
			float4 _MainTex_TexelSize;

			sampler2D_float _CameraDepthTexture;	//! 카메라로부터 뎁스텍스처를 받아옴
			half _DepthDistance;
            
            fixed4 frag (v2f i) : SV_Target
            {
                float4 fMainTex = tex2D(_MainTex, i.uv);
				return 1;
            }
            ENDCG
        }
    }
}
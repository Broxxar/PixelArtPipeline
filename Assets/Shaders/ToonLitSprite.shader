Shader "Unlit/ToonLitSprite"
{
	Properties
	{
		_MainTex ("Diffuse Map", 2D) = "white" {}
        [NoScaleOffset] _NormalTex ("Normal Map", 2D) = "bump" {}
	}
	SubShader
	{
        Cull Off
    
		Pass
		{
            Tags{ "LightMode" = "ForwardBase" }
            
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
            #pragma target 3.0
			
			#include "UnityCG.cginc"
            #include "Lighting.cginc"
            
			struct appdata
			{
				float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
                float3 wNormal : TEXCOORD1;
				float3 wTangent : TEXCOORD2;
				float3 wBitangent : TEXCOORD3;
			};

			sampler2D _MainTex;
			sampler2D _NormalTex;
            
			v2f vert (appdata v)
			{
				v2f o;
                
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.wNormal = UnityObjectToWorldNormal(v.normal);
				o.wTangent = UnityObjectToWorldNormal(v.tangent);
				o.wBitangent = cross(-o.wTangent, o.wNormal) * v.tangent.w;
                
				return o;
			}
			
			fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
			{
				fixed4 diffuseTex = tex2D(_MainTex, i.uv);
                clip(diffuseTex.a > 0 ? 1 : -1);
                
                float3 normalTex = normalize(tex2D(_NormalTex, i.uv) * 2 - 1);
                normalTex.z *= facing;
				float3 N = normalize(i.wTangent) * normalTex.r + normalize(i.wBitangent) * normalTex.g + normalize(i.wNormal) * normalTex.b;
                
                half3 toonLight = saturate(dot(N, _WorldSpaceLightPos0)) > 0.3 ? _LightColor0 : unity_AmbientSky;
                half3 diffuse = diffuseTex * (toonLight);
                
                return half4(diffuse, 0);
			}
			ENDCG
		}
	}
}

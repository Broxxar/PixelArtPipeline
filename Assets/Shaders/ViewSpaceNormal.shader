Shader "Hidden/ViewSpaceNormal"
{
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
                float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.normal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
				return o;
			}
            
			fixed4 frag (v2f i) : SV_Target
			{
				return half4(i.normal * 0.5 + 0.5, 1.0f);
			}
			ENDCG
		}
	}
}

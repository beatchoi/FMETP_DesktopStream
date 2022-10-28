Shader "Hidden/FMCubemapToEquirect"
{
	Properties
	{
		[HideInInspector]_MainTex ("Texture", CUBE) = "white" {}
        _Brightness ("Brightness", float) = 1
	}
	SubShader
	{
		// No culling or depth
        Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			uniform float FORWARD;
			samplerCUBE _MainTex;
            float _Brightness;

			v2f vert (float4 v : POSITION,float2 uv : TEXCOORD0)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v);
				o.uv =(uv - 0.5) * float2(3.141592 * 2, 1.570796 * 2) + float2(FORWARD, 0);
				return o;
			}
			fixed4 frag (v2f i) : SV_Target
			{
				float cy = cos(i.uv.y);
				float4 col = texCUBE(_MainTex, float3(sin(i.uv.x)*cy, sin(i.uv.y), cos(i.uv.x)*cy));
				col.rgb *= _Brightness;
				return col;
			}
			ENDCG
		}
	}
}

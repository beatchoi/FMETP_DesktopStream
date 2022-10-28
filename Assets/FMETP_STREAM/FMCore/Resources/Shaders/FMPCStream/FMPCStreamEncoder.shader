Shader "Hidden/FMPCStreamEncoder"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
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

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _CameraDepthNormalsTexture;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float mod(float x, float y){ return x - y * floor(x/y); }
            float RGBToFloat(float3 rgb, float scale)
            {
                return rgb.r + (rgb.g/scale)+ (rgb.b/(scale*scale));
            }
            float3 FloatToRGB(float v, float scale)
            {
                float r = v;
                float g = mod(v*scale,1.0);
                r-= g/scale;
                float b = mod(v*scale*scale,1.0);
                g-=b/scale;
                return float3(r,g,b);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = fixed4(0,0,0,1);
                float4 NormalDepth;

                float2 uvDepth = i.uv;
                float2 uvColor = i.uv;

                if(i.uv.x < 0.5){
                    uvDepth.x *= 2.0;
                    DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uvDepth), NormalDepth.w, NormalDepth.xyz);
                    col.rgb = FloatToRGB(NormalDepth.w, 1.0);

                    if(col.r > 0.96) col.rgb = float3(0,0,1);
                }
                else{
                    uvColor.x -= 0.5;
                    uvColor.x *= 2.0;
                    col = tex2D(_MainTex, uvColor);
                    col.rgb /= 2;
                }
                return col;
            }
            ENDCG
        }
    }
}
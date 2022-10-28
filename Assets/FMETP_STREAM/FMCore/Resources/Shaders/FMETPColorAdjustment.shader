Shader "Hidden/FMETPColorAdjustment"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        _WebcamTex ("Webcam", 2D) = "white" {}
        _FlipX("FlipX", float) = 0
		_FlipY("FlipY", float) = 0
        _ScaleX ("ScaleX", float) = 1
        _ScaleY ("ScaleY", float) = 1
        _OffsetX ("OffsetX", float) = 0
        _OffsetY ("OffsetY", float) = 0

        _Brightness ("Brightness", float) = 1
        _DeNoise ("DeNoise", float) = 0
        _DeNoise ("Sharpen", float) = 0
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

                UNITY_VERTEX_INPUT_INSTANCE_ID //for single pass vr
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;

                UNITY_VERTEX_OUTPUT_STEREO //for single pass vr
            };

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex); //for single pass vr
            sampler2D _WebcamTex;
            float _FlipX;
			float _FlipY;
            float _ScaleX;
            float _ScaleY;
            float _OffsetX;
            float _OffsetY;

            float _Brightness;
            float _DeNoise;
            float _Sharpen;

            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v); //for single pass vr
                UNITY_INITIALIZE_OUTPUT(v2f, o); //for single pass vr
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); //for single pass vr

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            #define INV_SQRT_OF_2PI 0.39894228040143267793994605993439  // 1.0/SQRT_OF_2PI
            #define INV_PI 0.31830988618379067153776752674503

            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            float2 EdgeUV(float2 uv, half OffX, half OffY) { return uv + float2(_MainTex_TexelSize.x * OffX, _MainTex_TexelSize.y * OffY); }
            half3 ApplySharpness(half3 c, float2 uv)
            {
                half3 sum_col = c;
                sum_col += UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, EdgeUV(uv, -1,0)).rgb;
                sum_col += UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, EdgeUV(uv, 1,0)).rgb;
                sum_col += UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, EdgeUV(uv, 0,-1)).rgb;
                sum_col += UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, EdgeUV(uv, 0,1)).rgb;
                return saturate((c * 2) - (sum_col/5));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); //for single pass vr
                float2 uv = i.uv;
                float4 col = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, uv);

                float2 mrUV= i.uv;
                if(_FlipX > 0) mrUV.x = 1.0 - mrUV.x;
                if(_FlipY > 0) mrUV.y = 1.0 - mrUV.y;

                if(_ScaleX > 1.0)
                {
                    mrUV.x += (_ScaleX - 1.0) * 0.5;
                }
                else
                {
                    mrUV.x -= (1.0 - _ScaleX) * 0.5;
                }
                if(_ScaleY > 1.0)
                {
                    mrUV.y += (_ScaleY - 1.0) * 0.5;
                }
                else
                {
                    mrUV.y -= (1.0 - _ScaleY) * 0.5;
                }

                mrUV.x /= _ScaleX;
                mrUV.y /= _ScaleY;
                mrUV.x += _OffsetX;
                mrUV.y += _OffsetY;
                
                fixed4 webcam = tex2D(_WebcamTex, mrUV);

                col.rgb = lerp(webcam.rgb, col.rgb, col.a);

                if(_Sharpen > 0) col.rgb = lerp(col.rgb, ApplySharpness(col.rgb, uv), _Sharpen);
                if(_DeNoise > 0)
                {
                    float sigma = 5;
                    float kSigma = 2;
                    //float threshold = 0.1;
                    float threshold = _DeNoise * 0.1;

                    //------------------------
                    float radius = round(kSigma*sigma);
                    float radQ = radius * radius;
    
                    float invSigmaQx2 = 0.5 / (sigma * sigma);// 1.0 / (sigma^2 * 2.0)
                    float invSigmaQx2PI = INV_PI * invSigmaQx2;// 1.0 / (sqrt(PI) * sigma)
    
                    float invThresholdSqx2 = .5 / (threshold * threshold);// 1.0 / (sigma^2 * 2.0)
                    float invThresholdSqrt2PI = INV_SQRT_OF_2PI / threshold;// 1.0 / (sqrt(2*PI) * sigma)

                    float4 centerPixel = col;
    
                    float zBuff = 0.0;
                    float4 aBuff = float4(0, 0, 0, 0);
                    float2 size = float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);

                    for(float x = -radius; x <= radius; x++)
                    {
                        float pt = sqrt(radQ - (x * x));  // pt = yRadius: have circular trend
                        for(float y=-pt; y <= pt; y++)
                        {
                            float2 d = float2(x,y);
                            float blurFactor = exp( -dot(d , d) * invSigmaQx2 ) * invSigmaQx2PI; 
                            float4 walkPixel =  UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, uv + (d/size));

                            float4 dC = walkPixel - centerPixel;
                            float deltaFactor = exp( -dot(dC, dC) * invThresholdSqx2) * invThresholdSqrt2PI * blurFactor;
                                 
                            zBuff += deltaFactor;
                            aBuff += deltaFactor * walkPixel;
                        }
                    }
                    col.rgb = (aBuff / zBuff).rgb;
                }

                col.rgb *= _Brightness;
                return col;
            }

            ENDCG
        }
    }
}

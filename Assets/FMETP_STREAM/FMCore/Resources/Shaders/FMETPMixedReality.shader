Shader "Hidden/FMETPMixedReality"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WebcamTex ("Webcam", 2D) = "white" {}

        _FlipX("FlipX", float) = 0
		_FlipY("FlipY", float) = 0
        _ScaleX ("ScaleX", float) = 1
        _ScaleY ("ScaleY", float) = 1
        _OffsetX ("OffsetX", float) = 0
        _OffsetY ("OffsetY", float) = 0
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

            //sampler2D _MainTex;
            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex); //for single pass vr
            sampler2D _WebcamTex;

            float _FlipX;
			float _FlipY;
            float _ScaleX;
            float _ScaleY;
            float _OffsetX;
            float _OffsetY;

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); //for single pass vr
                fixed4 col = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv);

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
                return col;
            }
            ENDCG
        }
    }
}

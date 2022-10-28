Shader "Hidden/FMDesktopMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _CursorTex ("Cursor", 2D) = "white" {}

        _FrameWidth ("FrameWidth", float) = 1
        _FrameHeight ("FrameHeight", float) = 1

        _CursorWidth ("CursorWidth", float) = 1
        _CursorHeight ("CursorHeight", float) = 1

        _MonitorScaling ("MonitorScaling", float) = 1
        
        _CursorPointX ("CursorPointX", float) = 1
        _CursorPointY ("CursorPointY", float) = 1

        _FlipX("FlipX", float) = 0
		_FlipY("FlipY", float) = 0

        _RangeX("RangeX", float) = 1
		_RangeY("RangeY", float) = 1

		_OffsetX("OffsetX", float) = 1
		_OffsetY("OffsetY", float) = 1

		_RotationAngle("Rotation(Angle)", float) = 1

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

            #define IF(a, b, c) lerp(b, c, step((float) (a), 0))
			#define PI 3.14159

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
            sampler2D _CursorTex;
            
            float _FrameWidth;
            float _FrameHeight;

            float _CursorWidth;
            float _CursorHeight;

            float _MonitorScaling;

            float _CursorPointX;
            float _CursorPointY;

            float _FlipX;
			float _FlipY;

			float _RangeX;
			float _RangeY;
			float _OffsetX;
			float _OffsetY;
			float _RotationAngle;

            float _Brightness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                #if !UNITY_UV_STARTS_AT_TOP
				o.uv.y = IF(_ProjectionParams.x < 0, 1.0 - o.uv.y, o.uv.y);
				#endif


				if (_FlipX > 0) o.uv.x = 1.0 - o.uv.x;
				if (_FlipY > 0) o.uv.y = 1.0 - o.uv.y;


                //correct orientation
				o.uv -= 0.5;
				float angle = (_RotationAngle / 180) * PI;

				float s = sin(angle);
				float c = cos(angle);
				float2x2 rotationMatrix = float2x2(c, -s, s, c);
				rotationMatrix *= 0.5;
				rotationMatrix += 0.5;
				rotationMatrix = rotationMatrix * 2.0 - 1.0;
				o.uv = mul(o.uv, rotationMatrix);
				o.uv += 0.5;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float screenWidth = _FrameWidth;
                float screenHeight = _FrameHeight;
                
                float monitorScaling = _MonitorScaling;
                float cursorWidth = _CursorWidth;
                float cursorHeight = _CursorHeight;

                float cursorPointX = _CursorPointX;
                float cursorPointY = _CursorPointY;

                float2 uv = i.uv;
                uv.x = uv.x * _RangeX;
				uv.y = uv.y * _RangeY;
				uv.x += (1.0 - _RangeX) * 0.5 + (_OffsetX);
				uv.y += (1.0 - _RangeY) * 0.5 + (_OffsetY);

				uv.x = uv.x % 1;
				uv.y = uv.y % 1;
				if (uv.x < 0) uv.x = 1.0 + uv.x;
				if (uv.y < 0) uv.y = 1.0 + uv.y;

                fixed4 screenColor = tex2D(_MainTex, uv);
                float2 uvCursor = uv;
                uvCursor.x *= monitorScaling * screenWidth / cursorWidth;
                uvCursor.y *= monitorScaling * screenHeight / cursorHeight;

                float2 pointUV = float2(cursorPointX / screenWidth, cursorPointY / screenHeight);
                float2 cursorScreenRatio = float2(cursorWidth / screenWidth, cursorHeight / screenHeight);

                float boundleft = pointUV.x;
                float boundright = pointUV.x + (cursorScreenRatio.x / monitorScaling);
                
                float boundbottom = pointUV.y;
                float boundtop = pointUV.y + (cursorScreenRatio.y / monitorScaling);

                if(uv.x > boundleft && uv.x < boundright && uv.y > boundbottom && uv.y < boundtop)
                {
                    float uvCursorOffX = (cursorPointX / cursorWidth) * monitorScaling;
                    float uvCursorOffY = (cursorPointY / cursorHeight) * monitorScaling;

                    uvCursor.x -= uvCursorOffX;
                    uvCursor.y -= uvCursorOffY;

                    fixed4 cursorColor = tex2D(_CursorTex, uvCursor);
                    if(cursorColor.a > 0) screenColor.rgb = cursorColor.rgb;
                }

                screenColor.rgb *= _Brightness;
                screenColor.rgb = fixed3(screenColor.b, screenColor.g, screenColor.r);
                
                return screenColor;
            }
            ENDCG
        }
    }
}

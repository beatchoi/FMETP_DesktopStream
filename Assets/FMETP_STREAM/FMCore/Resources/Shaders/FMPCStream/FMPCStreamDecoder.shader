Shader "Hidden/FMPCStreamDecoder"
{
    Properties     
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _PointSize("Point size", Range(0.00001, 100)) = 0.02
        [Toggle] _ApplyDistance("Apply Distance", Float) = 1

        [Toggle] _OrthographicProjection("Orthographic Projection", Float) = 0

        _OrthographicSize("OrthographicSize", Range(0.00001, 100)) = 1
        _Aspect("Aspect", Range(0.00001, 100)) = 1

        _NearClipPlane("NearClipPlane", Range(0.00001, 100)) = 0.3
        _FarClipPlane("FarClipPlane", Range(0.00001, 100)) = 10
        _VerticalFOV("VerticalFOV", Range(0.00001, 180)) = 60
    }       
    SubShader     
    {   
        Tags { "RenderType"="Transparent" "IgnoreProjector"="True" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag  
            #include "UnityCG.cginc"
            #define IF(a, b, c) lerp(b, c, step((float) (a), 0));

            uniform float4 _Color;
            uniform float _PointSize;  
            uniform float _ApplyDistance;

            uniform float _OrthographicSize;
            uniform float _Aspect;

            uniform float _NearClipPlane;
            uniform float _FarClipPlane;

            uniform float _VerticalFOV;
            uniform float _OrthographicProjection;

            struct appdata             
            {                 
                float4 vertex: POSITION;                 
                float4 color: COLOR;       
                float2 uv : TEXCOORD0;  
            };               

            struct v2f          
            {
                float4 pos: SV_POSITION;                 
                float4 col: COLOR;                 
                float size: PSIZE;     
                float2 uv : TEXCOORD0;
            };               


            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

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

            float2 UVOff(float2 uv, half OffX, half OffY) { return uv + float2(_MainTex_TexelSize.x * OffX, _MainTex_TexelSize.y * OffY); }
            float averagedDepth(float2 uv)
            {
                float3 inputRGB = tex2Dlod(_MainTex, float4(uv, 0.0, 0.0));
                if(inputRGB.b == 1) return 1.0;

                float decodeDepth0 = RGBToFloat(inputRGB.rgb, 1.0);
                if(decodeDepth0 > 0.96) return 1.0;
                
                float decodeDepth1 = RGBToFloat(tex2Dlod(_MainTex, float4(UVOff(uv, 0.0, 1.0), 0, 0)), 1.0);
                float decodeDepth2 = RGBToFloat(tex2Dlod(_MainTex, float4(UVOff(uv, 0.0, -1.0), 0, 0)), 1.0);
                float decodeDepth3 = RGBToFloat(tex2Dlod(_MainTex, float4(UVOff(uv, 1.0, 0), 0, 0)), 1.0);
                float decodeDepth4 = RGBToFloat(tex2Dlod(_MainTex, float4(UVOff(uv, -1.0, 0), 0, 0)), 1.0);

                float decodeDepth5 = RGBToFloat(tex2Dlod(_MainTex, float4(UVOff(uv, 1.0, 1.0), 0, 0)), 1.0);
                float decodeDepth6 = RGBToFloat(tex2Dlod(_MainTex, float4(UVOff(uv, -1.0, 1.0), 0, 0)), 1.0);
                float decodeDepth7 = RGBToFloat(tex2Dlod(_MainTex, float4(UVOff(uv, 1.0, -1.0), 0, 0)), 1.0);
                float decodeDepth8 = RGBToFloat(tex2Dlod(_MainTex, float4(UVOff(uv, -1.0, -1.0), 0, 0)), 1.0);

                float threshold = 0.08;
                float delta = abs(decodeDepth0-decodeDepth1);
                delta += abs(decodeDepth0-decodeDepth2);
                delta += abs(decodeDepth0-decodeDepth3);
                delta += abs(decodeDepth0-decodeDepth4);
                delta += abs(decodeDepth0-decodeDepth5);
                delta += abs(decodeDepth0-decodeDepth6);
                delta += abs(decodeDepth0-decodeDepth7);
                delta += abs(decodeDepth0-decodeDepth8);

                return IF(delta > threshold * 8, 1.0, (decodeDepth0 + decodeDepth1 + decodeDepth2 + decodeDepth3 + decodeDepth4 + decodeDepth5 + decodeDepth6 + decodeDepth7 + decodeDepth8 ) / 9);
            }

            v2f vert(appdata v)             
            {                 
                v2f o;

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                float4 pos = v.vertex;

                float2 uvDepth = pos.xy;
                float2 uvColor = pos.xy;
                uvDepth.x /= 2.0;
                
                float4 sampleDepth = tex2Dlod(_MainTex, float4(uvDepth, 0.0, 0.0));
                float decodeDepth = averagedDepth(uvDepth);

                if(_OrthographicProjection > 0)
                {
                    //=========== orth ==============
                    float depthClip = _FarClipPlane - _NearClipPlane;
                    pos.z = _NearClipPlane + (decodeDepth * depthClip);
                    pos.z = decodeDepth * _FarClipPlane;

                    pos.y -= 0.5;
                    pos.y *= _OrthographicSize * 2;

                    pos.x -= 0.5;
                    pos.x *= _OrthographicSize * 2 * _Aspect;
                }
                else
                {
                    //=========== fov ==============
                    float vfov = ((_VerticalFOV)/180) * 3.14159265359;
                    float hfov = 2.0 * atan(tan(vfov / 2) * _Aspect);

                    float3 dir = float3(1,1,1);
                    float d = _FarClipPlane;
                    dir.z = d;

                    pos.y -= 0.5;
                    pos.y *= 2.0;
                    pos.y *= tan(vfov * 0.5) * d;
                    float angY = atan(pos.y/d);
                    dir.y = tan(angY) * d;

                    pos.x -= 0.5;
                    pos.x *= 2.0;
                    pos.x *= tan(hfov * 0.5) * d;

                    //pos.x /= _Aspect;
                    float angX = atan(pos.x/d);
                    dir.x = tan(angX) * d;
                    pos.xyz = decodeDepth * dir;
                }

                o.pos = UnityObjectToClipPos(pos);

                uvColor.x /= 2.0;
                uvColor.x += 0.5;
                float4 sampleColor = tex2Dlod(_MainTex, float4(uvColor, 0.0, 0.0));
                sampleColor.rgb *= 2;
                o.col = sampleColor;
                    
                o.col.rgb *= _Color.rgb;
                #if !UNITY_COLORSPACE_GAMMA
                o.col.rgb = pow(o.col.rgb, 2.2);
                #endif

                o.col.a = IF(decodeDepth > 0.96, 0.0, _Color.a);

                o.size = IF(_ApplyDistance > 0, _PointSize / o.pos.w * _ScreenParams.y, _PointSize);
                #ifdef SHADER_API_MOBILE
                //o.size *= 2;
                #endif
                
                return o;             
            }               

            float4 frag(v2f o) : COLOR             
            {            
                return o.col;             
            }             
            ENDCG         
          }     
      } 
}

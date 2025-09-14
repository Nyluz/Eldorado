Shader "Custom/HexTileOutline"
{
    Properties
    {
        _Color ("Fill Color", Color) = (1,1,1,1)
        _EdgeColor ("Edge Color", Color) = (0,0,0,1)
        _EdgeThickness ("Edge Thickness", Range(0.0,0.2)) = 0.05
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
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _Color;
            float4 _EdgeColor;
            float _EdgeThickness;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // distance to nearest edge in UV space
                float2 d = abs(i.uv - 0.5) * 2.0;
                float edge = max(d.x, d.y);

                // simple edge fade
                float mask = smoothstep(1.0 - _EdgeThickness, 1.0, edge);

                return lerp(_Color, _EdgeColor, mask);
            }
            ENDCG
        }
    }
}
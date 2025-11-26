Shader "Custom/PortalShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Speed ("Speed", Float) = 1
        _Distortion ("Distortion", Float) = 0.2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float4 _Color;
            float _Speed;
            float _Distortion;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float rand(float2 n) { return frac(sin(dot(n, float2(12.9898, 4.1414))) * 43758.5453); }

            float2 swirl(float2 uv, float t)
            {
                float angle = t * 0.5 + length(uv - 0.5) * 4;
                float s = sin(angle);
                float c = cos(angle);
                uv -= 0.5;
                float2 rotated = float2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);
                return rotated + 0.5;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y * _Speed;
                float2 uv = swirl(i.uv, t);
                float2 offset = (uv - 0.5) * _Distortion;
                float4 tex = tex2D(_MainTex, uv + offset);
                return tex * _Color;
            }
            ENDCG
        }
    }
}

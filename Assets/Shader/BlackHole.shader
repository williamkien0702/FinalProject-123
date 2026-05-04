Shader "Custom/SwirlFade"
{
    Properties
    {
        _MainTex ("Texture 1", 2D) = "white" {}
        _SecondTex ("Texture 2", 2D) = "white" {}
        _SwirlSpeed ("Swirl Speed", Float) = 1.0
        _SwirlStrength ("Swirl Strength", Float) = 8.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
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
            sampler2D _SecondTex;
            float _SwirlSpeed;
            float _SwirlStrength;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 center = float2(0.5, 0.5);

                // Move UV so center is at origin
                float2 offset = uv - center;

                // Distance from center — stronger swirl near the middle
                float dist = length(offset);

                // Continuously increasing angle over time — this is what
                // makes it spin like a whirlpool instead of back and forth
                float angle = _Time.y * _SwirlSpeed * (1.0 - dist) * _SwirlStrength;

                // Rotate UV around center
                float s = sin(angle);
                float c = cos(angle);

                float2 rotatedOffset;
                rotatedOffset.x = offset.x * c - offset.y * s;
                rotatedOffset.y = offset.x * s + offset.y * c;

                // Convert back to normal UV space
                float2 newUV = rotatedOffset + center;

                // Blend the two textures using the swirled UV
                // Slowly oscillate the blend so both textures are visible
                float blend = sin(_Time.y * 0.5) * 0.5 + 0.5;

                fixed4 tex1 = tex2D(_MainTex, newUV);
                fixed4 tex2 = tex2D(_SecondTex, newUV);

                return lerp(tex1, tex2, blend);
            }
            ENDCG
        }
    }
}

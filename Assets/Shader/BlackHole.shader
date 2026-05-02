Shader "Custom/SwirlFade"
{
    Properties
    {
        _MainTex ("Texture 1", 2D) = "white" {}
        _SecondTex ("Texture 2", 2D) = "white" {}
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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // store the original UV coordinates
                float2 uv = i.uv;

                // set the center of the image
                float2 center = float2(0.5, 0.5);

                // make the animation loop over time
                float t = frac(_Time.y * 0.1);

                // split the loop into two halves
                float halfCycle = floor(t * 2.0);

                // local time inside each half, from 0 to 1
                float localT = frac(t * 2.0);

                // make the swirl go from weak to strong and then back to weak
                float swirlAmount = sin(localT * 3.14159);

                // fade amount goes from 0 to 1 during the half cycle
                float fade = localT;

                // first half = clockwise
                float direction = 1.0;
                // second half = counterclockwise
                if (halfCycle >= 1.0)
                {
                    direction = -1.0;
                }

                // move UV so the center is at (0,0)
                float2 offset = uv - center;

                // distance from center
                float dist = length(offset);

                // stronger swirl near the center
                float angle = swirlAmount * direction * (1.0 - dist) * 10.0;

                // rotate the UV around the center
                float s = sin(angle);
                float c = cos(angle);

                float2 rotatedOffset;
                rotatedOffset.x = offset.x * c - offset.y * s;
                rotatedOffset.y = offset.x * s + offset.y * c;

                // convert back to normal UV space
                float2 newUV = rotatedOffset + center;

                // sample both textures using the swirled UV
                fixed4 tex1 = tex2D(_MainTex, newUV);
                fixed4 tex2 = tex2D(_SecondTex, newUV);

                fixed4 col;

                if (halfCycle < 1.0)
                {
                    // first half: fade from picture 1 to picture 2
                    col = lerp(tex1, tex2, fade);
                }
                else
                {
                    // second half: fade from picture 2 back to picture 1
                    col = lerp(tex2, tex1, fade);
                }

                return col;
            }
            ENDCG
        }
    }
}
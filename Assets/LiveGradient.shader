Shader "Custom/GradientWithMovingLight"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}

        [Header(Gradient Colors)]
        _Color1("Color 1", Color) = (1, 0, 0, 1)
        _Color2("Color 2", Color) = (0, 1, 0, 1)
        _Color3("Color 3", Color) = (0, 0, 1, 1)
        _Speed("Gradient Speed", Range(0, 5)) = 1

        [Header(Noise)]
        _NoiseScale("Noise Scale", Range(0.1, 10)) = 4
        _NoiseSpeed("Noise Speed", Range(0, 5)) = 0.5
        _NoiseStrength("Noise Strength", Range(0, 5)) = 1
        _NoiseOffset("Noise Offset", Vector) = (0, 0, 0, 0)

        [Header(Pulse)]
        _PulseStrength("Pulse Strength", Range(0, 1)) = 0.2
        _PulseSpeed("Pulse Speed", Range(0, 10)) = 2
        _PulseNoiseScale("Pulse Noise Scale", Range(0.1, 10)) = 2

        [Header(Alpha Fade)]
        _AlphaFadeStrength("Alpha Fade Strength", Range(0, 1)) = 0.4

        [Header(Local Glow)]
        _GlowColor("Glow Color", Color) = (1,1,1,1)
        _GlowIntensity("Glow Intensity", Range(0, 2)) = 0.5
        _GlowMask("Glow Mask", 2D) = "white" {}

        [Header(Floating Circle Area)]
        _BaseColor("Background Color", Color) = (0.2,0.2,0.2,1)

        [Header(Moving Light Circle)]
        _LightColor("Light Circle Color", Color) = (1,1,1,1)
        _LightSize("Light Circle Size", Range(0.01, 0.5)) = 0.15
        _LightIntensity("Light Intensity", Range(0, 1)) = 0.5
        _LightMoveSpeed("Light Move Speed", Range(0, 3)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _GlowMask;
            float4    _MainTex_ST;

            float4 _Color1, _Color2, _Color3;
            float  _Speed;

            float  _NoiseScale, _NoiseSpeed, _NoiseStrength;
            float4 _NoiseOffset;

            float _PulseStrength, _PulseSpeed, _PulseNoiseScale;
            float _AlphaFadeStrength;

            float4 _GlowColor;
            float  _GlowIntensity;

            float4 _BaseColor;

            float4 _LightColor;
            float  _LightSize;
            float  _LightIntensity;
            float  _LightMoveSpeed;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float  a = hash(i);
                float  b = hash(i + float2(1, 0));
                float  c = hash(i + float2(0, 1));
                float  d = hash(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            float2 movingLightCenter(float t)
            {
                float seedTime = floor(t * _LightMoveSpeed);
                float interp = frac(t * _LightMoveSpeed);

                float2 seedA = float2(seedTime, 0.73);
                float2 seedB = float2(seedTime + 1.0, 0.73);

                float offsetA = noise(seedA + 5.4) * 0.3 * _LightSize;
                float offsetB = noise(seedB + 5.4) * 0.3 * _LightSize;

                float paddingA = _LightSize + offsetA;
                float paddingB = _LightSize + offsetB;

                float2 posRawA = float2(noise(seedA), noise(seedA + 1.3));
                float2 posRawB = float2(noise(seedB), noise(seedB + 1.3));

                float2 posA = lerp(float2(paddingA, paddingA), float2(1.0 - paddingA, 1.0 - paddingA), posRawA);
                float2 posB = lerp(float2(paddingB, paddingB), float2(1.0 - paddingB, 1.0 - paddingB), posRawB);

                return lerp(posA, posB, smoothstep(0.0, 1.0, interp));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float  time = _Time.y;

                float2 center = movingLightCenter(time);
                float  d = distance(uv, center);
                float  insideLight = smoothstep(_LightSize, _LightSize * 0.85, d);

                float4 finalColor = _BaseColor;

                if (insideLight > 0.001)
                {
                    float2 noiseUV = uv * _NoiseScale + time * _NoiseSpeed + _NoiseOffset.xy;
                    float  n = noise(noiseUV);
                    float  t = (time + n * _NoiseStrength) * _Speed;
                    float  cycle = frac(t * 0.2);
                    float  scaledTime = cycle * 3;
                    uint   indexA = (uint)floor(scaledTime) % 3;
                    uint   indexB = (indexA + 1) % 3;
                    float  lerpFactor = frac(scaledTime);

                    float4 colorA = (indexA == 0) ? _Color1 : (indexA == 1) ? _Color2 : _Color3;
                    float4 colorB = (indexB == 0) ? _Color1 : (indexB == 1) ? _Color2 : _Color3;

                    float4 colorIn = lerp(colorA, colorB, lerpFactor);

                    float pulseNoise = noise(uv * _PulseNoiseScale + _NoiseOffset.xy);
                    float pulse = 1.0 + sin((time + pulseNoise) * _PulseSpeed) * _PulseStrength;
                    colorIn.rgb *= pulse;

                    // Учет _LightIntensity
                    colorIn.rgb *= _LightIntensity;

                    finalColor.rgb = lerp(finalColor.rgb, colorIn.rgb, insideLight);
                }

                float glowMask = tex2D(_GlowMask, uv).r;
                finalColor.rgb += glowMask * _GlowColor.rgb * _GlowIntensity;

                float alpha = tex2D(_MainTex, uv).a;
                float alphaFade = 0.5 + 0.5 * sin(time * _PulseSpeed);
                alpha *= lerp(1.0 - _AlphaFadeStrength, 1.0, alphaFade);

                return float4(finalColor.rgb, alpha);
            }
            ENDCG
        }
    }
}

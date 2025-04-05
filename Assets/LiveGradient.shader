Shader "Custom/LiveGradientHSV"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        [Header(Gradient Colors)]
        _Color1 ("Color 1", Color) = (1, 0, 0, 1)
        _Color2 ("Color 2", Color) = (0, 1, 0, 1)
        _Color3 ("Color 3", Color) = (0, 0, 1, 1)
        _Speed ("Gradient Speed", Range(0, 5)) = 1

        [Header(Gradient Noise)]
        _NoiseScale ("Noise Scale", Range(0.1, 10)) = 4
        _NoiseSpeed ("Noise Speed", Range(0, 5)) = 0.5
        _NoiseStrength ("Noise Strength", Range(0, 5)) = 1
        _NoiseOffset ("Noise Offset", Vector) = (0, 0, 0, 0)

        [Header(Pulse)]
        _PulseStrength ("Pulse Strength", Range(0, 1)) = 0.2
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2
        _PulseNoiseScale ("Pulse Noise Scale", Range(0.1, 10)) = 2

        [Header(Color Breathing)]
        _ColorBreathScale ("Breath Scale", Range(1, 30)) = 10
        _ColorBreathSpeed ("Breath Speed", Range(0, 10)) = 2
        _ColorBreathStrength ("Breath Strength", Range(0, 1)) = 0.2

        [Toggle(_HSV_VIBRATION)] _UseHSVVibration ("Use HSV Vibration", Float) = 0
        _HSVStrength ("HSV Vibe Strength", Range(0, 0.5)) = 0.1

        [Header(Alpha Fade)]
        _AlphaFadeStrength ("Alpha Fade Strength", Range(0, 1)) = 0.4

        [Header(Local Glow)]
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _GlowIntensity ("Glow Intensity", Range(0, 2)) = 0.5
        _GlowMask ("Glow Mask", 2D) = "white" {}
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
            #pragma multi_compile _HSV_VIBRATION_OFF _HSV_VIBRATION
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _GlowMask;
            float4 _MainTex_ST;

            float4 _Color1, _Color2, _Color3;
            float _Speed;

            float _NoiseScale, _NoiseSpeed, _NoiseStrength;
            float4 _NoiseOffset;

            float _PulseStrength, _PulseSpeed, _PulseNoiseScale;
            float _ColorBreathScale, _ColorBreathSpeed, _ColorBreathStrength;

            float _HSVStrength;
            float _AlphaFadeStrength;

            float4 _GlowColor;
            float _GlowIntensity;

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
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float3 RgbToHsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = c.g < c.b ? float4(c.bg, K.wz) : float4(c.gb, K.xy);
                float4 q = c.r < p.x ? float4(p.xyw, c.r) : float4(c.r, p.yzx);
                float d = q.x - min(q.w, q.y);
                float e = 1e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 HsvToRgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float2 noiseUV = uv * _NoiseScale + _Time.y * _NoiseSpeed + _NoiseOffset.xy;
                float n = noise(noiseUV);
                float t = (_Time.y + n * _NoiseStrength) * _Speed;

                float cycle = frac(t * 0.2);
                float scaledTime = cycle * 3;
                uint indexA = (uint)floor(scaledTime) % 3;
                uint indexB = (indexA + 1) % 3;
                float lerpFactor = frac(scaledTime);

                float4 colorA = (indexA == 0) ? _Color1 :
                                (indexA == 1) ? _Color2 : _Color3;
                float4 colorB = (indexB == 0) ? _Color1 :
                                (indexB == 1) ? _Color2 : _Color3;

                float4 finalColor = lerp(colorA, colorB, lerpFactor);

                // === PULSE ===
                float pulseNoise = noise(uv * _PulseNoiseScale + _NoiseOffset.xy);
                float pulse = 1.0 + sin((_Time.y + pulseNoise) * _PulseSpeed) * _PulseStrength;
                finalColor.rgb *= pulse;

                // === VIBRATION ===
                #ifdef _HSV_VIBRATION
                    float3 hsv = RgbToHsv(finalColor.rgb);
                    float hsvNoise = noise(uv * _ColorBreathScale + _Time.y * _ColorBreathSpeed + _NoiseOffset.xy);
                    hsv.x += (hsvNoise - 0.5) * _HSVStrength;
                    hsv.x = frac(hsv.x);
                    finalColor.rgb = HsvToRgb(hsv);
                #else
                    float breathNoise = noise(uv * _ColorBreathScale + _Time.y * _ColorBreathSpeed + _NoiseOffset.xy);
                    float breath = 1.0 + (breathNoise - 0.5) * _ColorBreathStrength;
                    finalColor.rgb *= breath;
                #endif

                // === ALPHA ===
                float alpha = tex2D(_MainTex, uv).a;
                float alphaFade = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed);
                alpha *= lerp(1.0 - _AlphaFadeStrength, 1.0, alphaFade);

                // === GLOW ===
                float glowMask = tex2D(_GlowMask, uv).r;
                finalColor.rgb += glowMask * _GlowColor.rgb * _GlowIntensity;

                return float4(finalColor.rgb, alpha);
            }
            ENDCG
        }
    }
}

// Cine Quest — Locked Video Display (URP Unlit)
// CRITICAL: No tonemapping, bloom, auto-exposure, or scene-dependent processing.
// All adjustments are explicit user parameters. Bypass forces identity (plus optional range).
// Color math is intentional and deterministic for DP iris / lighting evaluation.

Shader "CineQuest/LockedVideo"
{
    Properties
    {
        [MainTexture] _MainTex ("Video", 2D) = "black" {}
        _Bypass ("Bypass (1=identity)", Float) = 0
        _LimitedRange ("Limited Range Rec.709", Float) = 1
        _Brightness ("Brightness / Gain Offset", Float) = 0
        _Contrast ("Contrast", Float) = 1
        _Gamma ("Gamma", Float) = 1
        _Saturation ("Saturation", Float) = 1
        _Temperature ("Temperature", Float) = 0
        _Tint ("Tint", Float) = 0
        _Lift ("Black Level / Lift", Float) = 0
        _Opacity ("Opacity", Range(0,1)) = 1
        [Toggle] _FlipY ("Flip Y", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }
        LOD 100
        ZWrite On
        Cull Off
        // Unlit, no fog, no lighting
        Lighting Off

        Pass
        {
            Name "LockedVideo"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Bypass;
                float _LimitedRange;
                float _Brightness;
                float _Contrast;
                float _Gamma;
                float _Saturation;
                float _Temperature;
                float _Tint;
                float _Lift;
                float _Opacity;
                float _FlipY;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                float2 uv = v.uv;
                if (_FlipY > 0.5) uv.y = 1.0 - uv.y;
                o.uv = TRANSFORM_TEX(uv, _MainTex);
                return o;
            }

            // Expand Rec.709 limited (16–235 in 8-bit) to full 0–1 domain for processing/display.
            float3 ExpandLimited(float3 c)
            {
                // 16/255 ≈ 0.062745, 219/255 ≈ 0.858824 (235-16)
                const float offset = 16.0 / 255.0;
                const float scale = 255.0 / 219.0;
                return saturate((c - offset) * scale);
            }

            // Rec.709 luma coefficients
            float Luma709(float3 c)
            {
                return dot(c, float3(0.2126, 0.7152, 0.0722));
            }

            float3 ApplySaturation(float3 c, float sat)
            {
                float y = Luma709(c);
                return lerp(float3(y, y, y), c, sat);
            }

            // Simple temperature / tint via RGB scaling (not a full Bradford CAT — intentional, transparent).
            float3 ApplyTempTint(float3 c, float temp, float tint)
            {
                // temp > 0 → warmer (more R, less B); temp < 0 → cooler
                float3 scale = float3(
                    1.0 + temp * 0.25,
                    1.0 + tint * 0.15,
                    1.0 - temp * 0.25
                );
                return c * scale;
            }

            float3 ApplyGrade(float3 c)
            {
                // Lift (black level)
                c = c + _Lift;

                // Brightness / gain offset
                c = c + _Brightness;

                // Contrast around mid-gray: (c - 0.5) * contrast + 0.5
                c = (c - 0.5) * _Contrast + 0.5;

                // Saturation
                c = ApplySaturation(c, _Saturation);

                // Temperature / Tint
                c = ApplyTempTint(c, _Temperature, _Tint);

                // Gamma (encode-style)
                c = sign(c) * pow(abs(c) + 1e-5, 1.0 / max(_Gamma, 1e-3));

                return saturate(c);
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 raw = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float3 c = raw.rgb;

                // Optional limited-range expand (broadcast legal → full for display)
                if (_LimitedRange > 0.5)
                {
                    c = ExpandLimited(c);
                }

                // Bypass / Reference: no creative grade — identity after optional range expand
                if (_Bypass < 0.5)
                {
                    c = ApplyGrade(c);
                }

                // Output linear-ish RGB. URP color management should not tonemap this material.
                // Do NOT apply ACES / filmic curves here.
                return half4(c, _Opacity * raw.a);
            }
            ENDHLSL
        }
    }

    // Fallback for non-URP editor previews
    FallBack Off
}

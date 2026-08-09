// Cine Quest — Waveform / Parade visualization with IRE graticule overlay.

Shader "CineQuest/ScopeWaveformViz"
{
    Properties
    {
        [MainTexture] _MainTex ("Scope", 2D) = "black" {}
        _Opacity ("Opacity", Range(0,1)) = 0.95
        _ShowGraticule ("Show Graticule", Float) = 1
        _LegalLow ("Legal Low IRE", Float) = 0
        _LegalHigh ("Legal High IRE", Float) = 100
        _Background ("Background", Color) = (0.02, 0.04, 0.03, 0.92)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Opacity;
                float _ShowGraticule;
                float _LegalLow;
                float _LegalHigh;
                float4 _Background;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float GraticuleLine(float y, float ire, float thickness)
            {
                // y is 0 at bottom, 1 at top; map IRE 0–100 (extend to 109)
                float t = ire / 109.0;
                return smoothstep(thickness, 0.0, abs(y - t));
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Scope data: x = spatial, y = level (bin)
                float2 uv = i.uv;
                float4 scope = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x, uv.y));
                float3 col = _Background.rgb;
                col = lerp(col, scope.rgb, saturate(scope.a + length(scope.rgb)));

                if (_ShowGraticule > 0.5)
                {
                    float g = 0;
                    g = max(g, GraticuleLine(uv.y, 0, 0.004));
                    g = max(g, GraticuleLine(uv.y, 7.5, 0.003));
                    g = max(g, GraticuleLine(uv.y, 50, 0.003));
                    g = max(g, GraticuleLine(uv.y, 100, 0.004));
                    g = max(g, GraticuleLine(uv.y, 109, 0.003));
                    // Legal limits
                    g = max(g, GraticuleLine(uv.y, _LegalLow, 0.002) * 0.6);
                    g = max(g, GraticuleLine(uv.y, _LegalHigh, 0.002) * 0.6);
                    col = lerp(col, float3(0.7, 0.9, 0.7), g * 0.65);
                }

                return half4(col, _Opacity);
            }
            ENDHLSL
        }
    }
    FallBack Off
}

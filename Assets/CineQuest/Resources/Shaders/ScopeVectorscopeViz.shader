// Cine Quest — Vectorscope visualization with professional graticule:
// skin-tone line, 75%/100% color boxes, primary targets (approximate Rec.709).

Shader "CineQuest/ScopeVectorscopeViz"
{
    Properties
    {
        [MainTexture] _MainTex ("Vectorscope", 2D) = "black" {}
        _Opacity ("Opacity", Range(0,1)) = 0.95
        _ShowGraticule ("Show Graticule", Float) = 1
        _Background ("Background", Color) = (0.02, 0.02, 0.05, 0.92)
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

            float sdSegment(float2 p, float2 a, float2 b)
            {
                float2 pa = p - a, ba = b - a;
                float h = saturate(dot(pa, ba) / dot(ba, ba));
                return length(pa - ba * h);
            }

            float sdBox(float2 p, float2 c, float2 halfSize)
            {
                float2 d = abs(p - c) - halfSize;
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                float4 scope = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                float3 col = lerp(_Background.rgb, scope.rgb, saturate(length(scope.rgb) * 1.5));

                if (_ShowGraticule > 0.5)
                {
                    // Centered coords in [-1,1], x~Cb, y~Cr
                    float2 p = uv * 2.0 - 1.0;
                    float g = 0;

                    // Crosshair
                    g = max(g, smoothstep(0.01, 0.0, abs(p.x)) * 0.4);
                    g = max(g, smoothstep(0.01, 0.0, abs(p.y)) * 0.4);

                    // Circle at ~75% and 100% chroma radius (approx)
                    float r = length(p);
                    g = max(g, smoothstep(0.015, 0.0, abs(r - 0.55)) * 0.5); // ~75%
                    g = max(g, smoothstep(0.015, 0.0, abs(r - 0.75)) * 0.5); // ~100%

                    // Skin-tone line (Rec.709 I-line approx ~123° from B-Y; simplified slope)
                    // Standard skin line is roughly along Cr positive / Cb slightly negative
                    float2 skinA = float2(0.0, 0.0);
                    float2 skinB = float2(-0.25, 0.55);
                    g = max(g, smoothstep(0.02, 0.0, sdSegment(p, skinA, skinB)) * 0.9);

                    // Primary boxes (R,G,B,C,M,Y) approximate target boxes at 75%
                    float2 targets[6] = {
                        float2(0.15, 0.55),   // R
                        float2(-0.35, 0.25),  // G
                        float2(0.35, -0.25),  // B
                        float2(-0.15, -0.55), // Cy
                        float2(0.45, 0.15),   // Mg
                        float2(-0.45, -0.05)  // Ye
                    };
                    for (int t = 0; t < 6; t++)
                    {
                        float d = sdBox(p, targets[t], float2(0.04, 0.04));
                        g = max(g, smoothstep(0.01, 0.0, abs(d)) * 0.7);
                    }

                    col = lerp(col, float3(0.85, 0.85, 0.9), g);
                }

                return half4(col, _Opacity);
            }
            ENDHLSL
        }
    }
    FallBack Off
}

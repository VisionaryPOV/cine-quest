// Cine Quest — False-color / zebra exposure assist (stretch goal).
// Samples video, maps Rec.709 luma to false-color bands + optional zebra stripes.
// Does NOT affect the primary locked monitoring path unless user enables overlay.

Shader "CineQuest/FalseColor"
{
    Properties
    {
        [MainTexture] _MainTex ("Video", 2D) = "black" {}
        _LimitedRange ("Limited Range", Float) = 1
        _Zebra ("Zebra Enable", Float) = 1
        _ZebraThreshold ("Zebra IRE Threshold", Range(0,1)) = 0.9
        _Opacity ("Opacity", Range(0,1)) = 0.85
        _FlipY ("Flip Y", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "FalseColor"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _LimitedRange;
                float _Zebra;
                float _ZebraThreshold;
                float _Opacity;
                float _FlipY;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                float2 uv = v.uv;
                if (_FlipY > 0.5) uv.y = 1.0 - uv.y;
                o.uv = TRANSFORM_TEX(uv, _MainTex);
                return o;
            }

            float Luma709(float3 c) { return dot(c, float3(0.2126, 0.7152, 0.0722)); }

            float3 ExpandLimited(float3 c)
            {
                return saturate((c - 16.0/255.0) * (255.0/219.0));
            }

            // Classic-ish false color bands by IRE
            float3 FalseColor(float y)
            {
                if (y < 0.02) return float3(0.5, 0.0, 0.5);      // underexposed purple
                if (y < 0.10) return float3(0.0, 0.0, 1.0);      // blue
                if (y < 0.20) return float3(0.0, 0.6, 1.0);      // cyan-blue
                if (y < 0.32) return float3(0.0, 1.0, 0.0);      // green (18% region)
                if (y < 0.45) return float3(0.6, 1.0, 0.0);      // yellow-green
                if (y < 0.60) return float3(1.0, 1.0, 0.0);      // yellow
                if (y < 0.75) return float3(1.0, 0.5, 0.0);      // orange
                if (y < 0.90) return float3(1.0, 0.0, 0.0);      // red
                return float3(1.0, 1.0, 1.0);                     // clip white
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb;
                if (_LimitedRange > 0.5) c = ExpandLimited(c);
                float y = Luma709(c);

                float3 fc = FalseColor(y);

                // Zebra stripes near/over threshold
                if (_Zebra > 0.5 && y >= _ZebraThreshold)
                {
                    float stripe = step(0.5, frac((i.uv.x + i.uv.y) * 40.0));
                    fc = lerp(fc, float3(0,0,0), stripe * 0.8);
                }

                return half4(fc, _Opacity);
            }
            ENDHLSL
        }
    }
    FallBack Off
}

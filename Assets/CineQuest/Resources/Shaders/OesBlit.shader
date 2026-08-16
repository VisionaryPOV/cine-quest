// Cine Quest — Copy GL_TEXTURE_EXTERNAL_OES (or 2D) into an ARGB32 RT.
// One blit, then freeze/scopes/false-color/display all sample 2D.

Shader "CineQuest/OesBlit"
{
    Properties
    {
        [MainTexture] _MainTex ("Video", 2D) = "black" {}
        [Toggle] _FlipY ("Flip Y", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off
        Cull Off
        Pass
        {
            Name "OesBlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ CQ_EXTERNAL_OES
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #if defined(CQ_EXTERNAL_OES) && defined(SHADER_API_GLES3)
                #extension GL_OES_EGL_image_external_essl3 : require
                samplerExternalOES _MainTex;
            #else
                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
            #endif

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
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

            half4 frag(Varyings i) : SV_Target
            {
                #if defined(CQ_EXTERNAL_OES) && defined(SHADER_API_GLES3)
                    return tex2D(_MainTex, i.uv);
                #else
                    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                #endif
            }
            ENDHLSL
        }
    }
    FallBack Off
}

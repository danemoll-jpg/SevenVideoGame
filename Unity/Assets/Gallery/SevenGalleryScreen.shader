// Round 5: the gallery TV's screen surface. Unlit; shows up to two single-channel (grayscale, sRGB-encoded R8) images
// cross-fading, each fitted inside the screen at its own aspect ratio (_RectA/_RectB = covered fraction of the screen
// in x, y), on a near-black screen background. Dissolves with the ship reveal (_SevenDissolve, like Seven/DissolveLit).
// Strictly monochrome: output is always (v, v, v).
Shader "Seven/GalleryScreen"
{
    Properties
    {
        _TexA ("Image A", 2D) = "black" {}
        _TexB ("Image B", 2D) = "black" {}
        _RectA ("Rect A (x,y = covered fraction)", Vector) = (1,1,0,0)
        _RectB ("Rect B", Vector) = (1,1,0,0)
        _Blend ("A -> B", Range(0,1)) = 0
        _Bright ("Brightness", Float) = 1
        _Bg ("Screen background", Float) = 0.02
        _Probe ("Probe: image area solid white (check runs)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "GalleryScreen"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float GalToLinear(float c) { return c <= 0.04045 ? c / 12.92 : pow(max((c + 0.055) / 1.055, 0.0), 2.4); }
            TEXTURE2D(_TexA); SAMPLER(sampler_TexA);
            TEXTURE2D(_TexB); SAMPLER(sampler_TexB);
            CBUFFER_START(UnityPerMaterial)
            float4 _TexA_ST, _TexB_ST, _RectA, _RectB;
            float _Blend, _Bright, _Bg, _Probe;
            CBUFFER_END
            float4 _SevenDissolve;     // xyz = wave centre, w = radius (< 0: off)
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 wpos : TEXCOORD1; };
            V vert(A i)
            {
                V o;
                o.wpos = TransformObjectToWorld(i.pos.xyz);
                o.pos = TransformWorldToHClip(o.wpos);
                o.uv = i.uv;
                return o;
            }
            float Layer(TEXTURE2D_PARAM(tex, smp), float2 uv, float4 rect)
            {
                float2 iuv = (uv - 0.5) / max(rect.xy, 1e-4) + 0.5;
                float2 fw = max(fwidth(iuv), 1e-5);
                float2 edge = saturate(min(iuv, 1 - iuv) / fw + 0.5);   // 1-pixel antialiased image border
                float inside = edge.x * edge.y;
                float g = SAMPLE_TEXTURE2D(tex, smp, saturate(iuv)).r;
                g = _Probe > 0.5 ? 1.0 : GalToLinear(g);
                return lerp(_Bg, g, inside);
            }
            half4 frag(V i) : SV_Target
            {
                if (_SevenDissolve.w > 0 && distance(i.wpos, _SevenDissolve.xyz) < _SevenDissolve.w) discard;
                float a = Layer(TEXTURE2D_ARGS(_TexA, sampler_TexA), i.uv, _RectA);
                float b = Layer(TEXTURE2D_ARGS(_TexB, sampler_TexB), i.uv, _RectB);
                float v = lerp(a, b, _Blend) * _Bright;
                return half4(v, v, v, 1);
            }
            ENDHLSL
        }
    }
}

// QF13 (6): 7's bubble helmet that reads in the bright misty water as well as in the dark. Monochrome glass: a bright
// white rim (fresnel) round the whole dome, two soft highlights and a reflection streak (placed in view space, so they
// always face the camera like a real reflection of the light above), and a faint gray veil over what is behind the glass
// (the tint). _Pulse = the seal check (a soft white glow). Transparent, no depth write, front faces only.
Shader "Seven/Bubble"
{
    Properties
    {
        _Veil ("Veil (tint over what is behind)", Range(0, 0.5)) = 0.1
        _VeilGray ("Veil gray", Range(0, 1)) = 0.55
        _Rim ("Rim strength", Range(0, 3)) = 1.5
        _RimPower ("Rim power", Range(1, 8)) = 2.6
        _Highlight ("Highlights", Range(0, 2)) = 1.0
        _Pulse ("Seal pulse", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Pass
        {
            Name "SevenBubble"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Veil, _VeilGray, _Rim, _RimPower, _Highlight, _Pulse;
            CBUFFER_END
            struct A { float4 pos : POSITION; float3 n : NORMAL; };
            struct V { float4 pos : SV_POSITION; float3 nW : TEXCOORD0; float3 pW : TEXCOORD1; float3 nV : TEXCOORD2; };
            V vert(A i)
            {
                V o;
                o.pW = TransformObjectToWorld(i.pos.xyz);
                o.pos = TransformWorldToHClip(o.pW);
                o.nW = TransformObjectToWorldNormal(i.n);
                o.nV = TransformWorldToViewDir(o.nW, true);
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                float3 n = normalize(i.nW);
                float3 v = normalize(GetWorldSpaceViewDir(i.pW));
                float ndv = saturate(dot(n, v));
                float rim = pow(1.0 - ndv, _RimPower);
                float3 nv = normalize(i.nV);
                // two soft highlights upper-left / lower-right and a long curved streak (a window of light on the glass)
                float h1 = exp(-(pow(nv.x + 0.42, 2) + pow(nv.y - 0.5, 2)) / 0.012);
                float h2 = exp(-(pow(nv.x - 0.5, 2) + pow(nv.y + 0.38, 2)) / 0.006) * 0.55;
                float streakAxis = nv.x * 0.6 + nv.y * 0.8;
                float streak = exp(-pow(streakAxis - 0.72, 2) / 0.0025) * smoothstep(0.2, 0.6, ndv + 0.3) * 0.7;
                float hi = saturate((h1 + h2 + streak) * _Highlight);
                float glow = rim * _Rim;
                float a = saturate(_Veil + glow * 0.85 + hi * 0.9 + _Pulse * 0.35);
                float g = saturate((_Veil * _VeilGray + glow + hi * 1.6 + _Pulse * 0.8) / max(a, 1e-3));
                return half4(g, g, g, a);
            }
            ENDHLSL
        }
    }
}

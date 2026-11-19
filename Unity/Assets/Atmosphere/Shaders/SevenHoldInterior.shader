// QF13 (1): the inside of the saucer's bike hold. One flat, unlit gray on every surface (floor, ceiling, sides, back),
// both faces, no lighting, no reflections, no fresnel - so from outside no surface of the hold can be told from another:
// the open hatch reads as an empty opening with only the bike in it (QF11's black bands, QF12's white bands were the hold's
// lit floor and ceiling seen edge-on). Monochrome: _Gray is a single value.
Shader "Seven/HoldInterior"
{
    Properties
    {
        _Gray ("Gray", Range(0, 1)) = 0.4
        [HideInInspector] _EmissionColor ("Unused (old runtime fades)", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite On
        Pass
        {
            Name "SevenHoldInterior"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Gray;
            float4 _EmissionColor;
            CBUFFER_END
            struct A { float4 pos : POSITION; };
            struct V { float4 pos : SV_POSITION; };
            V vert(A i) { V o; o.pos = TransformObjectToHClip(i.pos.xyz); return o; }
            half4 frag(V i) : SV_Target { return half4(_Gray, _Gray, _Gray, 1); }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Gray;
            float4 _EmissionColor;
            CBUFFER_END
            struct A { float4 pos : POSITION; };
            struct V { float4 pos : SV_POSITION; };
            V vert(A i) { V o; o.pos = TransformObjectToHClip(i.pos.xyz); return o; }
            half4 frag(V i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}

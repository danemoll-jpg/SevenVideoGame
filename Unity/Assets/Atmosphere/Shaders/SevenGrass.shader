// QF10: Venus grass (Kyle's "grassy rise in the fog"). Baked blade meshes (FightDemoBuilderGrass.cs): uv.x = across the
// blade (0..1), uv.y = height along the blade (0 root .. 1 tip), color.r = random phase, color.g = brightness variation,
// color.b = blade height (m, / 2). The vertex shader sways each blade in a soft wind (stronger toward the tip, slow gusts
// rolling across the field); the fragment is grayscale only: dark at the root, catching the light toward the tip, with a
// little back-light translucency so the tips glow against the bright mist. Receives the main light's shadows, casts
// none. Depth passes sway the same way, so Kyle's height fog (a full-screen pass on the depth) fades it into the haze.
Shader "Seven/Grass"
{
    Properties
    {
        _Root ("Root gray", Range(0,1)) = 0.16
        _Tip ("Tip gray", Range(0,1)) = 0.62
        _Translucency ("Back-light glow", Range(0,2)) = 0.55
        _WindStrength ("Wind (m at the tip)", Range(0,0.6)) = 0.16
        _WindSpeed ("Wind speed", Range(0,4)) = 1.1
        _GustScale ("Gust scale (1/m)", Range(0.001,0.2)) = 0.045
        _WindDir ("Wind direction (xz)", Vector) = (0.8, 0, 0.6, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
        half _Root, _Tip, _Translucency, _WindStrength, _WindSpeed, _GustScale;
        float4 _WindDir;
        CBUFFER_END

        float3 Sway(float3 ws, float h, float phase, float bladeH)
        {
            float2 wd = normalize(_WindDir.xz + 1e-4);
            float t = _Time.y * _WindSpeed;
            // a slow gust rolling across the field + each blade's own flutter
            float gust = 0.55 + 0.45 * sin(dot(ws.xz, wd) * _GustScale * 6.2831 - t * 0.9);
            float flutter = sin(t * 2.3 + phase * 6.2831) * 0.35 + sin(t * 3.7 + phase * 11.0) * 0.15;
            float bend = (gust + flutter) * _WindStrength * h * h * saturate(bladeH * 1.6);
            float3 o = float3(wd.x, 0, wd.y) * bend;
            o.y = -0.35 * bend * h;                // the tip dips a little as it bends
            return ws + o;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; half var : TEXCOORD3; };

            V Vert(A i)
            {
                V o; UNITY_SETUP_INSTANCE_ID(i);
                float3 ws = TransformObjectToWorld(i.positionOS.xyz);
                ws = Sway(ws, i.uv.y, i.color.r, i.color.b * 2.0);
                o.positionWS = ws;
                o.positionCS = TransformWorldToHClip(ws);
                // soft normals: mostly up (a field reads as one surface), a little of the blade's own facing
                o.normalWS = normalize(lerp(float3(0, 1, 0), TransformObjectToWorldNormal(i.normalOS), 0.35));
                o.uv = i.uv; o.var = i.color.g;
                return o;
            }

            half4 Frag(V i) : SV_Target
            {
                float h = i.uv.y;
                half g = lerp(_Root, _Tip, h * h * (3.0 - 2.0 * h)) * (0.8 + 0.4 * i.var);
                Light L = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float3 n = normalize(i.normalWS);
                float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
                half lam = saturate(dot(n, L.direction)) * 0.75 + 0.25;
                half sh = lerp(1.0, L.shadowAttenuation, 0.85);
                half back = pow(saturate(dot(-v, L.direction)), 3.0) * _Translucency * h;
                half3 amb = SampleSH(n);
                half lum = dot(L.color, half3(0.2126, 0.7152, 0.0722));
                half ambl = dot(amb, half3(0.2126, 0.7152, 0.0722));
                half c = g * (ambl + lum * lam * sh) + back * lum * sh * 0.6;
                return half4(c, c, c, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; };
            V Vert(A i) { V o; UNITY_SETUP_INSTANCE_ID(i); float3 ws = Sway(TransformObjectToWorld(i.positionOS.xyz), i.uv.y, i.color.r, i.color.b * 2.0); o.positionCS = TransformWorldToHClip(ws); return o; }
            half Frag(V i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; };
            V Vert(A i) { V o; UNITY_SETUP_INSTANCE_ID(i); float3 ws = Sway(TransformObjectToWorld(i.positionOS.xyz), i.uv.y, i.color.r, i.color.b * 2.0); o.positionCS = TransformWorldToHClip(ws); return o; }
            half4 Frag(V i) : SV_Target { return half4(0, 1, 0, 0); }
            ENDHLSL
        }
    }
}

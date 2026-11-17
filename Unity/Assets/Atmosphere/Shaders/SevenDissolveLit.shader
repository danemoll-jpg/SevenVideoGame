// URP lit surface that can dissolve away in a wave spreading from a point (Round 3: the saucer "reveal").
// Strictly monochrome: albedo / emission are grayscale; the dissolve edge glows white.
//   Global  _SevenDissolve      xyz = wave centre (world), w = radius (m). Everything with _Dissolvable = 1 closer than
//                               the radius is gone. w < 0 -> nothing dissolved.
//   Global  _SevenDissolveEdge  x = width of the glowing edge band (m), y = edge glow intensity, z = noise scale (1/m),
//                               w = noise amplitude (m) (breaks the edge up so it is not a perfect sphere).
//   _Rim / _RimPower            soft white rim (used by the black cat so its shape reads against dark carpet).
Shader "Seven/DissolveLit"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,1)
        _Rim ("Rim", Range(0,2)) = 0
        _RimPower ("Rim Power", Range(0.5,8)) = 3
        _Dissolvable ("Dissolvable", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half4 _BaseColor;
        half _Smoothness, _Metallic;
        half4 _EmissionColor;
        half _Rim, _RimPower, _Dissolvable, _Cull;
        CBUFFER_END
        float4 _SevenDissolve;
        float4 _SevenDissolveEdge;

        float Hash3(float3 p)
        {
            p = frac(p * 0.3183099 + 0.1);
            p *= 17.0;
            return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
        }
        float Noise3(float3 x)
        {
            float3 i = floor(x), f = frac(x);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(lerp(Hash3(i), Hash3(i + float3(1,0,0)), f.x), lerp(Hash3(i + float3(0,1,0)), Hash3(i + float3(1,1,0)), f.x), f.y),
                        lerp(lerp(Hash3(i + float3(0,0,1)), Hash3(i + float3(1,0,1)), f.x), lerp(Hash3(i + float3(0,1,1)), Hash3(i + float3(1,1,1)), f.x), f.y), f.z);
        }
        // Returns edge glow 0..1 and clips dissolved pixels.
        half DissolveClip(float3 positionWS)
        {
            if (_Dissolvable < 0.5 || _SevenDissolve.w < 0.0) return 0;
            float d = distance(positionWS, _SevenDissolve.xyz);
            float n = Noise3(positionWS * _SevenDissolveEdge.z) * 0.65 + Noise3(positionWS * _SevenDissolveEdge.z * 3.1) * 0.35;
            d += (n - 0.5) * _SevenDissolveEdge.w;
            float k = d - _SevenDissolve.w;
            clip(k);
            return (half)saturate(1.0 - k / max(_SevenDissolveEdge.x, 1e-3));
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID };

            V Vert(A i)
            {
                V o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                return o;
            }

            half4 Frag(V i, bool front : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half edge = DissolveClip(i.positionWS);
                float3 n = normalize(i.normalWS);
                if (!front) n = -n;    // two-sided surfaces (Cull Off) light correctly from both sides

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                half g = dot(tex.rgb * _BaseColor.rgb, half3(0.2126, 0.7152, 0.0722));   // grayscale only

                InputData d = (InputData)0;
                d.positionWS = i.positionWS;
                d.positionCS = i.positionCS;
                d.normalWS = n;
                d.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                d.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                d.bakedGI = SampleSH(n);
                d.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                d.shadowMask = half4(1, 1, 1, 1);

                SurfaceData s = (SurfaceData)0;
                s.albedo = g.xxx;
                s.metallic = _Metallic;
                s.smoothness = _Smoothness;
                s.occlusion = 1;
                s.alpha = 1;
                s.normalTS = half3(0, 0, 1);
                half rim = _Rim * pow(1.0 - saturate(dot(n, d.viewDirectionWS)), _RimPower);
                half em = dot(_EmissionColor.rgb, half3(0.2126, 0.7152, 0.0722));
                s.emission = (em + rim + edge * edge * _SevenDissolveEdge.y).xxx;

                half4 c = UniversalFragmentPBR(d, s);
                half l = dot(c.rgb, half3(0.2126, 0.7152, 0.0722));
                return half4(l, l, l, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };
            V Vert(A i)
            {
                V o;
                UNITY_SETUP_INSTANCE_ID(i);
                float3 p = TransformObjectToWorld(i.positionOS.xyz);
                float3 nrm = TransformObjectToWorldNormal(i.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 L = normalize(_LightPosition - p);
                #else
                float3 L = _LightDirection;
                #endif
                float4 cs = TransformWorldToHClip(ApplyShadowBias(p, nrm, L));
                #if UNITY_REVERSED_Z
                cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = cs;
                o.positionWS = p;
                return o;
            }
            half4 Frag(V i) : SV_Target { DissolveClip(i.positionWS); return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };
            V Vert(A i) { V o; UNITY_SETUP_INSTANCE_ID(i); o.positionWS = TransformObjectToWorld(i.positionOS.xyz); o.positionCS = TransformWorldToHClip(o.positionWS); return o; }
            half Frag(V i) : SV_Target { DissolveClip(i.positionWS); return i.positionCS.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; };
            V Vert(A i) { V o; UNITY_SETUP_INSTANCE_ID(i); o.positionWS = TransformObjectToWorld(i.positionOS.xyz); o.positionCS = TransformWorldToHClip(o.positionWS); o.normalWS = TransformObjectToWorldNormal(i.normalOS); return o; }
            half4 Frag(V i) : SV_Target { DissolveClip(i.positionWS); return half4(NormalizeNormalPerPixel(i.normalWS), 0); }
            ENDHLSL
        }
    }
}

// Round 5: full-view gallery image (uGUI RawImage). The texture is single-channel grayscale (sRGB-encoded R8);
// output is (v, v, v) * vertex alpha, so the image stays strictly monochrome and fades with the RawImage colour alpha.
Shader "Seven/GalleryUI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float GalToLinear(float c) { return c <= 0.04045 ? c / 12.92 : pow(max((c + 0.055) / 1.055, 0.0), 2.4); }
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            half4 _Color;
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            V vert(A i)
            {
                V o;
                o.pos = TransformObjectToHClip(i.pos.xyz);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.color = i.color * _Color;
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                half g = GalToLinear(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).r);
                half lum = dot(i.color.rgb, half3(0.2126, 0.7152, 0.0722));
                half v = g * lum;
                return half4(v, v, v, i.color.a);
            }
            ENDHLSL
        }
    }
}

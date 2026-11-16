// Full-screen height + distance fog with in-fog glow sources, strictly grayscale.
// Driven by globals that the Atmosphere component sets every frame (_SevenFogOn = 0 -> pass-through).
// Runs as a URP Full Screen Pass renderer feature before transparents (effects and UI stay crisp on top).
Shader "Seven/HeightFog"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "SevenHeightFog"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float _SevenFogOn;
            float4 _SevenFogGround;   // x density/m at base, y base height, z falloff (m), w range (m) from the camera it extends
            float4 _SevenFogValley;   // x density/m at base, y base height, z falloff (m), w unused
            float4 _SevenFogDist;     // x distance density/m, y max fog, z sky distance (m), w haze height (m; distance haze thins above this)
            float4 _SevenFogTone;     // x near brightness, y far brightness, z distance (m) to reach far, w zenith brightness
            float4 _SevenFogNoise;    // x strength, y world scale, z speed, w fade distance
            float _SevenFogStart;     // metres from the camera before any fog accumulates (keeps the foreground dark)
            float4 _SevenGlowPos[8];  // xyz position, w radius
            float4 _SevenGlowInt[8];  // x intensity
            float _SevenGlowCount;
            TEXTURE2D(_SevenNoiseTex); SAMPLER(sampler_SevenNoiseTex);

            // Integral of d0 * exp(-(y - base)/falloff) along a ray of length dist from height y0 with vertical slope dy.
            float LayerIntegral(float4 layer, float y0, float dy, float dist)
            {
                float k = 1.0 / max(layer.z, 1e-3);
                float a = clamp(-k * (y0 - layer.y), -60.0, 60.0);
                float h = k * dy * dist;                       // total exponent change along the ray
                float f = abs(h) > 1e-4 ? (1.0 - exp(clamp(-h, -60.0, 60.0))) / h : 1.0;
                return layer.x * exp(a) * dist * f;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                if (_SevenFogOn < 0.5) return col;

                float depth = SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
                #endif
                float3 wp = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float3 cam = _WorldSpaceCameraPos;
                float3 ray = wp - cam;
                float dist = length(ray);
                float3 dir = ray / max(dist, 1e-4);
                #if UNITY_REVERSED_Z
                bool sky = depth <= 1e-6;
                #else
                bool sky = depth >= 1.0 - 1e-6;
                #endif
                if (sky) dist = _SevenFogDist.z;
                float fullDist = dist;
                float start = min(_SevenFogStart, dist);
                float3 o = cam + dir * start;          // fog starts accumulating here
                dist -= start;

                float tau = LayerIntegral(_SevenFogGround, o.y, dir.y, min(dist, _SevenFogGround.w));
                // Soft drifting variation in the ground layer (fades out with distance).
                if (_SevenFogNoise.x > 0)
                {
                    float2 p = (o.xz + dir.xz * min(dist, _SevenFogNoise.w)) * _SevenFogNoise.y;
                    float t = _Time.y * _SevenFogNoise.z;
                    float n = SAMPLE_TEXTURE2D(_SevenNoiseTex, sampler_SevenNoiseTex, p + float2(t, t * 0.37)).r
                            * SAMPLE_TEXTURE2D(_SevenNoiseTex, sampler_SevenNoiseTex, p * 0.43 - float2(t * 0.21, t * 0.5)).r * 2.0;
                    tau *= lerp(1.0, saturate(n) * 2.0, _SevenFogNoise.x * saturate(1.0 - dist / (_SevenFogNoise.w * 4.0)));
                }
                tau += LayerIntegral(_SevenFogValley, o.y, dir.y, dist);
                // Distance haze, thinner for rays that travel high up (keeps the overhead mass dark).
                float midY = o.y + dir.y * dist * 0.5;
                tau += _SevenFogDist.x * dist * exp(-max(midY, 0.0) / max(_SevenFogDist.w, 1.0));
                float fog = min(1.0 - exp(-tau), _SevenFogDist.y);

                // Fog brightness: brighter with distance, dimmer looking up.
                float tone = lerp(_SevenFogTone.x, _SevenFogTone.y, saturate(fullDist / _SevenFogTone.z));
                tone = lerp(tone, _SevenFogTone.w, saturate(dir.y * 3.0));

                // Glow sources seen through the fog: closest approach of the view ray to each source.
                float glow = 0;
                int count = (int)_SevenGlowCount;
                for (int i = 0; i < 8; i++)
                {
                    if (i >= count) break;
                    float3 L = _SevenGlowPos[i].xyz - cam;
                    float t = clamp(dot(L, dir), 0.0, fullDist);
                    float d = length(L - dir * t) / _SevenGlowPos[i].w;
                    glow += _SevenGlowInt[i].x * (exp(-d * d) + 0.12 / (1.0 + d * d));
                }

                float3 scene = col.rgb;
                float3 outc = lerp(scene, (tone + glow).xxx, fog);
                return half4(outc, col.a);
            }
            ENDHLSL
        }
    }
}

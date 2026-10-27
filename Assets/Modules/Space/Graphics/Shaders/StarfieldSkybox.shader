// Procedural skybox for Last Fleet: three star layers, a galactic band, noise-based nebulae and a sun
// whose direction is set globally by SpaceEnvironment (_LF_SunDirection). Everything derives from
// _Seed so each star system can get its own sky.
Shader "LastFleet/StarfieldSkybox"
{
    Properties
    {
        _Seed ("Seed", Float) = 1

        [Header(Space)]
        _SpaceColor ("Space Color", Color) = (0.002, 0.003, 0.006, 1)

        [Header(Stars)]
        _StarDensity ("Star Density", Range(0, 1)) = 0.06
        _StarBrightness ("Star Brightness", Float) = 4
        _StarSize ("Star Size", Range(0.02, 0.25)) = 0.12
        _StarScale ("Star Scale", Float) = 180

        [Header(Galactic Band)]
        _BandNormal ("Band Normal", Vector) = (0.2, 0.9, 0.35, 0)
        _BandWidth ("Band Width", Range(0.01, 1)) = 0.25
        _BandStrength ("Band Strength", Range(0, 4)) = 1.5

        [Header(Nebula)]
        [HDR] _NebulaColorA ("Nebula Color A", Color) = (0.12, 0.03, 0.2, 1)
        [HDR] _NebulaColorB ("Nebula Color B", Color) = (0.02, 0.09, 0.16, 1)
        _NebulaStrength ("Nebula Strength", Range(0, 2)) = 0.5
        _NebulaScale ("Nebula Scale", Float) = 2.2

        [Header(Sun)]
        [HDR] _SunColor ("Sun Color", Color) = (8, 7, 5.5, 1)
        _SunSize ("Sun Size (Radians)", Range(0.001, 0.1)) = 0.012
        _SunGlow ("Sun Glow", Range(0, 2)) = 0.6
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Seed;
                float4 _SpaceColor;
                float _StarDensity;
                float _StarBrightness;
                float _StarSize;
                float _StarScale;
                float4 _BandNormal;
                float _BandWidth;
                float _BandStrength;
                float4 _NebulaColorA;
                float4 _NebulaColorB;
                float _NebulaStrength;
                float _NebulaScale;
                float4 _SunColor;
                float _SunSize;
                float _SunGlow;
            CBUFFER_END

            // Global, set by SpaceEnvironment. Points towards the sun; (0, 0, 0) means no sun.
            float4 _LF_SunDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            float3 Hash33(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.xxy + p.yxx) * p.zyx);
            }

            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash13(i);
                float b = Hash13(i + float3(1, 0, 0));
                float c = Hash13(i + float3(0, 1, 0));
                float d = Hash13(i + float3(1, 1, 0));
                float e = Hash13(i + float3(0, 0, 1));
                float g = Hash13(i + float3(1, 0, 1));
                float h = Hash13(i + float3(0, 1, 1));
                float k = Hash13(i + float3(1, 1, 1));

                return lerp(lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y),
                            lerp(lerp(e, g, f.x), lerp(h, k, f.x), f.y), f.z);
            }

            float Fbm(float3 p)
            {
                float sum = 0.0;
                float amplitude = 0.5;
                for (int octave = 0; octave < 5; octave++)
                {
                    sum += amplitude * ValueNoise(p);
                    p = p * 2.03 + 17.1;
                    amplitude *= 0.5;
                }
                return sum;
            }

            // One star per cell of a 3D grid with random position, size, brightness and tint.
            // pixelSize is the cell size of one screen pixel. Stars smaller than a pixel are widened and
            // dimmed by the same energy, so they do not flicker while the camera rotates.
            float3 StarLayer(float3 p, float pixelSize, float density, float seed)
            {
                float3 cell = floor(p);
                float3 h = Hash33(cell + seed);
                if (h.x > density) return 0;

                float3 h2 = Hash33(cell + seed + 71.7);
                float3 center = cell + 0.3 + 0.4 * h2;
                float dist = length(p - center);

                float baseRadius = _StarSize * (0.5 + h.y);
                float radius = max(baseRadius, pixelSize * 0.6);
                float energy = (baseRadius * baseRadius) / (radius * radius);

                float brightness = (pow(h.z, 6.0) * 6.0 + 0.15) * energy;
                float3 tint = lerp(float3(0.7, 0.8, 1.0), float3(1.0, 1.0, 1.0), saturate(h2.x * 3.0));
                tint = lerp(tint, float3(1.0, 0.8, 0.6), saturate(h2.x * 5.0 - 4.0));

                return tint * brightness * exp(-dist * dist / (radius * radius));
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.direction);
                float seed = _Seed * 17.13;

                float band = exp(-pow(dot(dir, normalize(_BandNormal.xyz)) / _BandWidth, 2.0));
                float density = saturate(_StarDensity * (0.4 + band * _BandStrength));

                // Three layers: few large, some medium and many fine stars.
                float3 p0 = dir * _StarScale;
                float3 p1 = dir * _StarScale * 2.0;
                float3 p2 = dir * _StarScale * 4.0;
                float3 stars = StarLayer(p0, length(fwidth(p0)), density * 0.5, seed) * 1.0
                             + StarLayer(p1, length(fwidth(p1)), density * 0.7, seed + 11.0) * 0.6
                             + StarLayer(p2, length(fwidth(p2)), density, seed + 23.0) * 0.35;
                stars *= _StarBrightness;

                float3 nebulaPos = dir * _NebulaScale + seed;
                float shape = Fbm(nebulaPos);
                float hue = Fbm(nebulaPos * 1.7 + 41.3);
                float cloud = pow(saturate(shape * 1.6 - 0.45), 2.0);
                float3 nebula = lerp(_NebulaColorA.rgb, _NebulaColorB.rgb, hue) * cloud * _NebulaStrength * (0.35 + band);

                float3 color = _SpaceColor.rgb + nebula;

                float3 sunDir = _LF_SunDirection.xyz;
                if (dot(sunDir, sunDir) > 0.5)
                {
                    float c = dot(dir, sunDir);
                    float disc = smoothstep(cos(_SunSize), cos(_SunSize * 0.85), c);
                    float glow = exp((c - 1.0) * 400.0) * 0.6 + exp((c - 1.0) * 40.0) * 0.15;
                    stars *= 1.0 - saturate(glow * 4.0);
                    color += _SunColor.rgb * (disc + glow * _SunGlow);
                }

                return half4(color + stars, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

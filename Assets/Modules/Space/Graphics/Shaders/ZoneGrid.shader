// Tactical grid of a zone: fine lines per klick, strong lines every 10 klicks and a rim at the zone
// radius. Line width is in screen pixels; lines that become too dense fade out when zooming out.
// The center is the object origin, the radius is set by SpaceZone.
Shader "LastFleet/ZoneGrid"
{
    Properties
    {
        [HDR] _MinorColor ("Minor Line Color", Color) = (0.25, 0.45, 0.6, 0.12)
        [HDR] _MajorColor ("Major Line Color", Color) = (0.35, 0.6, 0.85, 0.3)
        [HDR] _RimColor ("Zone Rim Color", Color) = (0.4, 0.7, 1.0, 0.6)
        _MinorSpacing ("Minor Spacing (Units)", Float) = 10
        _MajorSpacing ("Major Spacing (Units)", Float) = 100
        _LineWidth ("Line Width (Pixels)", Float) = 1.2
        _ZoneRadius ("Zone Radius (Units)", Float) = 500
        _EdgeFade ("Edge Fade", Range(0.01, 1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ZoneGrid"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MinorColor;
                float4 _MajorColor;
                float4 _RimColor;
                float _MinorSpacing;
                float _MajorSpacing;
                float _LineWidth;
                float _ZoneRadius;
                float _EdgeFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            // Returns the line mask; fade tells whether the cells are large enough on screen to draw.
            float GridLines(float2 coord, float widthPixels, out float fade)
            {
                float2 w = max(fwidth(coord), 1e-5);
                float2 g = abs(frac(coord - 0.5) - 0.5) / w;
                float cellPixels = 1.0 / max(w.x, w.y);
                fade = saturate((cellPixels - 4.0) / 8.0);
                return 1.0 - saturate(min(g.x, g.y) / widthPixels);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 center = TransformObjectToWorld(float3(0, 0, 0));
                float2 p = input.positionWS.xz - center.xz;

                float minorFade, majorFade;
                float minor = GridLines(p / _MinorSpacing, _LineWidth, minorFade) * minorFade;
                float major = GridLines(p / _MajorSpacing, _LineWidth * 1.3, majorFade) * majorFade;

                float d = length(p);
                float dw = max(fwidth(d), 1e-5);
                float rim = 1.0 - saturate(abs(d - _ZoneRadius) / dw / (_LineWidth * 1.5));
                float inside = 1.0 - smoothstep(_ZoneRadius * (1.0 - _EdgeFade), _ZoneRadius, d);

                float3 color = (_MinorColor.rgb * _MinorColor.a * minor + _MajorColor.rgb * _MajorColor.a * major) * inside
                             + _RimColor.rgb * _RimColor.a * rim;

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

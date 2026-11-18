Shader "PhewaValley/Lake"
{
    Properties
    {
        _Color ("Lake colour", Color) = (0.025, 0.14, 0.17, 1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.92
        _Metallic ("Reflectivity", Range(0,1)) = 0.2
        _WaveScale ("Ripple scale", Float) = 0.7
        _WaveStrength ("Ripple strength", Range(0,0.3)) = 0.08
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        fixed4 _Color;
        half _Glossiness, _Metallic, _WaveScale, _WaveStrength;
        float _PhewaWet;
        struct Input { float3 worldPos; float3 viewDir; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // World-space capillary ripples: consistent scale across the entire lake.
            float2 p = IN.worldPos.xz * _WaveScale;
            float t = _Time.y;
            float2 slope = float2(cos(p.x + p.y * .65 + t * 1.1),
                                  sin(p.y * 1.3 - p.x * .4 - t * .85));
            slope += .35 * float2(sin(p.y * 3.1 + t * 1.9), cos(p.x * 2.7 - t * 1.6));
            o.Normal = normalize(float3(slope * _WaveStrength * (1 + _PhewaWet), 1));
            float fresnel = pow(1 - saturate(dot(normalize(IN.viewDir), o.Normal)), 4);
            o.Albedo = lerp(_Color.rgb, _Color.rgb * 1.5, fresnel);
            o.Smoothness = _Glossiness - .08 * _PhewaWet;
            o.Metallic = _Metallic;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}

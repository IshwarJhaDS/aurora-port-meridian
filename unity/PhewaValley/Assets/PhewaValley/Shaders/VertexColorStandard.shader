// Standard PBR lighting driven by vertex data from MeshBuilder:
// COLOR = albedo, TEXCOORD1 = (roughness, metallic, emission, battery).
// Negative emission glows only at night (windows); during load shedding only a
// few candle-lit windows and battery-powered lights (vehicles) keep glowing.
// Procedural world-space detail adds realism without UVs: brick bonding on brick
// walls, plaster/concrete grain, grime near the ground, rain streaks, asphalt
// texture and wet, reflective roads in the rain. Detail fades with distance.
Shader "PhewaValley/VertexColorStandard"
{
    Properties { _Color ("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.5
        struct Input { float4 vcolor; float4 surface; float3 worldPos; float3 worldNormal; };
        fixed4 _Color;
        float _PhewaNight; float _PhewaPower; float _PhewaWet;

        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.vcolor = v.color;
            o.surface = v.texcoord1;
        }

        // Cheap 2D value noise, evaluated on each surface's own plane.
        float hash2(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
        float vnoise(float2 p)
        {
            float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
            return lerp(lerp(hash2(i), hash2(i + float2(1, 0)), f.x), lerp(hash2(i + float2(0, 1)), hash2(i + 1), f.x), f.y);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 albedo = IN.vcolor.rgb * _Color.rgb, wp = IN.worldPos, n = normalize(IN.worldNormal);
            float rough = saturate(IN.surface.x), metal = saturate(IN.surface.y), e = IN.surface.z;
            float dist = distance(_WorldSpaceCameraPos, wp), near = saturate(1 - dist / 120);
            float wall = saturate((1 - abs(n.y)) * 1.5 - .25), floorUp = saturate(n.y * 4 - 3);

            [branch] if (e == 0 && metal < .5 && near > 0)
            {
                float along = abs(n.x) > abs(n.z) ? wp.z : wp.x;
                float2 uv = wall > .5 ? float2(along, wp.y) : wp.xz;
                // Weathering and grain on every surface.
                float grain = vnoise(uv * .4) * .55 + vnoise(uv * 3.3) * .45;
                albedo *= lerp(1, .84 + .3 * grain, near);
                [branch] if (wall > .5)
                {
                    // Grime splashed up the bottom of walls, rain streaks running down.
                    albedo *= lerp(1, .74 + .26 * saturate(wp.y / 1.4), near);
                    albedo *= 1 - smoothstep(.6, .92, vnoise(float2(along * 2.3, wp.y * .22))) * .2 * near;
                    // Exposed brick (warm, saturated walls): staggered courses and pale mortar.
                    float brickish = saturate((albedo.r - albedo.g * 1.65) * 8), close = saturate(1 - dist / 45);
                    [branch] if (brickish * close > .01)
                    {
                        float row = floor(wp.y / .16), u = along / .42 + row * .5, v = frac(wp.y / .16);
                        float mortar = saturate(step(v, .14) + step(frac(u), .07));
                        albedo = lerp(albedo, albedo * (.85 + .3 * hash2(float2(floor(u), row))), brickish * close);
                        albedo = lerp(albedo, float3(.52, .48, .42), mortar * brickish * close * .8);
                        rough = lerp(rough, .95, brickish);
                    }
                }
                else
                {
                    // Asphalt: dark flat ground gets aggregate speckle, tar patches and monsoon puddles.
                    float lum = dot(albedo, float3(.3, .59, .11)), asphalt = floorUp * saturate((.14 - lum) * 12);
                    [branch] if (asphalt > .01)
                    {
                        float patch = smoothstep(.62, .7, vnoise(wp.xz * .18));
                        albedo *= lerp(1, (.8 + .45 * vnoise(wp.xz * 9)) * (1 - patch * .3), asphalt * near);
                        float wet = _PhewaWet * (.55 + .45 * smoothstep(.45, .65, vnoise(wp.xz * .25)));
                        albedo *= 1 - wet * .35; rough = lerp(rough, .05, wet * asphalt);
                    }
                }
                rough = lerp(rough, rough * .6, _PhewaWet * (floorUp * .7 + wall * .2));
            }

            o.Albedo = albedo;
            o.Metallic = metal;
            o.Smoothness = saturate(1.0 - rough);
            // Load shedding: grid lights go dark, a few windows keep a candle burning.
            float candle = step(.82, frac(sin(dot(floor(wp.xz * .7 + wp.y * 1.3), float2(12.9898, 78.233))) * 43758.5453)) * .3;
            float power = max(_PhewaPower, IN.surface.w);
            float glow = e < 0 ? -e * _PhewaNight * lerp(candle, 1, power) : e;
            float3 tint = e < 0 ? lerp(albedo, float3(1.0, 0.55, 0.19), 0.85) : albedo;
            o.Emission = tint * glow;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

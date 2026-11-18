using UnityEngine;

namespace PhewaValley
{
    /// <summary>Continuous, deterministic ridges around the playable valley.</summary>
    public static class ValleyBackdrop
    {
        public static float Height(float angle, float across, int layer)
        {
            float x = Mathf.Sin(angle), z = Mathf.Cos(angle);
            float broad = Mathf.PerlinNoise(x * 2.7f + 17 + layer * 11, z * 2.7f + 31);
            float detail = Mathf.PerlinNoise(x * 10 + 63, z * 10 + 19 + layer * 7);
            float north = Mathf.Exp(-Mathf.Pow(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, 178) / 37, 2));
            float east = Mathf.Exp(-Mathf.Pow(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, 96) / 32, 2));
            float crest = layer == 0 ? 70 + 115 * broad + 30 * detail
                : 160 + 210 * broad + 90 * detail + (layer == 1 ? 270 : 350) * north + 150 * east;
            // A wandering crest and subsidiary spurs avoid repeated cones and level snow bands.
            float ridge = .48f + .12f * Mathf.Sin(angle * 7 + layer);
            float side = across < ridge ? across / ridge : (1 - across) / (1 - ridge);
            float profile = Mathf.Pow(Mathf.Clamp01(side), .8f);
            float ribs = Mathf.PerlinNoise(x * 18 + across * 4 + 51, z * 18 + layer * 13 + 29);
            return -3 + crest * profile * (.82f + .3f * ribs);
        }

        public static void Build(MeshBuilder mesh)
        {
            const int segments = 160, bands = 10;
            for (int layer = 2; layer >= 0; layer--)
            {
                float inner = layer == 0 ? 850 : layer == 1 ? 1230 : 1700;
                float width = layer == 0 ? 540 : layer == 1 ? 740 : 680;
                Vector3 Point(int i, int j)
                {
                    // Use the same exact angle at the seam.
                    float angle = (i % segments) * Mathf.PI * 2 / segments, t = (float)j / bands;
                    float radius = inner + width * t;
                    return new Vector3(Mathf.Sin(angle) * radius, Height(angle, t, layer), Mathf.Cos(angle) * radius * .95f);
                }
                for (int i = 0; i < segments; i++) for (int j = 0; j < bands; j++)
                {
                    var a = Point(i, j); var b = Point(i + 1, j); var c = Point(i + 1, j + 1); var d = Point(i, j + 1);
                    float height = (a.y + b.y + c.y + d.y) * .25f;
                    float snowLine = 430 + 60 * Mathf.Sin(i * .17f + layer);
                    var forest = Color.Lerp(new Color(.12f, .2f, .14f), new Color(.23f, .3f, .25f), Mathf.Clamp01(height / 210));
                    var rock = new Color(.33f, .37f, .4f);
                    var color = Color.Lerp(forest, rock, Mathf.InverseLerp(170, 340, height));
                    color = Color.Lerp(color, new Color(.8f, .85f, .88f), Mathf.InverseLerp(snowLine, snowLine + 90, height));
                    color = Color.Lerp(color, new Color(.4f, .48f, .55f), layer * .14f);
                    var centre = (a + b + c + d) * .25f;
                    mesh.Facing(new Vector3(centre.x, -100, centre.z), a, b, c, d, color, new Surface(.95f));
                }
            }
        }
    }
}

// HDR tone mapping with restrained color grading, bloom and ambient occlusion.
// Clear and Natural preserve material colors. Monochrome retains its film grain.
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace PhewaValley
{
    public class CinematicLook : MonoBehaviour
    {
        PostProcessVolume volume; ColorGrading grading; Bloom bloom; Vignette vignette; AmbientOcclusion ao; Grain grain; ChromaticAberration fringe;
        PostProcessLayer layer; Camera cam; ReflectionProbe probe; float probeTimer, probeEvery = 6; int quality = 1;
        public static readonly string[] QualityNames = { "Performance", "Balanced", "Cinematic" };
        public static readonly string[] FilmNames = { "Kung Fu Cinema", "Natural", "Kurosawa B&W" };
        /// <summary>0 = Clear Â· 1 = Natural Â· 2 = Kurosawa black and white.</summary>
        public int film;

        public void Init(Camera cam, int quality = 1)
        {
            this.cam = cam; QualitySettings.shadowProjection = ShadowProjection.StableFit; QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable; QualitySettings.lodBias = 2; cam.allowHDR = true;

            // Reflections of the current sky for metal, glass and wet roads.
            probe = new GameObject("Sky Reflections").AddComponent<ReflectionProbe>(); probe.transform.SetParent(transform, false);
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime; probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            probe.cullingMask = 0; probe.size = new Vector3(5000, 2000, 5000); probe.resolution = 128; probe.importance = 0; probe.RenderProbe();
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;

            var holder = Resources.Load<PostFXResources>("PhewaPostFX");
            if (holder == null || holder.resources == null) { Apply(quality); Debug.LogWarning("[graphics] Post-processing resources missing: use Phewa Valley â–¸ Repair project setup."); return; }
            layer = cam.gameObject.AddComponent<PostProcessLayer>(); layer.Init(holder.resources);
            layer.volumeLayer = 1 << 0; layer.volumeTrigger = cam.transform; layer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;

            var profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            grading = profile.AddSettings<ColorGrading>(); grading.enabled.Override(true);
            grading.gradingMode.Override(GradingMode.HighDefinitionRange); grading.tonemapper.Override(Tonemapper.ACES);
            bloom = profile.AddSettings<Bloom>(); bloom.enabled.Override(true); bloom.intensity.Override(.25f); bloom.threshold.Override(1.25f); bloom.softKnee.Override(.6f); bloom.diffusion.Override(7);
            ao = profile.AddSettings<AmbientOcclusion>(); ao.enabled.Override(true); ao.mode.Override(AmbientOcclusionMode.MultiScaleVolumetricObscurance); ao.intensity.Override(.75f); ao.thicknessModifier.Override(1.4f);
            vignette = profile.AddSettings<Vignette>(); vignette.enabled.Override(true); vignette.intensity.Override(.08f); vignette.smoothness.Override(.45f); vignette.roundness.Override(.85f);
            grain = profile.AddSettings<Grain>(); grain.enabled.Override(false); grain.intensity.Override(.16f); grain.size.Override(1.1f); grain.colored.Override(false);
            fringe = profile.AddSettings<ChromaticAberration>(); fringe.enabled.Override(false); fringe.intensity.Override(.05f); fringe.fastMode.Override(true);
            volume = PostProcessManager.instance.QuickVolume(0, 100, profile.settings.ToArray()); volume.transform.SetParent(transform, false);
            Apply(quality);
        }

        /// <summary>0 Performance Â· 1 Balanced (default) Â· 2 Cinematic.</summary>
        public void Apply(int q)
        {
            quality = Mathf.Clamp(q, 0, 2);
            QualitySettings.shadowCascades = quality == 2 ? 4 : 2; QualitySettings.shadowDistance = new[] { 70f, 110f, 170f }[quality];
            QualitySettings.shadowResolution = quality == 2 ? ShadowResolution.VeryHigh : ShadowResolution.High;
            QualitySettings.antiAliasing = quality == 0 ? 0 : 4; if (cam) cam.allowMSAA = quality > 0;
            QualitySettings.pixelLightCount = quality == 0 ? 2 : 4; probeEvery = quality == 2 ? 3 : 8;
            if (layer) layer.antialiasingMode = quality == 0 ? PostProcessLayer.Antialiasing.FastApproximateAntialiasing : PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
            if (ao != null) { ao.enabled.Override(quality > 0); ao.mode.Override(quality == 2 ? AmbientOcclusionMode.MultiScaleVolumetricObscurance : AmbientOcclusionMode.ScalableAmbientObscurance); ao.intensity.Override(quality == 2 ? .45f : .3f); ao.quality.Override(AmbientOcclusionQuality.Medium); }
            if (grain != null) grain.enabled.Override(film == 2);
            if (fringe != null) fringe.enabled.Override(false);
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ Time-of-day grade â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        struct Grade
        {
            public float temp, tint, sat, contrast, exposure; public Vector4 lift, gamma, gain; public Color bloomTint;
            public static Grade Lerp(Grade a, Grade b, float t) => new Grade
            {
                temp = Mathf.Lerp(a.temp, b.temp, t), tint = Mathf.Lerp(a.tint, b.tint, t), sat = Mathf.Lerp(a.sat, b.sat, t), contrast = Mathf.Lerp(a.contrast, b.contrast, t), exposure = Mathf.Lerp(a.exposure, b.exposure, t),
                lift = Vector4.Lerp(a.lift, b.lift, t), gamma = Vector4.Lerp(a.gamma, b.gamma, t), gain = Vector4.Lerp(a.gain, b.gain, t), bloomTint = Color.Lerp(a.bloomTint, b.bloomTint, t)
            };
        }

        static Grade G(float temp, float tint, float sat, float contrast, float exposure, Vector4 lift, Vector4 gamma, Vector4 gain, Color bloom) =>
            new Grade { temp = temp, tint = tint, sat = sat, contrast = contrast, exposure = exposure, lift = lift, gamma = gamma, gain = gain, bloomTint = bloom };

        // Hours and their grades. Lift/gamma/gain are (r, g, b, master) offsets: lift tints the shadows,
        // gain the highlights. Shadows lean teal/blue, highlights lean warm, the classic film split.
        static readonly (float hour, Grade grade)[] Keys =
        {
            (0f,    G(-12, 2, -4, 13, 1.05f,new Vector4(-.01f, .005f, .045f, 0),  new Vector4(-.02f, 0, .04f, 0),    new Vector4(-.04f, .0f, .07f, 0),  new Color(.75f, .85f, 1f))),   // moonlit night
            (4.6f,  G(-11, 4, -3, 13, .9f,new Vector4(0, .005f, .04f, 0),       new Vector4(-.01f, 0, .03f, 0),    new Vector4(-.03f, 0, .05f, 0),    new Color(.8f, .85f, 1f))),
            (5.8f,  G(2, 9, -2, 14, .1f,     new Vector4(.02f, -.005f, .045f, .01f), new Vector4(.02f, -.01f, .02f, 0), new Vector4(.06f, .01f, .0f, 0),  new Color(1f, .8f, .85f))),   // dawn: violet shadows, rose light
            (7.5f,  G(8, 4, 6, 15, -.05f,    new Vector4(.0f, .01f, .03f, .01f),   new Vector4(.01f, 0, -.01f, 0),    new Vector4(.05f, .02f, -.03f, 0), new Color(1f, .9f, .78f))),
            (12f,   G(6, 2, 12, 17, -.32f,   new Vector4(-.012f, .008f, .028f, .012f), new Vector4(.0f, 0, 0, 0),     new Vector4(.04f, .02f, -.04f, 0), new Color(1f, .92f, .82f))),  // day: orange & teal
            (15.5f, G(9, 3, 12, 17, -.28f,   new Vector4(-.01f, .006f, .026f, .01f), new Vector4(.01f, 0, -.01f, 0),  new Vector4(.05f, .02f, -.04f, 0), new Color(1f, .88f, .74f))),
            (17.4f, G(14, 4, 8, 19, -.2f,    new Vector4(-.005f, .0f, .035f, .008f), new Vector4(.015f, 0, -.015f, 0), new Vector4(.07f, .02f, -.05f, 0), new Color(1f, .78f, .58f))),   // golden hour: amber light, cool shadows
            (18.6f, G(-6, 6, 4, 16, .2f,     new Vector4(.0f, .01f, .06f, 0),      new Vector4(-.01f, 0, .04f, 0),    new Vector4(.06f, -.01f, .04f, 0), new Color(.95f, .8f, 1f))),   // blue hour: cobalt with pink rim
            (20f,   G(-11, 3, -2, 13, .95f,  new Vector4(-.005f, .005f, .05f, 0),  new Vector4(-.02f, 0, .04f, 0),    new Vector4(-.03f, 0, .06f, 0),    new Color(.8f, .85f, 1f))),
            (24f,   G(-12, 2, -4, 13, 1.05f,new Vector4(-.01f, .005f, .045f, 0),  new Vector4(-.02f, 0, .04f, 0),    new Vector4(-.04f, .0f, .07f, 0),  new Color(.75f, .85f, 1f))),
        };
        static readonly Grade Monsoon = G(-6, -3, -22, 9, -.05f, new Vector4(0, .012f, .018f, .02f), new Vector4(0, .01f, .01f, 0), new Vector4(-.02f, .01f, .0f, 0), new Color(.9f, .95f, 1f));
        static readonly Grade Candle = G(14, 2, -4, 22, .5f, new Vector4(.01f, 0, .02f, -.01f), new Vector4(.02f, 0, -.02f, 0), new Vector4(.12f, .03f, -.08f, 0), new Color(1f, .7f, .45f));

        static Grade AtHour(float h)
        {
            h = Mathf.Repeat(h, 24);
            for (int i = 0; i < Keys.Length - 1; i++)
                if (h >= Keys[i].hour && h <= Keys[i + 1].hour)
                {
                    float t = Mathf.InverseLerp(Keys[i].hour, Keys[i + 1].hour, h);
                    return Grade.Lerp(Keys[i].grade, Keys[i + 1].grade, t * t * (3 - 2 * t));
                }
            return Keys[0].grade;
        }

        /// <summary>Updates the grade for the time of day, weather and power, then applies the film look.</summary>
        public void Tick(float night, float rain, Vector3 focus, float minutes, bool powerCut)
        {
            probeTimer -= Time.deltaTime; if (probe && probeTimer <= 0) { probe.transform.position = focus; probe.RenderProbe(); probeTimer = probeEvery; }
            if (grading == null) return;
            var g = AtHour(minutes / 60f);
            g = Grade.Lerp(g, Monsoon, Mathf.Clamp01(rain) * .85f);
            if (powerCut) g = Grade.Lerp(g, Candle, night * .45f);

            if (film == 0)
                g = G(10, -3, -7, 9, Mathf.Lerp(.15f, .65f, night),
                    new Vector4(-.006f, .008f, .004f, .025f),
                    new Vector4(.015f, .005f, -.01f, .02f),
                    new Vector4(.025f, .012f, -.015f, 0),
                    new Color(1f, .91f, .76f));
            float strength = film == 0 ? 1f : .3f;                       // Clear uses the lightest time-of-day tint
            Vector4 fade = Vector4.zero; // Preserve the black point
            grading.temperature.Override(g.temp * strength);
            grading.tint.Override(g.tint * strength);
            grading.lift.Override(g.lift * strength + fade);
            grading.gamma.Override(g.gamma * strength);
            grading.gain.Override(g.gain * strength);
            grading.contrast.Override(film == 2 ? 28 : film == 0 ? g.contrast : g.contrast * .35f);
            grading.saturation.Override(film == 2 ? -100 : g.sat * strength);
            grading.postExposure.Override((film == 0 ? g.exposure : Mathf.Lerp(g.exposure, .1f, 1 - night)) - rain * .1f + (film == 2 ? -.15f : 0));
            // Kurosawa B&W: a red filter on black-and-white film darkens blue skies and brightens skin.
            grading.colorFilter.Override(film == 2 ? new Color(1f, .9f, .75f) : Color.white);
            grading.mixerRedOutRedIn.Override(film == 2 ? 120 : 100);
            grading.mixerRedOutBlueIn.Override(film == 2 ? -20 : 0);

            bloom.intensity.Override(Mathf.Lerp(.18f, .45f, night) * (film == 2 ? .8f : 1));
            bloom.color.Override(film == 2 ? Color.white : Color.Lerp(Color.white, g.bloomTint, .15f)); // Restrained highlight tint
            vignette.intensity.Override((film == 2 ? .25f : .06f) + rain * .1f - night * .06f);
            grain.enabled.Override(film == 2);
            grain.intensity.Override(film == 2 ? .24f : film == 0 ? .075f : .035f); grain.size.Override(film == 2 ? 1.5f : 1.1f);
            fringe.intensity.Override(0);
        }
    }
}

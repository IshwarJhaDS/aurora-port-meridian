// Sun and moon, sky, haze, street lamps and 2000s load shedding.
// Days are warm and hazy ("hotty vibes"); nights are blue and calm ("chill vibes").
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PhewaValley
{
    public class DayNight : MonoBehaviour
    {
        public Light sun, moon, fill; Material sky; readonly List<Light> lampLights = new List<Light>();
        // Keep save-facing fields for compatibility; rain effects are disabled.
        public float rainTarget, rainTimer;
        public float Wetness => 0; public float Rain => 0;
        public float Night { get; private set; }
        public bool PowerCut { get; private set; } public string PowerCutUntil { get; private set; }
        public string WeatherName => PowerCut ? "Load shedding · बत्ती गयो" : Night > .5f ? "Cool, clear night" : "Hot and hazy";
        static readonly int NightId = Shader.PropertyToID("_PhewaNight"), PowerId = Shader.PropertyToID("_PhewaPower"), WetId = Shader.PropertyToID("_PhewaWet");

        public void Init()
        {
            var sunGo = new GameObject("Sun"); sunGo.transform.SetParent(transform, false);
            sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional; sun.shadows = LightShadows.Soft; sun.shadowStrength = .85f;
            var moonGo = new GameObject("Moon"); moonGo.transform.SetParent(transform, false);
            moon = moonGo.AddComponent<Light>(); moon.type = LightType.Directional; moon.shadows = LightShadows.Soft; moon.shadowStrength = .5f; moon.color = new Color(.55f, .66f, 1f);
            RenderSettings.sun = sun;
            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader) { sky = new Material(skyShader); sky.SetFloat("_AtmosphereThickness", 1.25f); sky.SetFloat("_SunSize", .05f); RenderSettings.skybox = sky; }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            QualitySettings.shadowDistance = 140;
            Shader.SetGlobalFloat(PowerId, 1);
            for (int i = 0; i < 12; i++)
            {
                var l = new GameObject($"Lamp light {i}").AddComponent<Light>(); l.transform.SetParent(transform, false);
                l.type = LightType.Point; l.range = 21; l.color = new Color(1, .74f, .42f); l.shadows = LightShadows.None; l.enabled = false; lampLights.Add(l);
            }
            // Night "hero light", as open-world games do: a soft, shadowless fill from the camera side that keeps the
            // player and the people around them readable at night (dark clothes otherwise turn into silhouettes).
            fill = new GameObject("Night fill light").AddComponent<Light>(); fill.transform.SetParent(transform, false);
            fill.type = LightType.Point; fill.range = 11; fill.color = new Color(.78f, .86f, 1f); fill.shadows = LightShadows.None; fill.renderMode = LightRenderMode.ForcePixel; fill.enabled = false;
            // Clear old saved wetness immediately, including before the first simulation tick.
            rainTarget = rainTimer = 0;
            Shader.SetGlobalFloat(WetId, 0);
        }

        /// <summary>The 2000s load-shedding roster: evenings rotate by day, some early mornings too.</summary>
        public static (bool on, string until) Schedule(int day, float minutes)
        {
            float h = minutes / 60f;
            (float from, float to) evening = day % 2 == 0 ? (18f, 20f) : (19f, 21f);
            if (h >= evening.from && h < evening.to) return (true, $"{(int)evening.to:00}:00");
            if (day % 3 == 0 && h >= 5 && h < 7) return (true, "07:00");
            return (false, null);
        }

        void OnDestroy()
        {
            if (sky) Destroy(sky);
        }

        public void Tick(float minutes, int day, Vector3 focus, IEnumerable<LampInfo> lamps, ICollection<string> damaged)
        {
            float hour = minutes / 60f, angle = (hour - 6) / 24f * 360f; // 06:00 sunrise, 18:00 sunset
            sun.transform.rotation = Quaternion.Euler(angle, -30, 0); moon.transform.rotation = Quaternion.Euler(angle + 180, -10, 0);
            float elevation = Mathf.Sin(angle * Mathf.Deg2Rad); Night = Mathf.Clamp01((.15f - elevation) / .28f);
            (PowerCut, PowerCutUntil) = Schedule(day, minutes);
            float day01 = 1 - Night, cloud = Rain * .6f, noon = Mathf.Clamp01(elevation * 1.6f);
            // Hot, golden, slightly hazy days.
            sun.intensity = Mathf.Lerp(0, 1.35f, day01) * (1 - cloud * .6f);
            sun.color = Color.Lerp(new Color(1, .55f, .3f), new Color(1, .9f, .74f), noon);
            sun.enabled = sun.intensity > .01f;
            // Moonlit, cool nights.
            moon.intensity = .22f * Night * (1 - cloud * .6f); moon.enabled = moon.intensity > .01f;
            var camT = Camera.main ? Camera.main.transform : null;
            fill.enabled = Night > .05f && camT;
            if (fill.enabled)
            {
                var toCam = camT.position - focus; toCam.y = 0; toCam = toCam.sqrMagnitude > .01f ? toCam.normalized : Vector3.back;
                fill.transform.position = focus + Vector3.up * 2.4f + toCam * 2.2f; fill.intensity = .9f * Night;
            }
            if (sky) { sky.SetFloat("_Exposure", Mathf.Lerp(.35f, 1.3f, day01) * (1 - cloud * .45f)); sky.SetColor("_SkyTint", Color.Lerp(Color.Lerp(new Color(.35f, .42f, .7f), new Color(.62f, .55f, .45f), day01), new Color(.6f, .62f, .66f), cloud)); sky.SetColor("_GroundColor", Color.Lerp(new Color(.08f, .1f, .16f), new Color(.55f, .48f, .38f), day01)); }
            // Night ambient is a readable blue moonlight: faces and clothes stay visible, not pure silhouettes.
            RenderSettings.ambientSkyColor = Color.Lerp(new Color(.1f, .14f, .24f), new Color(.62f, .6f, .58f), day01) * (1 - cloud * .3f);
            RenderSettings.ambientEquatorColor = Color.Lerp(new Color(.09f, .12f, .2f), new Color(.62f, .52f, .38f), day01);
            RenderSettings.ambientGroundColor = Color.Lerp(new Color(.05f, .06f, .09f), new Color(.32f, .26f, .18f), day01);
            RenderSettings.fogColor = Color.Lerp(new Color(.07f, .1f, .2f), Color.Lerp(new Color(.86f, .78f, .62f), new Color(.72f, .76f, .8f), cloud), day01);
            RenderSettings.fogDensity = (day01 > .5f ? .0026f : .0016f) + Rain * .004f;
            Shader.SetGlobalFloat(NightId, Night); Shader.SetGlobalFloat(PowerId, PowerCut ? 0 : 1);
            Shader.SetGlobalFloat(WetId, 0);
            var near = PowerCut ? new List<LampInfo>() : lamps.Where(l => !damaged.Contains(l.id)).OrderBy(l => (l.position - focus).sqrMagnitude).Take(lampLights.Count).ToList();
            for (int i = 0; i < lampLights.Count; i++)
            {
                bool on = Night > .05f && i < near.Count; lampLights[i].enabled = on;
                if (on) { lampLights[i].transform.position = near[i].position + new Vector3(1, 5.4f, 0); lampLights[i].intensity = 2.8f * Night; }
            }
        }
    }
}

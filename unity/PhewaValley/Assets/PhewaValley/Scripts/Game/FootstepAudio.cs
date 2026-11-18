using UnityEngine;

namespace PhewaValley
{
    public enum GroundSound { Stone, Road, Grass }

    /// <summary>Small locally synthesized foley; no downloads or generated-asset dependency.</summary>
    public class FootstepAudio : MonoBehaviour
    {
        AudioSource source; readonly AudioClip[] steps = new AudioClip[3]; AudioClip landing;
        float distance; int step;
        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>(); source.spatialBlend = .6f;
            source.minDistance = 2; source.maxDistance = 14; source.playOnAwake = false;
            for (int i = 0; i < steps.Length; i++) steps[i] = MakeClip((GroundSound)i);
            landing = MakeClip(GroundSound.Stone, true);
        }
        public static AudioClip MakeClip(GroundSound surface, bool heavy = false)
        {
            const int rate = 22050; int count = Mathf.RoundToInt(rate * (heavy ? .18f : .11f));
            var samples = new float[count]; var random = new System.Random(731 + (int)surface);
            float low = 0, smooth = surface == GroundSound.Grass ? .16f : surface == GroundSound.Road ? .45f : .7f;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate, envelope = Mathf.Exp(-t * (heavy ? 24 : 48)) * Mathf.Clamp01(t / .003f);
                low = Mathf.Lerp(low, (float)random.NextDouble() * 2 - 1, smooth);
                float thump = Mathf.Sin(t * Mathf.PI * 2 * (heavy ? 85 : 125)) * Mathf.Exp(-t * 60);
                samples[i] = Mathf.Clamp((low * .55f + thump * .4f) * envelope, -1, 1);
            }
            var clip = AudioClip.Create(heavy ? "Soft landing" : "Footstep " + surface, count, 1, rate, false);
            clip.SetData(samples, 0); return clip;
        }
        public static GroundSound SurfaceAt(Vector3 feet)
        {
            if (World.TerrainHeight(feet.x, feet.z) > .15f) return GroundSound.Grass;
            float RoadDistance(float v) { float n = Mathf.Repeat(v, 80); return Mathf.Min(n, 80 - n); }
            return Mathf.Min(RoadDistance(feet.x), RoadDistance(feet.z)) < 8 ? GroundSound.Road : GroundSound.Stone;
        }
        bool Audible => !Game.Instance || Game.Instance.audioDirector && Game.Instance.audioDirector.Enabled && !Game.Instance.Paused;
        public void Travel(float metres, float speed, bool grounded)
        {
            if (!grounded || speed < .2f) { distance = 0; return; }
            distance += metres; float spacing = Mathf.Lerp(.58f, 1.35f, Mathf.InverseLerp(1.2f, 5.8f, speed));
            if (distance < spacing) return;
            distance %= spacing;
            if (!Audible) return;
            source.pitch = (++step % 2 == 0 ? 1.04f : .96f);
            source.PlayOneShot(steps[(int)SurfaceAt(transform.position)], Mathf.Lerp(.17f, .3f, Mathf.InverseLerp(1, 6, speed)));
        }
        public void Land(float impact)
        {
            distance = 0; if (!Audible || impact < 3) return;
            source.pitch = 1; source.PlayOneShot(landing, Mathf.Lerp(.18f, .45f, Mathf.InverseLerp(3, 14, impact)));
        }
        public void ResetStride() { distance = 0; }
        void OnDestroy() { foreach (var clip in steps) if (clip) Destroy(clip); if (landing) Destroy(landing); }
    }
}

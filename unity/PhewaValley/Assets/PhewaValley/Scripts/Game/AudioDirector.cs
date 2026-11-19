// Plays ElevenLabs output from StreamingAssets: ambience beds crossfaded by district,
// time and weather; voice clips per sentence; one-shot effects; background music.
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace PhewaValley
{
    public class AudioDirector : MonoBehaviour
    {
        readonly Dictionary<string, AudioSource> scores = new Dictionary<string, AudioSource>();
        float actionUntil;
        readonly Dictionary<string, AudioSource> beds = new Dictionary<string, AudioSource>();
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, string> voiceFiles = new Dictionary<string, string>();
        AudioSource voice, effects, music, engine; string activeScore = "theme"; WorldData data; Coroutine speaking;
        public bool Enabled = true; public bool HasVoices => voiceFiles.Count > 0;
        public bool Speaking => speaking != null;
        /// <summary>The generated music track, also played through cassette players.</summary>
        public int LoadedAmbience
        {
            get { int count = 0; foreach (var bed in beds.Values) if (bed.clip) count++; return count; }
        }
        public int LoadedEffects => clips.Count;
        public int LoadedScores => scores.Count;
        public AudioClip MusicClip => scores.TryGetValue(activeScore, out var current) ? current.clip : music ? music.clip : null;
        static readonly Vector2[] Temples = { new Vector2(47, 47), new Vector2(-120, 214) };

        public void Init(WorldData data)
        {
            this.data = data;
            voice = gameObject.AddComponent<AudioSource>(); effects = gameObject.AddComponent<AudioSource>(); music = gameObject.AddComponent<AudioSource>();
            music.loop = true; music.volume = 0;
            var engineObject = new GameObject("Car engine audio"); engineObject.transform.SetParent(transform, false);
            engine = engineObject.AddComponent<AudioSource>(); engine.loop = true; engine.volume = 0;
            engine.spatialBlend = .65f; engine.minDistance = 3; engine.maxDistance = 35;
            foreach (var v in data.audio?.voices ?? new VoiceClipData[0]) voiceFiles[Key(v.speaker, v.sentence)] = v.file;
            foreach (var a in data.audio?.ambience ?? new AudioEntry[0]) { var s = gameObject.AddComponent<AudioSource>(); s.loop = true; s.volume = 0; beds[a.id] = s; StartCoroutine(LoadInto(a.file, clip => { s.clip = clip; s.time = Random.Range(0, clip.length * .9f); s.Play(); })); }
            foreach (var entry in data.audio?.music ?? new AudioEntry[0])
            {
                var source = entry.id == "theme" ? music : gameObject.AddComponent<AudioSource>();
                source.loop = true; source.volume = 0;
                var id = entry.id;
                StartCoroutine(LoadInto(entry.file, clip => { source.clip = clip; scores[id] = source; source.Play(); }));
            }
            foreach (var e in data.audio?.effects ?? new AudioEntry[0]) { var id = e.id; StartCoroutine(LoadInto(e.file, clip =>
            {
                clips[id] = clip;
                if (id == "engine_loop") { engine.clip = clip; engine.Play(); }
            })); }
        }

        static string Key(string speaker, string sentence) => speaker + "|" + Regex.Replace(sentence.Trim(), @"\s+", " ").ToLowerInvariant();

        IEnumerator LoadInto(string file, System.Action<AudioClip> done)
        {
            var path = Path.Combine(WorldData.Root, "generated", file);
            if (!File.Exists(path)) yield break;
            using var req = UnityWebRequestMultimedia.GetAudioClip(new System.Uri(path).AbsoluteUri, AudioType.MPEG);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) { Debug.LogWarning($"[audio] {file}: {req.error}"); yield break; }
            var clip = DownloadHandlerAudioClip.GetContent(req); clip.name = file; done(clip);
        }

        public void Tick(string district, Vector3 position, float rain, float night, bool paused)
        {
            float temple = 0; foreach (var t in Temples) temple = Mathf.Max(temple, 1 - Vector2.Distance(new Vector2(position.x, position.z), t) / 55);
            temple = Mathf.Max(0, temple);
            float town = district == "coast" || district == "rural" ? 0 : district == "suburbs" || district == "industrial" ? .65f : 1;
            var mix = new Dictionary<string, float>
            {
                ["bazaar"] = town * (1 - night * .75f) * (1 - temple * .5f), ["temple"] = temple * .9f, ["lake"] = district == "coast" ? 1 : 0,
                ["hills"] = district == "rural" ? 1 - night * .4f : 0, ["night"] = night * .85f, ["rain"] = Mathf.Min(1, rain * 1.2f)
            };
            float master = !Enabled ? 0 : Speaking ? .18f : paused ? .25f : .55f;
            foreach (var kv in beds) kv.Value.volume = Mathf.MoveTowards(kv.Value.volume, (mix.TryGetValue(kv.Key, out var v) ? v : 0) * master, Time.unscaledDeltaTime * .5f);
            var game = Game.Instance;
            bool driving = game && game.Started && game.player.InCar;
            bool dialogue = Speaking || game && game.InDialogue;
            if (driving && Mathf.Abs(game.car.speed) > 12) actionUntil = Mathf.Max(actionUntil, Time.unscaledTime + 8);
            if (game && game.car)
            {
                engine.transform.position = game.car.transform.position;
                float revs = Mathf.Repeat(Mathf.Abs(game.car.speed), 5) / 5;
                engine.pitch = Mathf.Lerp(.85f, 1.65f, revs);
                float level = Enabled && !paused && (driving || Mathf.Abs(game.car.speed) > .5f) ? Mathf.Lerp(.16f, .3f, revs) : 0;
                engine.volume = Mathf.MoveTowards(engine.volume, level, Time.unscaledDeltaTime * .7f);
            }
            string score = paused ? "theme" : Time.unscaledTime < actionUntil || driving && Mathf.Abs(game.car.speed) > 12 ? "tension" :
                night > .5f ? "night" : district == "coast" || district == "rural" ? "exploration" : "theme";
            if (!scores.ContainsKey(score)) score = "theme";
            activeScore = score;
            float musicLevel = !Enabled || driving && !paused ? 0 : dialogue ? .08f : paused ? .45f : .32f;
            foreach (var track in scores)
                track.Value.volume = Mathf.MoveTowards(track.Value.volume, track.Key == score ? musicLevel : 0, Time.unscaledDeltaTime * .08f);
        }

        public AudioClip EffectClip(string id) => Enabled && clips.TryGetValue(id, out var clip) ? clip : null;

        public void Effect(string id, float volume = .7f)
        {
            var clip = EffectClip(id);
            if (clip) effects.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        public void ActionCue(string group, string action)
        {
            if (group == "Blade" || group == "Archery" || group == "Stealth") actionUntil = Time.unscaledTime + 12;
            if (action == "bow_release") Effect("bow_release", .5f);
            else if (group == "Blade" && (action.Contains("cut") || action.Contains("thrust"))) Effect("blade_swing", .4f);
            else if (group == "Traversal" || group == "Social" || group == "Stealth") Effect("cloth_whoosh", .25f);
        }

        void OnDestroy()
        {
            var owned = new HashSet<AudioClip>(clips.Values);
            foreach (var bed in beds.Values) if (bed && bed.clip) owned.Add(bed.clip);
            foreach (var score in scores.Values) if (score && score.clip) owned.Add(score.clip);
            foreach (var clip in owned) if (clip) Destroy(clip);
        }

        /// <summary>Plays every sentence's clip in order; false if any sentence is not voiced.</summary>
        public bool Speak(string speaker, string text)
        {
            Stop();
            var files = new List<string>();
            foreach (Match m in Regex.Matches(text, @"[^.!?…]+[.!?…]+[""”’)]*|[^.!?…]+$"))
            {
                var s = m.Value.Trim(); if (s.Length == 0 || Regex.IsMatch(s, @"^It is \d{1,2}:\d{2}\.$")) continue;
                if (!voiceFiles.TryGetValue(Key(speaker, s), out var f)) return false; files.Add(f);
            }
            if (files.Count == 0) return false;
            speaking = StartCoroutine(PlaySequence(files)); return true;
        }

        IEnumerator PlaySequence(List<string> files)
        {
            foreach (var f in files)
            {
                AudioClip clip = null;
                if (!clips.TryGetValue(f, out clip)) { yield return LoadInto(f, c => clip = clips[f] = c); }
                if (clip == null) continue;
                voice.clip = clip; voice.Play(); yield return new WaitWhile(() => voice.isPlaying); yield return new WaitForSeconds(.08f);
            }
            speaking = null;
        }

        public void Stop() { if (speaking != null) StopCoroutine(speaking); speaking = null; voice.Stop(); }
    }
}

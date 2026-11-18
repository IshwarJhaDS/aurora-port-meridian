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
        readonly Dictionary<string, AudioSource> beds = new Dictionary<string, AudioSource>();
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, string> voiceFiles = new Dictionary<string, string>();
        AudioSource voice, effects, music; WorldData data; Coroutine speaking;
        public bool Enabled = true; public bool HasVoices => voiceFiles.Count > 0;
        public bool Speaking => speaking != null;
        /// <summary>The generated music track, also played through cassette players.</summary>
        public AudioClip MusicClip => music ? music.clip : null;
        static readonly Vector2[] Temples = { new Vector2(47, 47), new Vector2(-120, 214) };

        public void Init(WorldData data)
        {
            this.data = data;
            voice = gameObject.AddComponent<AudioSource>(); effects = gameObject.AddComponent<AudioSource>(); music = gameObject.AddComponent<AudioSource>();
            music.loop = true; music.volume = 0;
            foreach (var v in data.audio?.voices ?? new VoiceClipData[0]) voiceFiles[Key(v.speaker, v.sentence)] = v.file;
            foreach (var a in data.audio?.ambience ?? new AudioEntry[0]) { var s = gameObject.AddComponent<AudioSource>(); s.loop = true; s.volume = 0; beds[a.id] = s; StartCoroutine(LoadInto(a.file, clip => { s.clip = clip; s.time = Random.Range(0, clip.length * .9f); s.Play(); })); }
            foreach (var m in data.audio?.music ?? new AudioEntry[0]) if (m.id == "theme") StartCoroutine(LoadInto(m.file, clip => { music.clip = clip; music.Play(); }));
            foreach (var e in data.audio?.effects ?? new AudioEntry[0]) { var id = e.id; StartCoroutine(LoadInto(e.file, clip => clips[id] = clip)); }
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
            music.volume = Mathf.MoveTowards(music.volume, Enabled ? (paused ? .38f : .14f) : 0, Time.unscaledDeltaTime * .3f);
        }

        public void Effect(string id) { if (Enabled && clips.TryGetValue(id, out var c)) effects.PlayOneShot(c, .7f); }

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

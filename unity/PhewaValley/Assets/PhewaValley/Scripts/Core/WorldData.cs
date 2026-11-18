// Content exported from the web game by `npm run unity:data` (tools/export-unity-data.mjs).
// Do not hand-edit world.json; edit the JavaScript sources and re-export.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PhewaValley
{
    [Serializable] public class PoiData { public string id, name, kind, description; public float x, z; public int openFrom, openTo; }
    [Serializable] public class CitizenData { public string id, name, role, place, line; public float x, z; public float[] outfit, topi; }
    [Serializable] public class GreetingData { public string again, morning, afternoon, evening; }
    [Serializable] public class LineData { public string fleeing, idle, later, thanks, rain, evening, day, jobsFallback, weatherRain, weatherClear, askOthers, welcome, topics, unknown; }
    [Serializable] public class IdText { public string id, text; }
    [Serializable] public class JobData { public string id; public string[] missions; }
    [Serializable] public class DialogueData { public GreetingData greetings; public LineData lines; public IdText[] voices; public JobData[] jobs; }
    [Serializable] public class StepData { public string kind, target, place, text; public string[] targets; public float x, z; public bool hasPosition; }
    [Serializable] public class VariantData { public StepData[] steps; }
    [Serializable] public class MissionData { public string id, title, category, description, giver, requirement; public int reward, reputation; public bool repeatable; public VariantData[] variants; }
    [Serializable] public class PlacementData { public float x, z, yaw, y = -999; }
    [Serializable] public class ModelData { public string id, replaces, building, file; public float height; public bool solid, water; public PlacementData[] placements; }
    [Serializable] public class CharacterData { public string id; public float height; }
    [Serializable] public class AudioEntry { public string id, file; }
    [Serializable] public class VoiceClipData { public string speaker, sentence, file; }
    [Serializable] public class AudioData { public AudioEntry[] ambience, effects, music; public VoiceClipData[] voices; }

    [Serializable]
    public class WorldData
    {
        public PoiData[] pois; public CitizenData[] citizens; public DialogueData dialogue; public MissionData[] missions;
        public ModelData[] models; public CharacterData[] characters; public AudioData audio;

        public static string Root => Path.Combine(Application.streamingAssetsPath, "PhewaValley");

        public static WorldData Load()
        {
            var path = Path.Combine(Root, "world.json");
            if (!File.Exists(path)) throw new FileNotFoundException("world.json is missing. Run `npm run unity:data` in the project root.", path);
            return JsonUtility.FromJson<WorldData>(File.ReadAllText(path));
        }

        public PoiData Poi(string id) { foreach (var p in pois) if (p.id == id) return p; return null; }
        public MissionData Mission(string id) { foreach (var m in missions) if (m.id == id) return m; return null; }
        public string[] JobsFor(string id) { foreach (var j in dialogue.jobs) if (j.id == id) return j.missions; return Array.Empty<string>(); }
        public string VoiceLine(string id) { foreach (var v in dialogue.voices) if (v.id == id) return v.text; return null; }
        public Vector3 PoiPosition(string id) { var p = Poi(id); return p == null ? Vector3.zero : new Vector3(p.x, World.TerrainHeight(p.x, p.z), p.z); }
    }

    /// <summary>World rules ported from src/world/util.js. Coordinates match the web build.</summary>
    public static class World
    {
        public const float ChunkSize = 80f;
        public const int Min = -6, Max = 6;
        public const float LakeEdge = 436f;

        public struct District { public string id, name; public Color color; }

        public static District DistrictAt(float x, float z)
        {
            if (x > 420) return new District { id = "coast", name = "Phewa Lakeside", color = new Color(.37f, .65f, .72f) };
            if (z > 230) return new District { id = "rural", name = "Sarangkot Hills", color = new Color(.55f, .67f, .45f) };
            if (x > 180) return new District { id = "industrial", name = "Ghat Bazaar", color = new Color(.84f, .67f, .47f) };
            if (x < -150) return new District { id = "suburbs", name = "Newa Tole", color = new Color(.79f, .72f, .63f) };
            if (z < -190) return new District { id = "highway", name = "Prithvi Highway", color = new Color(.57f, .64f, .72f) };
            return new District { id = "downtown", name = "Durbar Tole", color = new Color(.82f, .6f, .48f) };
        }

        public static float TerrainHeight(float x, float z)
        {
            if (x > LakeEdge) return -3.5f;
            float field = Mathf.Clamp01((z - 230f) / 100f);
            if (field <= 0) return 0;
            float RoadDistance(float n) { float m = ((n % 80f) + 80f) % 80f; return Mathf.Min(m, 80f - m); }
            float roadMask = Mathf.Clamp01((Mathf.Min(RoadDistance(x), RoadDistance(z)) - 12f) / 13f);
            float hills = 2.2f + 2.1f * Mathf.Sin(x * .018f) * Mathf.Cos(z * .021f);
            return hills * field * roadMask;
        }

        public static uint Hash(int x, int z = 0, int salt = 0)
        {
            unchecked
            {
                int h = (x ^ (int)0x9e3779b9) * (int)0x85ebca6b ^ (z ^ salt) * (int)0xc2b2ae35;
                h = (h ^ (int)((uint)h >> 16)) * 0x7feb352d;
                h = (h ^ (int)((uint)h >> 15)) * (int)0x846ca68b;
                return (uint)(h ^ (int)((uint)h >> 16));
            }
        }

        public static string Clock(float minutes) { int m = Mathf.FloorToInt(minutes); return $"{(m / 60) % 24:00}:{m % 60:00}"; }
        public static float DistanceXZ(Vector3 a, Vector3 b) => Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z));
        public static float DistanceXZ(Vector3 a, float x, float z) => Mathf.Sqrt((a.x - x) * (a.x - x) + (a.z - z) * (a.z - z));
    }

    /// <summary>Deterministic generator (mulberry32), same stream as the web build.</summary>
    public class SeededRandom
    {
        uint state;
        public SeededRandom(uint seed) { state = seed == 0 ? 1u : seed; }
        public float Next()
        {
            unchecked
            {
                uint t = state += 0x6D2B79F5;
                t = (t ^ (t >> 15)) * (t | 1);
                t ^= t + (t ^ (t >> 7)) * (t | 61);
                return (t ^ (t >> 14)) / 4294967296f;
            }
        }
        public float Range(float a, float b) => a + (b - a) * Next();
        public T Pick<T>(IList<T> values) => values[Mathf.Min(values.Count - 1, (int)(Next() * values.Count))];
    }
}

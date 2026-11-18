// Save game (port of src/game/progress.js), stored as JSON in PlayerPrefs.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhewaValley
{
    [Serializable] public class MissionState { public string id, status = "active"; public int step, variant; public float remaining = 150; public List<string> flags = new List<string>(); }
    [Serializable] public class Counter { public string id; public int count; }

    [Serializable]
    public class Progress
    {
        public const string Key = "phewa-valley.save.v1";
        public int version = 1;
        [NonSerialized] public bool sessionOnly;
        public float cash = 250, minutes = 16 * 60 + 20; public int reputation, jobCount, courierCycle;
        public string tracked = "arrival";
        public List<MissionState> missions = new List<MissionState>();
        public List<string> receipts = new List<string>(), repaired = new List<string>();
        public List<Counter> conversations = new List<Counter>();
        public Vector3 player = new Vector3(34, 0.1f, 29), car = new Vector3(15, .5f, 49);
        public bool sound = true, ambience = true;
        // Calendar and 2000s pastimes.
        public int day = 1, lattuBest, chungiBest, fireflies, kitesCut, lanBest;
        public float lattuBestSeconds, pingBest;

        // Protagonists, discoveries and settings.
        public int hero; public List<Vector3> heroPositions = new List<Vector3>(); public List<string> memories = new List<string>();
        public int density = 1, drawDistance = 1, graphics = 1;
        /// <summary>0 = radar and distances; 1 = Explorer (no radar, a kite flies over the objective).</summary>
        public int guidance;
        public bool showFirstVisitHints = true;
        public int introVersion, introFlags; public float introWalkDistance, introDriveDistance;
        /// <summary>Film look: 0 = 2000s Kodak, 1 = Natural, 2 = Kurosawa B&amp;W.</summary>
        public int film;
        public float rain, wetness, rainTarget, weatherSeconds = 90;
        /// <summary>Lok Katha folk tales heard at heritage sites.</summary>
        public List<string> tales = new List<string>();

        /// <summary>
        /// Honest work pays more as the tole comes to trust you: +2% per reputation point, up to +50%.
        /// (A response to the critique that in crime games ordinary jobs never pay enough.)
        /// </summary>
        public float HonestBonus => 1 + Mathf.Min(reputation, 25) * .02f;
        public int Pay(int amount) { int paid = Mathf.RoundToInt(amount * HonestBonus); cash += paid; return paid; }

        /// <summary>Pays a pastime reward once per in-game day.</summary>
        public bool DailyReward(string what, int amount, int rep = 0) => Reward($"daily:{what}:{day}", amount, rep);

        public MissionState Mission(string id) => missions.Find(m => m.id == id);
        public int Conversations(string id) => conversations.Find(c => c.id == id)?.count ?? 0;
        public void Talked(string id) { var c = conversations.Find(x => x.id == id); if (c == null) conversations.Add(new Counter { id = id, count = 1 }); else c.count++; }

        public bool Reward(string receipt, int amount, int rep)
        {
            if (receipts.Contains(receipt)) return false;
            receipts.Add(receipt); Pay(amount); reputation += rep; return true;
        }

        public static Progress Load()
        {
            try
            {
                var raw = PlayerPrefs.GetString(Key, "");
                if (!string.IsNullOrEmpty(raw)) { var p = JsonUtility.FromJson<Progress>(raw); if (p != null && p.version == 1) return p.Sanitized(); }
            }
            catch (Exception e) { Debug.LogWarning($"[save] ignored unreadable save: {e.Message}"); }
            return new Progress();
        }

        Progress Sanitized()
        {
            float Finite(float v, float fallback) => float.IsNaN(v) || float.IsInfinity(v) ? fallback : v;
            introWalkDistance = Mathf.Clamp(Finite(introWalkDistance, 0), 0, 4); introDriveDistance = Mathf.Clamp(Finite(introDriveDistance, 0), 0, 40);
            cash = Finite(cash, 250); minutes = Finite(minutes, 980);
            rain = Mathf.Clamp01(Finite(rain, 0)); wetness = Mathf.Clamp01(Finite(wetness, 0)); rainTarget = Mathf.Clamp01(Finite(rainTarget, 0)); weatherSeconds = Mathf.Clamp(Finite(weatherSeconds, 90), 0, 180);
            hero = Mathf.Clamp(hero, 0, 2); graphics = Mathf.Clamp(graphics, 0, 2); film = Mathf.Clamp(film, 0, 2);
            density = Mathf.Clamp(density, 0, 2); drawDistance = Mathf.Clamp(drawDistance, 0, 2); day = Mathf.Max(1, day);
            missions ??= new List<MissionState>(); receipts ??= new List<string>(); repaired ??= new List<string>();
            conversations ??= new List<Counter>(); heroPositions ??= new List<Vector3>(); memories ??= new List<string>(); tales ??= new List<string>();
            missions.ForEach(m => { if (m != null) { m.step = Mathf.Max(0, m.step); m.flags ??= new List<string>(); } });
            conversations.RemoveAll(c => c == null);
            cash = Mathf.Clamp(cash, 0, 10000000); reputation = Mathf.Clamp(reputation, 0, 10000); minutes = ((minutes % 1440) + 1440) % 1440;
            bool Bad(Vector3 v) => float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || v.magnitude > 2000;
            if (Bad(player)) player = new Vector3(34, .1f, 29); if (Bad(car)) car = new Vector3(15, .5f, 49);
            for (int i = 0; i < heroPositions.Count; i++) if (Bad(heroPositions[i])) heroPositions[i] = Protagonists.Roster[i % 3].home;
            missions.RemoveAll(m => m == null || string.IsNullOrEmpty(m.id) || (m.status != "active" && m.status != "completed"));
            return this;
        }

        public void Save() { if (sessionOnly) return; PlayerPrefs.SetString(Key, JsonUtility.ToJson(this)); PlayerPrefs.Save(); }
        public static void Reset() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); }
    }
}

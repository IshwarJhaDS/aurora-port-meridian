// Open-world systems informed by GTA V's development and open-world game research:
//  · Three switchable protagonists with a sky-camera transition (no long drives back).
//  · Dynamic street events that can be taken up or ignored.
//  · A car radio with 2000s FM stations and a DJ/news ticker.
//  · An honest passenger (tempo/taxi) job whose pay grows with reputation.
//  · Samjhana: hidden memories of 2000s Nepal that reward exploring every corner.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PhewaValley
{
    public static class Marker
    {
        static Mesh ring, beam;
        /// <summary>A glowing ground ring and light beam, like a mission marker.</summary>
        public static GameObject Create(Vector3 at, Color color, float radius = 1.4f, float height = 5)
        {
            var go = new GameObject("Marker"); go.transform.position = at;
            var g = new MeshBuilder(); g.Cylinder(0, .05f, 0, radius, .04f, color, new Surface(.5f, 0, 2.2f, 1), 24);
            var b = new MeshBuilder(); b.Cylinder(0, height / 2, 0, .12f, height / 2, color, new Surface(.5f, 0, 3, 1), 8);
            foreach (var (name, mesh) in new[] { ("Ring", g.Build("Ring")), ("Beam", b.Build("Beam")) })
            {
                var part = new GameObject(name); part.transform.SetParent(go.transform, false);
                part.AddComponent<MeshFilter>().sharedMesh = mesh; var r = part.AddComponent<MeshRenderer>(); r.sharedMaterial = Materials.VertexLit; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            go.AddComponent<MarkerBob>(); return go;
        }
    }
    public class MarkerBob : MonoBehaviour { void Update() { transform.GetChild(0).localScale = Vector3.one * (1 + Mathf.Sin(Time.time * 3) * .06f); } }

    // ───────────────────────── Protagonists ─────────────────────────
    public class Protagonists : MonoBehaviour
    {
        public class Hero { public string id, name, role, model, bio; public Color outfit; public Vector3 home; public GameObject idle; }
        public static readonly List<Hero> Roster = new List<Hero>
        {
            new Hero { id = "kiran", name = "Kiran Gurung", role = "College student", model = "player", outfit = new Color(.17f, .23f, .26f), home = new Vector3(34, 0, 29), bio = "Plays chungi between classes, owes Laxman dai for three chiya, dreams of Lakeside." },
            new Hero { id = "dawa", name = "Dawa Sherpa", role = "Tempo driver", model = "resident_m", outfit = new Color(.2f, .25f, .3f), home = new Vector3(100, 0, 74), bio = "Knows every gali of the valley. Press T in any vehicle to start a passenger shift." },
            new Hero { id = "sunita", name = "Sunita Shrestha", role = "Kirana pasal owner", model = "resident_f", outfit = new Color(.15f, .45f, .25f), home = new Vector3(-226, 0, 52), bio = "Sharpest bargainer in Newa Tole. Hears every rumour before the radio does." },
            new Hero { id = "dhane", name = "Dhane", role = "Trail scout", model = "dhane", outfit = new Color(.27f, .34f, .21f), home = new Vector3(44, 0, 29), bio = "Walks the valley trails before the market opens." }
        };
        public List<Hero> heroes => Roster;
        public bool Switching { get; private set; }
        public Hero Current => heroes[Mathf.Clamp(Game.Instance.progress.hero, 0, heroes.Count - 1)];

        public void Init()
        {
            var P = Game.Instance.progress;
            while (P.heroPositions.Count < heroes.Count) P.heroPositions.Add(heroes[P.heroPositions.Count].home);
            P.heroPositions[P.hero] = Game.Instance.player.transform.position;
            for (int i = 0; i < heroes.Count; i++) if (i != P.hero) SpawnIdle(i, P.heroPositions[i]);
        }

        void SpawnIdle(int i, Vector3 at)
        {
            var h = heroes[i]; var go = new GameObject($"{h.name} (idle)"); go.transform.SetParent(transform, false);
            at.y = World.TerrainHeight(at.x, at.z); go.transform.position = at;
            Figure.Create(go.transform, h.model, h.outfit, new Color(.55f, .35f, .22f), new Color(.02f, .02f, .02f), null, Game.Instance.models, i == 2 ? 1.6f : 1.72f);
            h.idle = go;
        }

        public void Switch(int i)
        {
            var G = Game.Instance; if (Switching || i == G.progress.hero || i < 0 || i >= heroes.Count) return;
            if (G.player.InCar) { G.Notify("Park and get out of the vehicle before switching.", false); return; }
            if (G.activities.Current != null || G.InDialogue || G.player.Busy || G.work.HasValue) return;
            StartCoroutine(SwitchRoutine(i));
        }

        /// <summary>Sky camera: rise above the city, glide across, and drop in behind the other character.</summary>
        IEnumerator SwitchRoutine(int i)
        {
            var G = Game.Instance; Switching = true; var P = G.progress; int from = P.hero;
            P.heroPositions[from] = G.player.transform.position; SpawnIdle(from, G.player.transform.position);
            var target = P.heroPositions[i]; target.y = World.TerrainHeight(target.x, target.z);
            var cam = G.rig.transform; G.rig.enabled = false; var start = cam.position; var startRot = cam.rotation;
            var high = new Vector3(start.x, 160, start.z); var over = new Vector3(target.x, 160, target.z); var down = Quaternion.Euler(89, G.rig.yaw, 0);
            for (float t = 0; t < 1; t += Time.deltaTime / .8f) { cam.SetPositionAndRotation(Vector3.Lerp(start, high, Smooth(t)), Quaternion.Slerp(startRot, down, Smooth(t))); yield return null; }
            G.streamer.Prime(target);
            for (float t = 0; t < 1; t += Time.deltaTime / 1.1f) { cam.position = Vector3.Lerp(high, over, Smooth(t)); yield return null; }
            // Swap bodies.
            if (heroes[i].idle) Destroy(heroes[i].idle); heroes[i].idle = null;
            G.player.Teleport(target + Vector3.up * .1f);
            G.player.SetFigure(heroes[i].model, heroes[i].outfit, i == 2 ? 1.6f : 1.75f); P.hero = i;
            var land = target + Quaternion.Euler(12, G.rig.yaw, 0) * Vector3.back * 5 + Vector3.up * 1.6f;
            for (float t = 0; t < 1; t += Time.deltaTime / .8f) { cam.SetPositionAndRotation(Vector3.Lerp(over, land, Smooth(t)), Quaternion.Slerp(down, Quaternion.LookRotation(target + Vector3.up * 1.5f - land), Smooth(t))); yield return null; }
            G.rig.enabled = true; Switching = false; G.Notify($"Now playing as {heroes[i].name}, {heroes[i].role.ToLower()}.", false); G.Save();
        }
        static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }
    }

    // ───────────────────────── Dynamic street events ─────────────────────────
    public class StreetEvents : MonoBehaviour
    {
        public enum Kind { Tourist, Shopping, Cow }
        public class Live { public Kind kind; public Vector3 position; public GameObject root; public Figure npc; public bool accepted; public float timer; public string destination; public List<GameObject> items = new List<GameObject>(); public int nudges; public EventPrompt prompt; }
        public class EventPrompt : Activity
        {
            public Live live; public StreetEvents owner;
            public override string Prompt => live.kind switch { Kind.Tourist => "Help the lost tourist", Kind.Shopping => "Help pick up the spilled shopping", _ => live.nudges == 0 ? "Coax the cow off the road (E × 3)" : $"Keep coaxing the cow ({live.nudges}/3)" };
            public override bool Available => live != null && (!live.accepted || live.kind == Kind.Cow && live.nudges < 3);
            public override void Begin() { owner.Accept(live); End(); }
            public override void Draw(float W, float H) { }
        }
        public Live Current; float cooldown = 45;

        void Update()
        {
            var G = Game.Instance; if (G == null || !G.Ready || !G.Started || G.Paused) return;
            var p = G.player.transform.position;
            if (Current == null)
            {
                cooldown -= Time.deltaTime;
                if (cooldown <= 0 && !G.player.InCar && G.activities.Current == null && !G.InDialogue && World.DistrictAt(p.x, p.z).id != "coast") Spawn(G, p);
                return;
            }
            var e = Current;
            if (!e.accepted && World.DistanceXZ(p, e.position) > 90) { Clear(); return; } // ignored: life goes on
            if (e.kind == Kind.Tourist && e.accepted)
            {
                var t = e.npc.transform.parent; var d = p - t.position; d.y = 0; float speed = d.magnitude > 2.4f ? Mathf.Min(G.player.Speed + 1.5f, 6.5f) : 0;
                if (d.magnitude > 30) { t.position = p - d.normalized * 3; } // never lose your guest
                if (speed > 0) { t.position += d.normalized * speed * Time.deltaTime; t.rotation = Quaternion.LookRotation(d); var tp = t.position; tp.y = World.TerrainHeight(tp.x, tp.z); t.position = tp; }
                e.npc.Speed = speed;
                var goal = G.data.PoiPosition(e.destination);
                if (World.DistanceXZ(t.position, goal) < 10) { Reward(G, 60, 2, $"\"Dhanyabad, dai!\" The tourist made it to {G.data.Poi(e.destination).name}."); G.navTarget = null; Clear(); }
            }
            else if (e.kind == Kind.Shopping && e.accepted)
            {
                e.timer -= Time.deltaTime;
                for (int i = e.items.Count - 1; i >= 0; i--) if (World.DistanceXZ(p, e.items[i].transform.position) < 1.3f) { Destroy(e.items[i]); e.items.RemoveAt(i); }
                if (e.items.Count == 0) { Reward(G, 25, 1, "\"Babu, timi jasto chhora sabai ko hos!\" Every orange is back in the bag."); Clear(); }
                else if (e.timer <= 0) { G.Notify("The auntie gathered the rest herself. Maybe next time.", false); Clear(); }
            }
            else if (e.kind == Kind.Cow && e.nudges >= 3)
            {
                // The cow ambles to the footpath; traffic moves again.
                var t = e.root.transform; var target = new Vector3(e.position.x + (e.timer > 0 ? 9 : 9), t.position.y, t.position.z);
                t.position = Vector3.MoveTowards(t.position, target, .8f * Time.deltaTime); e.timer += Time.deltaTime;
                if (Vector3.Distance(t.position, target) < .2f) { StreetLife.Obstacles.Remove(e.position); Reward(G, 10, 2, "The cow strolls off. Horns stop, drivers wave a thank-you."); Clear(); }
            }
        }

        void Spawn(Game G, Vector3 p)
        {
            var kind = (Kind)Random.Range(0, 3); var fwd = Vector3.ProjectOnPlane(G.rig.transform.forward, Vector3.up).normalized;
            var at = p + fwd * Random.Range(22, 36) + Vector3.Cross(Vector3.up, fwd) * Random.Range(-8, 8);
            // Snap onto the footpath (or onto the road for the cow).
            float Snap(float v, float offset) { float b = Mathf.Round(v / 80) * 80; return b + (v > b ? offset : -offset); }
            bool alongX = Mathf.Abs(((at.z % 80) + 80) % 80 - 40) > Mathf.Abs(((at.x % 80) + 80) % 80 - 40);
            if (alongX) at.z = Snap(at.z, kind == Kind.Cow ? 2.2f : 9); else at.x = Snap(at.x, kind == Kind.Cow ? 2.2f : 9);
            at.x = Mathf.Clamp(at.x, -460, 420); at.z = Mathf.Clamp(at.z, -460, 540); at.y = World.TerrainHeight(at.x, at.z);
            var e = new Live { kind = kind, position = at, root = new GameObject($"Event {kind}") }; e.root.transform.SetParent(transform, false); e.root.transform.position = at;
            if (kind == Kind.Tourist) { e.npc = Figure.Create(e.root.transform, null, new Color(.85f, .55f, .1f), new Color(.85f, .65f, .5f), new Color(.6f, .45f, .2f), null, null, 1.8f); }
            else if (kind == Kind.Shopping)
            {
                e.npc = Figure.Create(e.root.transform, "resident_f", new Color(.5f, .1f, .3f), new Color(.55f, .35f, .22f), new Color(.02f, .02f, .02f), null, G.models, 1.58f);
                for (int i = 0; i < 6; i++)
                {
                    var g = new MeshBuilder(); g.Sphere(0, .06f, 0, .07f, .07f, .07f, i % 2 == 0 ? new Color(.95f, .5f, .05f) : new Color(.55f, .4f, .2f), new Surface(.7f), 8);
                    var item = new GameObject("Spilled"); item.transform.position = at + new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f)); item.transform.position = new Vector3(item.transform.position.x, World.TerrainHeight(item.transform.position.x, item.transform.position.z), item.transform.position.z);
                    item.AddComponent<MeshFilter>().sharedMesh = g.Build("Orange"); item.AddComponent<MeshRenderer>().sharedMaterial = Materials.VertexLit; item.transform.SetParent(e.root.transform, true); e.items.Add(item);
                }
            }
            else
            {
                var g = new MeshBuilder(); var hide = new Color(.88f, .86f, .8f); g.Box(0, .45f, 0, .32f, .3f, .75f, hide); g.Box(0, .65f, .85f, .16f, .17f, .22f, hide); g.Box(0, .77f, .95f, .2f, .03f, .03f, new Color(.4f, .35f, .3f));
                e.root.AddComponent<MeshFilter>().sharedMesh = g.Build("Road cow"); e.root.AddComponent<MeshRenderer>().sharedMaterial = Materials.VertexLit;
                e.root.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0); StreetLife.Obstacles.Add(at);
            }
            e.prompt = new EventPrompt { live = e, owner = this, id = "event", position = at, radius = kind == Kind.Cow ? 3.2f : 2.6f };
            e.prompt.Attach(G.activities); G.activities.all.Add(e.prompt);
            Current = e; cooldown = Random.Range(70, 140);
            G.Notify(kind switch { Kind.Tourist => "Someone nearby looks lost (blue marker on the radar).", Kind.Shopping => "Somebody dropped their shopping just ahead.", _ => "A cow is sitting in the middle of the road. Traffic is backing up." }, false);
        }

        public void Accept(Live e)
        {
            var G = Game.Instance;
            if (e.kind == Kind.Tourist)
            {
                var options = new[] { "waterfront", "park", "plaza", "market", "cafe" }.Where(id => World.DistanceXZ(e.position, G.data.PoiPosition(id)) > 120).ToArray();
                e.destination = options.Length > 0 ? options[Random.Range(0, options.Length)] : "waterfront"; e.accepted = true; G.Navigate(e.destination);
                G.Notify($"\"Namaste! Can you take me to {G.data.Poi(e.destination).name}?\" Walk with them. The micro bus works too.", false);
            }
            else if (e.kind == Kind.Shopping) { e.accepted = true; e.timer = 45; G.Notify("Collect the six oranges and potatoes before they roll into the drain (45 s).", false); }
            else { e.nudges++; e.accepted = true; G.audioDirector.Effect("bell"); if (e.nudges >= 3) G.Notify("\"Hat, hat!\" The cow finally gets up.", false); }
        }

        void Reward(Game G, int cash, int rep, string text) { int paid = G.progress.Pay(cash); G.progress.reputation += rep; G.Notify($"{text}  +Rs {paid}  +{rep} reputation", true); G.Save(); }

        void Clear()
        {
            if (Current == null) return; StreetLife.Obstacles.Remove(Current.position);
            Game.Instance.activities.all.Remove(Current.prompt); if (Current.root) Destroy(Current.root); Current = null;
        }
    }

    // ───────────────────────── Car radio ─────────────────────────
    public class CarRadio : MonoBehaviour
    {
        public int station; public float ShowUntil; AudioSource src; AudioClip staticClip; float tickerTimer; int tickerIndex;
        public static readonly (string name, string freq, float pitch, string[] ticker)[] Stations =
        {
            ("Radio Phewa · Adhunik Geet", "90.4", 1f, new[] { "Next up: a request for Maya didi from Laxman dai at the chiya pasal.", "Purano geet, naya saathi: songs from the cassette shelf, all evening long.", "Shout-out to the Sarangkot kite gang, keep your manja sharp!" }),
            ("Lok Dohori FM · Gaun Ghar", "98.2", 1.05f, new[] { "Dohori night at the Lakeside restaurant this Saturday, entry free.", "Listener letter from Newa Tole: 'Play something for the bandh day cricket team!'", "Madal, sarangi and a little bit of teasing. This is Lok Dohori FM." }),
            ("Samachar 102.4 · News & Talk", "102.4", 1f, new[] { "Load shedding schedule: group three, six to nine this evening. Keep candles ready.", "SLC results next week. Best of luck, students, don't forget your symbol numbers.", "Petrol queue at Prithvi Chowk stretches two kilometres; tempo fares unchanged.", "Weather: hot and hazy by day, monsoon showers likely in the evening." }),
            ("Radio off", "--", 1f, new[] { "" })
        };
        public string Ticker => Stations[station].ticker[tickerIndex % Stations[station].ticker.Length];

        void Start()
        {
            src = gameObject.AddComponent<AudioSource>(); src.loop = true; src.volume = 0; src.spatialBlend = 0;
            gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency = 5200;
            int n = 44100 * 2; var data = new float[n]; float last = 0; for (int i = 0; i < n; i++) { last = last * .9f + Random.Range(-1f, 1f) * .1f; data[i] = last * .25f; }
            staticClip = AudioClip.Create("Tuning static", n, 1, 44100, false); staticClip.SetData(data, 0);
        }

        void Update()
        {
            var G = Game.Instance; if (G == null || !G.Ready) return;
            bool inCar = G.player.InCar && G.Started;
            if (inCar && !G.Paused && !G.ui.PhotoActive && Input.GetKeyDown(KeyCode.Q) && G.activities.Current == null) { station = (station + 1) % Stations.Length; ShowUntil = Time.time + 4; tickerIndex = 0; tickerTimer = 0; }
            if (inCar && !wasInCar) ShowUntil = Time.time + 4; wasInCar = inCar;
            tickerTimer += Time.deltaTime; if (tickerTimer > 7) { tickerTimer = 0; tickerIndex++; }
            var music = G.audioDirector.MusicClip; var want = station == 3 ? null : station == 2 || music == null ? staticClip : music;
            if (src.clip != want && src.volume <= .005f) { src.clip = want; if (want) { src.time = want == music ? Random.Range(0, want.length * .6f) : 0; src.Play(); } }
            src.pitch = Stations[station].pitch;
            float target = inCar && src.clip == want && want != null && G.progress.ambience ? (want == staticClip ? .05f : .55f) : 0;
            src.volume = Mathf.MoveTowards(src.volume, target, Time.deltaTime * .8f);
        }
        bool wasInCar;
    }

    // ───────────────────────── Passenger job ─────────────────────────
    public class PassengerJob : MonoBehaviour
    {
        public Vector3? Target => OnDuty ? (carrying ? dropoff : pickup) : (Vector3?)null;
        public bool OnDuty; public string Status = ""; public int Fares; Vector3 pickup, dropoff; bool carrying; GameObject marker, waiting; float started; float earnedToday;

        void Update()
        {
            var G = Game.Instance; if (G == null || !G.Ready || !G.Started || G.Paused) return;
            if (G.player.InCar && !G.ui.PhotoActive && Input.GetKeyDown(KeyCode.T) && G.activities.Current == null) Toggle(G);
            if (!OnDuty) return;
            if (!G.player.InCar) { Status = "Get back in your vehicle to continue the shift."; return; }
            var car = G.car.transform.position; bool slow = Mathf.Abs(G.car.speed) < 3;
            if (!carrying)
            {
                Status = $"Pick up the passenger · {World.DistanceXZ(car, pickup):0} m";
                if (World.DistanceXZ(car, pickup) < 6 && slow)
                {
                    carrying = true; if (waiting) Destroy(waiting); dropoff = Spot(car, 160, 380); Move(dropoff); started = Time.time;
                    G.Notify($"\"{Pick(Board)}\" Drop them off at the green marker.", false);
                }
            }
            else
            {
                Status = $"Drop-off · {World.DistanceXZ(car, dropoff):0} m";
                if (World.DistanceXZ(car, dropoff) < 6 && slow)
                {
                    float distance = Vector3.Distance(pickup, dropoff), par = distance / 9f, time = Time.time - started;
                    int fare = 8 + Mathf.RoundToInt(distance / 12), tip = time < par ? 5 + Mathf.RoundToInt((par - time) * .5f) : 0;
                    int paid = G.progress.Pay(fare + tip); Fares++; G.progress.reputation += Fares % 4 == 0 ? 1 : 0;
                    G.Notify($"Fare Rs {fare}{(tip > 0 ? $" + tip Rs {tip}" : "")}{(paid > fare + tip ? $" (honest-work bonus → Rs {paid})" : "")}. \"{Pick(Thanks)}\"", true);
                    carrying = false; NextPickup(G);
                }
            }
        }

        public void Toggle(Game G)
        {
            OnDuty = !OnDuty;
            if (OnDuty) { Fares = 0; NextPickup(G); G.Notify("On duty! Pick up passengers at the green markers. Press T again to end the shift.", false); }
            else { if (marker) Destroy(marker); if (waiting) Destroy(waiting); carrying = false; Status = ""; G.Notify($"Shift over. {Fares} passenger{(Fares == 1 ? "" : "s")} today.", false); }
        }

        void NextPickup(Game G)
        {
            pickup = Spot(G.car.transform.position, 70, 200); Move(pickup);
            waiting = new GameObject("Passenger"); waiting.transform.position = pickup + Vector3.right * 2.6f;
            Figure.Create(waiting.transform, Random.value < .5f ? "resident_m" : "resident_f", new Color(.3f, .3f, .5f), new Color(.55f, .35f, .22f), new Color(.02f, .02f, .02f), null, G.models, 1.68f);
        }

        void Move(Vector3 at) { if (marker) Destroy(marker); marker = Marker.Create(at, new Color(.2f, .9f, .3f), 2.2f, 7); }

        /// <summary>A point on a road lane a sensible distance away.</summary>
        static Vector3 Spot(Vector3 from, float min, float max)
        {
            for (int tries = 0; tries < 30; tries++)
            {
                var a = Random.Range(0, Mathf.PI * 2); var p = from + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * Random.Range(min, max);
                if (Random.value < .5f) p.x = Mathf.Round(p.x / 80) * 80 - 2.2f; else p.z = Mathf.Round(p.z / 80) * 80 + 2.2f;
                if (p.x > -460 && p.x < 420 && p.z > -460 && p.z < 540) { p.y = World.TerrainHeight(p.x, p.z); return p; }
            }
            return new Vector3(80 - 2.2f, 0, 40);
        }
        static readonly string[] Board = { "Lakeside jane? Chito garnus, dai!", "Bazaar samma, please. I'm late for tuition.", "Hospital tira, bistarai hai, mero buba hunuhunchha.", "Ekdam chito! My bus to Kathmandu leaves soon." };
        static readonly string[] Thanks = { "Dhanyabad! Here, keep the change.", "Ramro chalaunu bhayo, dai!", "Phone number dinus, I'll call you tomorrow too.", "Thank you! The radio song was nice." };
        static string Pick(string[] a) => a[Random.Range(0, a.Length)];
    }

    // ───────────────────────── Samjhana: memories of 2000s Nepal ─────────────────────────
    public class Memories : MonoBehaviour
    {
        public static readonly (string id, Vector3 at, string title, string text)[] All =
        {
            ("tv", new Vector3(22, 0, 18), "Sunday film", "The only TV in the tole. Sunday afternoon Hindi film, twenty people in one living room, the antenna turned by hand whenever the picture snowed."),
            ("nokia", new Vector3(104, 0, 30), "The first mobile", "A Nokia with a torch on top. Rs 100 recharge cards scratched with a coin, and missed calls as a language of their own."),
            ("waiwai", new Vector3(-56, 0, 116), "Crushed noodles", "Instant noodles crushed in the packet, masala shaken in, eaten on the walk home from school, crumbs everywhere."),
            ("slc", new Vector3(184, 0, 96), "Result day", "SLC results printed in the newspaper. Whole families up before dawn, fingers running down columns of symbol numbers."),
            ("candle", new Vector3(-216, 0, 30), "Homework by candlelight", "Load shedding at 6 PM. Homework by candlelight, wax dripping on the copy, and the cheer when the power came back."),
            ("cricket", new Vector3(80, 0, 200), "Bandh-day cricket", "No traffic on bandh days, so the main road became a cricket pitch. A brick for the wicket, a plastic ball, endless arguments about LBW."),
            ("cassette", new Vector3(344, 0, 96), "Pencil rewind", "Cassette tapes rewound with a pencil to save the walkman batteries. Every tape had a favourite song worn thin."),
            ("kite", new Vector3(40, 0, 370), "Glass manja", "Dashain kites over Sarangkot. Manja coated with crushed glass, fingers cut, still shouting 'chet!' at every fallen kite."),
            ("deusi", new Vector3(-120, 0, 186), "Deusi-bhailo", "Tihar nights going house to house singing deusi-bhailo, paid in sel roti, fruit and a few crisp rupees."),
            ("videohall", new Vector3(260, 0, 40), "Video hall", "Rs 10 at the video hall. A ceiling fan that barely turned, a film everyone already knew by heart."),
            ("penpal", new Vector3(-150, 0, 120), "Pen pals", "Greeting cards from the bookshop, pen-pal letters with stickers, and waiting weeks for a reply."),
            ("microbus", new Vector3(0, 0, -150), "Standing room only", "A whole class squeezed into one micro bus, the conductor hanging out of the door shouting the route."),
            ("hi5", new Vector3(140, 0, 72), "Profile songs", "Rs 20 an hour at the cyber. Messenger chats, a social profile with a song that played automatically, and printing photos you could not afford."),
            ("lattu", new Vector3(56, 0, 22), "The iron tip", "Lattu tournaments in the courtyard. The iron tip sharpened on a stone, the string wound so tight it creaked."),
            ("junkiri", new Vector3(-90, 0, 300), "A jar of light", "Junkiri caught in a glass jar by the paddy fields, watched for an hour, then let go before bedtime."),
            ("kulfi", new Vector3(400, 0, 60), "The bell of the kulfi man", "A bicycle bell and 'Kulfi! Kulfi!' on a hot afternoon. Two rupees, and it melted down your wrist.")
        };
        readonly Dictionary<string, GameObject> shown = new Dictionary<string, GameObject>(); Mesh mesh;
        public (string title, string text, float until) Popup;

        void Update()
        {
            var G = Game.Instance; if (G == null || !G.Ready || !G.Started) return;
            var p = G.player.transform.position;
            if (mesh == null) { var g = new MeshBuilder(); g.Sphere(0, 0, 0, .22f, .22f, .22f, new Color(1, .82f, .35f), new Surface(.4f, 0, 3, 1), 10); g.Box(0, 0, 0, .5f, .03f, .03f, new Color(1, .9f, .5f), new Surface(.4f, 0, 3, 1)); g.Box(0, 0, 0, .03f, .5f, .03f, new Color(1, .9f, .5f), new Surface(.4f, 0, 3, 1)); mesh = g.Build("Samjhana"); }
            foreach (var (id, at, title, text) in All)
            {
                if (G.progress.memories.Contains(id)) continue;
                float d = World.DistanceXZ(p, at.x, at.z); shown.TryGetValue(id, out var go);
                if (d < 70 && go == null) { go = new GameObject($"Samjhana {id}"); go.transform.SetParent(transform, false); go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = Materials.VertexLit; shown[id] = go; }
                else if (d >= 80 && go != null) { Destroy(go); shown.Remove(id); continue; }
                if (go == null) continue;
                go.transform.position = new Vector3(at.x, World.TerrainHeight(at.x, at.z) + 1.3f + Mathf.Sin(Time.time * 2 + at.x) * .15f, at.z); go.transform.Rotate(0, 90 * Time.deltaTime, 0);
                if (d < 1.8f && !G.player.InCar)
                {
                    G.progress.memories.Add(id); Destroy(go); shown.Remove(id); int paid = G.progress.Pay(5); G.progress.reputation += 1;
                    Popup = (title, text, Time.time + 9); G.Notify($"Samjhana found ({G.progress.memories.Count}/{All.Length}): {title}  +Rs {paid}", true); G.Save();
                    if (G.progress.memories.Count == All.Length) G.Notify("Every memory of the valley found. Khusi lagyo!", true);
                }
            }
        }
    }

    // ───────────────────────── Lok Katha: folk tales at heritage sites ─────────────────────────
    // Black Myth: Wukong builds its world on local folklore and real heritage architecture. Here the
    // valley's temples, stupa and chautari carry carved stone tablets (shila-lekh) that tell
    // Nepali legends. A tall bamboo pole of prayer flags marks each one so it can be spotted from afar.
    public class Folklore : MonoBehaviour
    {
        public static readonly (string id, Vector3 at, string title, string text)[] All =
        {
            ("talbarahi", new Vector3(404, 0, 50), "The flood of Tal Barahi",
             "Long ago a goddess came to a rich village in the shape of a ragged old woman, asking for food during a feast. Everyone turned her away except one poor couple, who shared what little they had. She warned them to run to the hill that night. By morning the rain had drowned the village, and the water never left. That is Phewa Tal, and her temple still sits on its island."),
            ("kasthamandap", new Vector3(36, 0, 56), "A temple from one tree",
             "The old pavilion at the heart of Kathmandu was said to be built from the wood of a single sal tree, given by a god who had been caught in disguise at a chariot festival. The city took its name from it: kastha, wood; mandap, pavilion. The pagodas of every Durbar Square remember that story."),
            ("jyajima", new Vector3(-110, 0, 204), "The poultry woman's stupa",
             "A poor woman who kept hens asked the king for as much land as a buffalo hide could cover, to build a stupa. He laughed and agreed. She cut the hide into one thin strip and laid it in a great circle. The king had given his word, so the stupa rose. Turn the wheels clockwise when you pass, as she would have."),
            ("lakhey", new Vector3(96, 0, 22), "Lakhey, the dancing demon",
             "Lakhey was a demon who fell in love with a girl of the city and took human form to be near her. When he was found out, the people did not drive him away; they asked him to guard their children instead. At Indra Jatra he still dances through the streets with his red face and wild hair, and children run screaming and laughing."),
            ("gaijatra", new Vector3(-142, 0, 114), "Why Gai Jatra makes you laugh",
             "A queen could not stop grieving for her son. The king asked every family that had lost someone that year to parade a cow through the city, so she would see she was not alone, and promised a reward to anyone who could make her smile. Jokes, costumes and satire filled the streets, and she laughed. Gai Jatra has been a day of mourning and mischief ever since."),
            ("chautari", new Vector3(60, 0, 68), "The pipal and the bar",
             "Every chautari is a gift. A family builds the stone platform in memory of someone they lost, so tired porters can rest their doko in the shade. The pipal and the bar trees planted together are married in a small ceremony, and nobody may cut them. Sit for a while: that is what it was built for."),
            ("machhapuchhre", new Vector3(48, 0, 346), "The mountain no one stands on",
             "Machhapuchhre, the fish-tail peak above Pokhara, is sacred to Shiva. A climbing team in 1957 stopped just below the summit out of respect, and the mountain was closed to climbers soon after. Its top has never been stood on. People say it is better that way."),
            ("bhimsen", new Vector3(-208, 0, 48), "Bhimsen of the traders",
             "Bhimsen, the strongest of the Pandava brothers, became the god of traders and travelling merchants. Newar shopkeepers from Kathmandu to Lhasa kept his image near the scales, so that every weight would be honest and every journey would come home safely."),
        };

        public (string title, string text, float until) Popup;
        readonly List<GameObject> stones = new List<GameObject>();

        public void Build()
        {
            var stone = new Color(.42f, .41f, .38f); var bamboo = new Color(.62f, .55f, .36f);
            foreach (var (id, at, title, _) in All)
            {
                float y = World.TerrainHeight(at.x, at.z);
                var g = new MeshBuilder();
                g.Box(0, .12f, 0, .7f, .12f, .45f, stone * .85f, new Surface(.95f));            // plinth
                g.Box(0, .85f, 0, .45f, .65f, .1f, stone, new Surface(.9f));                    // carved slab
                g.Pyramid(0, 1.5f, 0, .45f, .3f, .1f, stone * 1.1f, new Surface(.9f));           // gable top
                g.Box(0, 1.05f, -.11f, .32f, .02f, .01f, stone * .6f, new Surface(.9f));        // inscription lines
                g.Box(0, .9f, -.11f, .32f, .02f, .01f, stone * .6f, new Surface(.9f));
                g.Box(0, .75f, -.11f, .32f, .02f, .01f, stone * .6f, new Surface(.9f));
                g.Cylinder(1.4f, 4.5f, .6f, .06f, 4.5f, bamboo, new Surface(.7f), 6);          // tall lingdar pole
                g.PrayerFlags(new Vector3(1.4f, 8.8f, .6f), new Vector3(-1.6f, .3f, 2.2f), .4f, .45f);
                g.PrayerFlags(new Vector3(1.4f, 8.8f, .6f), new Vector3(3.8f, .3f, -1.2f), .4f, .45f);
                var go = new GameObject($"Lok Katha {id}"); go.transform.SetParent(transform, false); go.transform.position = new Vector3(at.x, y, at.z);
                go.AddComponent<MeshFilter>().sharedMesh = g.Build(id); go.AddComponent<MeshRenderer>().sharedMaterial = Materials.VertexLit;
                stones.Add(go);
            }
        }

        void Update()
        {
            var G = Game.Instance; if (G == null || !G.Ready || !G.Started || G.player.InCar) return;
            var p = G.player.transform.position;
            foreach (var (id, at, title, text) in All)
            {
                if (World.DistanceXZ(p, at.x, at.z) > 2.6f) continue;
                if (Time.time < Popup.until && Popup.title == title) continue;
                Popup = (title, text, Time.time + 16);
                if (!G.progress.tales.Contains(id))
                {
                    G.progress.tales.Add(id); int paid = G.progress.Pay(10); G.progress.reputation += 1;
                    G.Notify($"Lok Katha learned ({G.progress.tales.Count}/{All.Length}): {title}  +Rs {paid}", true); G.Save();
                    if (G.progress.tales.Count == All.Length) G.Notify("You know every tale of the valley. Hajurba would be proud.", true);
                }
            }
        }
    }

    // ───────────────────────── Explorer guidance: follow the kite ─────────────────────────
    // Elden Ring guides players with landmarks rather than markers, which players read as
    // freedom (Farraj & Ostrowska, Uppsala 2023). In Explorer mode the radar and distances go
    // away; a red changa flies high over the current objective, and its string leads the eye down to it.
    public class GuideKite : MonoBehaviour
    {
        Transform kite, line; float sway;

        void Start()
        {
            var red = new Color(.8f, .1f, .08f); var gold = new Color(.95f, .85f, .3f); var s = new Surface(.6f, 0, .35f, 1);
            // Diamond sail (a square turned 45 degrees) with its bamboo cross, and a paper tail hanging below.
            var sail = new MeshBuilder(); sail.Box(0, 0, 0, .8f, .8f, .03f, red, s); sail.Box(0, 0, -.04f, 1.1f, .04f, .02f, gold, s); sail.Box(0, 0, -.04f, .04f, 1.1f, .02f, gold, s);
            var tail = new MeshBuilder(); for (int i = 0; i < 5; i++) tail.Box(.12f * Mathf.Sin(i), -1.35f - i * .55f, 0, .12f, .2f, .02f, i % 2 == 0 ? gold : red, s);
            kite = new GameObject("Changa").transform; kite.SetParent(transform, false);
            Piece(kite, sail.Build("Changa")).localRotation = Quaternion.Euler(0, 0, 45); Piece(kite, tail.Build("Changa tail"));
            var l = new MeshBuilder(); l.Box(0, .5f, 0, .5f, .5f, .5f, new Color(.95f, .93f, .85f), new Surface(.8f, 0, .5f, 1));
            line = new GameObject("Manja").transform; line.SetParent(transform, false); Piece(line, l.Build("Manja"));
        }

        static Transform Piece(Transform parent, Mesh mesh)
        {
            var go = new GameObject(mesh.name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = Materials.VertexLit; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        void LateUpdate()
        {
            var G = Game.Instance; if (G == null || !G.Started || kite == null) return;
            var goal = G.ui.CurrentGoal(); var p = G.player.transform.position;
            bool show = G.progress.guidance == 1 && goal.HasValue && World.DistanceXZ(p, goal.Value.x, goal.Value.z) > 10;
            if (kite.gameObject.activeSelf != show) { kite.gameObject.SetActive(show); line.gameObject.SetActive(show); }
            if (!show) return;
            var ground = goal.Value; ground.y = World.TerrainHeight(ground.x, ground.z);
            sway += Time.deltaTime;
            var top = ground + new Vector3(Mathf.Sin(sway * .7f) * 4, 24 + Mathf.Sin(sway * 1.3f) * 2, Mathf.Cos(sway * .5f) * 3);
            kite.position = top; var cam = Camera.main ? Camera.main.transform.position : p;
            var face = cam - top; face.y = 0; if (face.sqrMagnitude > .01f) kite.rotation = Quaternion.LookRotation(-face) * Quaternion.Euler(0, 0, Mathf.Sin(sway * 2.1f) * 12);
            // The manja: a thin string from the ground to the kite, thicker the farther away you are so it stays visible.
            var dir = top - ground; float thick = Mathf.Clamp(Vector3.Distance(cam, ground) * .0016f, .03f, .5f);
            line.position = ground; line.up = dir.normalized; line.localScale = new Vector3(thick, dir.magnitude, thick);
            kite.localScale = Vector3.one * Mathf.Clamp(Vector3.Distance(cam, top) * .012f, 1.5f, 6);
        }
    }
}

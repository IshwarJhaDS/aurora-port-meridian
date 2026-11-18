// 2000s pastimes: lattu, chungi keep-ups, luki-chhupi (hide and seek), changa chet
// (kite fighting), the Dashain ping swing, Hari's salon, cassette tapes, the cyber
// cafe and catching junkiri (fireflies). Each one is a small playable minigame.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PhewaValley
{
    public abstract class Activity
    {
        public string id; public Vector3 position; public float radius = 2.4f;
        public virtual string Prompt => id;
        public virtual bool Available => true;
        /// <summary>When true the player cannot walk while playing.</summary>
        public virtual bool Freeze => true;
        protected Game G => Game.Instance; protected Progress P => G.progress;
        protected Activities Owner;
        public void Attach(Activities owner) => Owner = owner;
        public abstract void Begin();
        public virtual void Tick(float dt) { }
        public abstract void Draw(float W, float H);
        public virtual void Cancel() => End();
        protected void End() { G.player.figure.ArmOut = 0; Owner.Current = null; }
        protected void Panel(float W, float H, string eyebrow, string title, float height = 330)
        {
            var r = new Rect(W / 2 - 380, H - height - 40, 760, height); Retro.PaperPanel(r);
            DevaText.Label(new Rect(r.x + 28, r.y + 26, 700, 22), eyebrow, Retro.Eyebrow); GUI.Label(new Rect(r.x + 28, r.y + 46, 700, 40), title, Retro.Heading);
            DevaText.Label(new Rect(r.xMax - 260, r.y + 26, 232, 22), "Esc · छोड्नुहोस्", new GUIStyle(Retro.Eyebrow) { alignment = TextAnchor.UpperRight });
        }
        protected Rect Inner(float W, float H, float height = 330) => new Rect(W / 2 - 352, H - height + 50, 704, height - 110);
    }

    public class Activities : MonoBehaviour
    {
        public readonly List<Activity> all = new List<Activity>();
        public Activity Current;
        public readonly List<Figure> kids = new List<Figure>(); public readonly string[] kidNames = { "Bishal", "Pemba", "Sunita", "Rabin" };
        public Vector3 KidBase = new Vector3(23, 0, 39);
        public FireflyCatch fireflies; public Festivals festivals;
        GameObject tapeSalon;

        public void Init()
        {
            // Neighbourhood kids who hang around Durbar Square after school.
            var outfits = new[] { new Color(.1f, .25f, .5f), new Color(.55f, .1f, .1f), new Color(.15f, .4f, .2f), new Color(.5f, .4f, .1f) };
            for (int i = 0; i < kidNames.Length; i++)
            {
                var go = new GameObject($"Kid {kidNames[i]}"); go.transform.SetParent(transform, false); go.transform.position = KidSpot(i);
                var f = Figure.Create(go.transform, null, outfits[i], new Color(.56f, .35f, .22f), new Color(.02f, .02f, .02f), i == 0 ? new Color(.4f, .1f, .09f) : (Color?)null, null, 1.18f);
                go.transform.rotation = Quaternion.Euler(0, 90 + i * 40, 0); kids.Add(f);
            }
            Add(new LattuGame { id = "lattu", position = new Vector3(25, 0, 36) });
            Add(new ChungiGame { id = "chungi", position = new Vector3(30, 0, 33) });
            Add(new HideAndSeek { id = "hide", position = new Vector3(20, 0, 43) });
            Add(new KiteFight { id = "kite", position = new Vector3(34, World.TerrainHeight(34, 352), 352) });
            Add(new KiteFight { id = "kite-dashain", position = new Vector3(30, 0, 24), dashainOnly = true });
            Add(new PingSwing { id = "ping", position = new Vector3(18, 0, 30) });
            Add(new Salon { id = "salon", position = new Vector3(124, 0, 65) });
            Add(new CassetteTape { id = "tape", position = new Vector3(93.4f, 0, 56.6f) });
            Add(new CyberCafe { id = "cyber", position = new Vector3(140, 0, 65.5f) });
            fireflies = gameObject.AddComponent<FireflyCatch>(); fireflies.Attach(this); all.Add(fireflies.activity);
            festivals = gameObject.AddComponent<Festivals>();
        }

        public Vector3 KidSpot(int i) => KidBase + new Vector3(Mathf.Cos(i * 1.7f) * 2.2f, 0, Mathf.Sin(i * 1.7f) * 2.2f);
        void Add(Activity a) { a.Attach(this); all.Add(a); }

        public Activity Near(Vector3 p)
        {
            if (Current != null) return null;
            Activity best = null; float bestD = float.MaxValue;
            foreach (var a in all) { if (!a.Available) continue; float d = World.DistanceXZ(p, a.position); if (d < a.radius && d < bestD) { bestD = d; best = a; } }
            return best;
        }

        public void Play(Activity a) { if (Current != null || a == null) return; Current = a; a.Begin(); }

        void Update()
        {
            var G = Game.Instance; if (G == null || !G.Ready) return;
            if (Current != null && !G.Paused) Current.Tick(Time.deltaTime);
            // Kids idle and watch the player; hide-and-seek moves them itself.
            if (!(Current is HideAndSeek)) for (int i = 0; i < kids.Count; i++)
            {
                var t = kids[i].transform.parent; var look = G.player.transform.position - t.position; look.y = 0;
                if (look.magnitude < 12) t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(look), Time.deltaTime * 2);
                kids[i].Speed = 0;
            }
        }

        public void DrawCurrent(float W, float H) => Current?.Draw(W, H);

        /// <summary>A small salon cassette that plays while the barber works.</summary>
        public void SalonTape(bool on) { if (on && tapeSalon == null) tapeSalon = Cassette.Play(new Vector3(125.6f, 1.4f, 60.4f)); else if (!on && tapeSalon) { Destroy(tapeSalon); tapeSalon = null; } }
    }

    /// <summary>Plays the music track through a lo-fi "tape" filter from a 3D point.</summary>
    public static class Cassette
    {
        public static GameObject Play(Vector3 at)
        {
            var clip = Game.Instance.audioDirector.MusicClip; if (clip == null) return null;
            var go = new GameObject("Tape"); go.transform.position = at;
            var s = go.AddComponent<AudioSource>(); s.clip = clip; s.loop = true; s.spatialBlend = 1; s.minDistance = 2; s.maxDistance = 22; s.volume = .9f; s.time = Random.Range(0, clip.length * .5f); s.Play();
            go.AddComponent<AudioLowPassFilter>().cutoffFrequency = 3800; go.AddComponent<AudioHighPassFilter>().cutoffFrequency = 180;
            go.AddComponent<TapeWobble>(); return go;
        }
    }
    public class TapeWobble : MonoBehaviour { AudioSource s; void Awake() => s = GetComponent<AudioSource>(); void Update() { if (s) s.pitch = 1 + Mathf.Sin(Time.time * .9f) * .006f + Mathf.Sin(Time.time * 7.1f) * .002f; } }

    // ───────────────────────── Lattu ─────────────────────────
    public class LattuGame : Activity
    {
        enum Phase { Wind, Spin, Result } Phase phase; float meter, held, spinTime, spinLeft, rival, quality, rate; bool winding; GameObject top; string result;
        public override string Prompt => "Play lattu with Bishal  ·  लट्टु";
        public override void Begin()
        {
            phase = Phase.Wind; meter = 0; held = 0; winding = false; rival = Random.Range(6f, 11f); result = null;
            Face(G.player.transform, position + Vector3.forward * 2);
        }
        public override void Tick(float dt)
        {
            if (phase == Phase.Wind)
            {
                if (Input.GetKeyDown(KeyCode.Space)) G.player.figure.Action("chop", .6f, 1.9f, .9f, Act.WhileMoving); // raise the lattu overhead
                if (Input.GetKey(KeyCode.Space)) { winding = true; held += dt; meter = Mathf.PingPong(held * 1.25f, 1); G.player.figure.ArmOut = 40 + meter * 40; }
                else if (winding) Throw();
            }
            else if (phase == Phase.Spin)
            {
                spinLeft -= dt; spinTime += dt; float life = Mathf.Clamp01(spinLeft / 2.5f);
                if (top)
                {
                    rate = Mathf.Lerp(120, 1800 * (.4f + quality), Mathf.Clamp01(spinLeft / 4));
                    top.transform.Rotate(0, rate * dt, 0, Space.Self);
                    float wobble = (1 - life) * 18; top.transform.parent.localRotation = Quaternion.Euler(Mathf.Sin(spinTime * 9) * wobble, 0, Mathf.Cos(spinTime * 9) * wobble);
                }
                if (spinLeft <= 0) Finish();
            }
            else if (phase == Phase.Result && Input.GetKeyDown(KeyCode.Space)) { if (top) Object.Destroy(top.transform.parent.gameObject); Begin(); }
        }
        void Throw()
        {
            const float zone = .8f; quality = Mathf.Clamp01(1 - Mathf.Abs(meter - zone) * 3.2f);
            spinLeft = 2.5f + quality * 11 + Random.Range(0, 1.5f); spinTime = 0; phase = Phase.Spin; G.player.figure.ArmOut = 0;
            G.player.figure.Action("chop", 1.75f, 3.3f, 1.7f, Act.WhileMoving); // the overhand throw, whipping the string
            var pivot = new GameObject("Lattu"); pivot.transform.position = G.player.transform.position + G.player.transform.forward * 1.3f + Vector3.up * .01f;
            top = G.models.Instantiate("lattu", pivot.transform, pivot.transform.position, 0, .12f);
            if (!top) { var g = new MeshBuilder(); g.Pyramid(0, 0, 0, .001f, .06f, .001f, new Color(.4f, .4f, .4f), new Surface(.4f, .8f), .05f, .05f); g.Sphere(0, .09f, 0, .06f, .045f, .06f, new Color(.7f, .1f, .1f)); g.Cylinder(0, .14f, 0, .012f, .02f, new Color(.3f, .2f, .1f)); top = new GameObject("lattu"); top.transform.SetParent(pivot.transform, false); top.AddComponent<MeshFilter>().sharedMesh = g.Build("Lattu"); top.AddComponent<MeshRenderer>().sharedMaterial = Materials.VertexLit; }
        }
        void Finish()
        {
            phase = Phase.Result; if (top) top.transform.parent.localRotation = Quaternion.Euler(72, Random.Range(0, 360), 0);
            bool win = spinTime > rival; P.lattuBestSeconds = Mathf.Max(P.lattuBestSeconds, spinTime);
            if (win) { P.lattuBest++; bool paid = P.DailyReward("lattu", 25, 1); result = $"Your lattu spun {spinTime:0.0}s, Bishal's only {rival:0.0}s. Jityo!{(paid ? "  +Rs 25" : "")}"; G.Notify("Lattu champion of the tole!", true); }
            else result = $"Your lattu spun {spinTime:0.0}s. Bishal's spun {rival:0.0}s. Pheri khelau?";
        }
        public override void Cancel() { if (top) Object.Destroy(top.transform.parent.gameObject); base.Cancel(); }
        public override void Draw(float W, float H)
        {
            Panel(W, H, "STREET GAME · लट्टु", "Lattu with Bishal"); var r = Inner(W, H);
            GUI.Label(new Rect(r.x, r.y, r.width, 26), $"Bishal's best today: {rival:0.0} seconds  ·  your record {P.lattuBestSeconds:0.0}s", Retro.Body);
            if (phase == Phase.Wind)
            {
                GUI.Label(new Rect(r.x, r.y + 36, r.width, 26), "Hold SPACE to wind the string, release in the green to throw.", Retro.Small);
                Retro.Meter(new Rect(r.x, r.y + 76, r.width, 34), meter, .72f, .88f);
            }
            else if (phase == Phase.Spin) { DevaText.Label(new Rect(r.x, r.y + 40, r.width, 60), $"घुम्दैछ…  {spinTime:0.0}s", Retro.Title); }
            else { GUI.Label(new Rect(r.x, r.y + 36, r.width, 60), result, Retro.Body); GUI.Label(new Rect(r.x, r.y + 110, r.width, 26), "SPACE to play again · Esc to leave", Retro.Small); }
        }
        static void Face(Transform t, Vector3 at) { var d = at - t.position; d.y = 0; if (d.sqrMagnitude > .01f) t.rotation = Quaternion.LookRotation(d); }
    }

    // ───────────────────────── Chungi ─────────────────────────
    public class ChungiGame : Activity
    {
        GameObject ball; Vector3 offset; float y, vy, flash; int count; bool playing, over;
        public override string Prompt => "Kick chungi (keep it up)  ·  चुङ्गी";
        public override void Begin()
        {
            if (!ball)
            {
                var g = new MeshBuilder(); var cols = new[] { new Color(.8f, .1f, .1f), new Color(.95f, .75f, .1f), new Color(.1f, .5f, .2f), new Color(.1f, .2f, .6f) };
                for (int i = 0; i < 7; i++) g.Sphere(0, 0, 0, .075f + i * .0015f, .075f - i * .002f, .075f - i * .0005f, cols[i % 4], new Surface(.6f), 10);
                ball = new GameObject("Chungi"); ball.AddComponent<MeshFilter>().sharedMesh = g.Build("Chungi"); ball.AddComponent<MeshRenderer>().sharedMaterial = Materials.VertexLit;
            }
            count = 0; playing = false; over = false; y = 1.1f; vy = 0; offset = Vector3.zero;
        }
        public override void Tick(float dt)
        {
            var feet = G.player.transform.position + G.player.transform.forward * .45f;
            void Kick() => G.player.figure.Action(Random.value < .5f ? "front_kick_02" : "front_kick_01", .25f, .95f, 2.1f, Act.WhileMoving | Act.PinXZ);
            if (!playing && !over) { if (Input.GetKeyDown(KeyCode.Space)) { playing = true; vy = 3.5f; Kick(); } }
            else if (playing)
            {
                vy -= 9.81f * .8f * dt; y += vy * dt; flash -= dt;
                if (Input.GetKeyDown(KeyCode.Space) && y < .75f && vy < 0) { vy = Random.Range(3.4f, 4.3f); count++; flash = .25f; offset = Vector3.ClampMagnitude(offset + new Vector3(Random.Range(-.08f, .08f), 0, Random.Range(-.08f, .08f)), .25f); G.player.figure.ArmOut = 25; Kick(); }
                if (y <= .06f) { playing = false; over = true; y = .07f; Done(); }
            }
            else if (over && Input.GetKeyDown(KeyCode.Space)) Begin();
            G.player.figure.ArmOut = Mathf.MoveTowards(G.player.figure.ArmOut, 0, dt * 80);
            ball.transform.position = feet + offset + Vector3.up * y; ball.transform.Rotate(300 * dt, 120 * dt, 0);
        }
        void Done()
        {
            P.chungiBest = Mathf.Max(P.chungiBest, count);
            if (count >= 10) { bool paid = P.DailyReward("chungi", 15, 1); G.Notify($"{count} chungi kicks! The kids are impressed.{(paid ? "  +Rs 15" : "")}", true); }
        }
        public override void Cancel() { if (ball) Object.Destroy(ball); base.Cancel(); }
        public override void Draw(float W, float H)
        {
            Panel(W, H, "STREET GAME · चुङ्गी", "Chungi keep-ups", 300); var r = Inner(W, H, 300);
            GUI.Label(new Rect(r.x, r.y, r.width, 26), "Press SPACE as the chungi falls near your foot. Ten in a row earns respect.", Retro.Small);
            GUI.Label(new Rect(r.x, r.y + 30, 300, 70), Retro.Devanagari(count), new GUIStyle(Retro.Title) { fontSize = 60, normal = { textColor = flash > 0 ? Retro.Crimson : Retro.Ink } });
            GUI.Label(new Rect(r.x + 320, r.y + 44, 380, 60), over ? $"Dropped at {count}.  Best: {P.chungiBest}.\nSPACE to go again." : playing ? $"Kicks: {count}   ·   best {P.chungiBest}" : "SPACE to toss it up.", Retro.Body);
        }
    }

    // ───────────────────────── Luki-chhupi ─────────────────────────
    public class HideAndSeek : Activity
    {
        static readonly Vector3[] Spots = { new Vector3(40, 0, 54), new Vector3(54, 0, 40), new Vector3(50, 0, 70), new Vector3(16, 0, 15), new Vector3(63, 0, 18), new Vector3(15, 0, 64.5f), new Vector3(26, 0, 60), new Vector3(29, 0, 54.5f), new Vector3(62.5f, 0, 58), new Vector3(9, 0, 46), new Vector3(70, 0, 30), new Vector3(44, 0, 12) };
        readonly List<int> hidden = new List<int>(); readonly HashSet<int> found = new HashSet<int>(); float counting, timeLeft; Vector3[] targets; bool over;
        public override string Prompt => "Play luki-chhupi (hide & seek)  ·  लुकिछिपी";
        public override bool Freeze => counting > 0 || over;
        public override void Begin()
        {
            hidden.Clear(); found.Clear(); over = false; counting = 5; timeLeft = 100;
            var spots = Spots.OrderBy(_ => Random.value).Take(3).ToArray(); targets = new Vector3[Owner.kids.Count];
            for (int i = 1; i < Owner.kids.Count; i++) { hidden.Add(i); targets[i] = spots[i - 1]; }
            G.player.transform.rotation = Quaternion.LookRotation(Vector3.left); G.rig.yaw = 270;
        }
        public override void Tick(float dt)
        {
            if (over) { if (Input.GetKeyDown(KeyCode.Space)) Cancel(); return; }
            for (int k = 0; k < hidden.Count; k++)
            {
                int i = hidden[k]; var t = Owner.kids[i].transform.parent; var goal = found.Contains(i) ? Owner.KidSpot(i) : targets[i];
                var d = goal - t.position; d.y = 0; bool arrived = d.magnitude < .3f;
                if (!arrived) { t.position += d.normalized * Mathf.Min(d.magnitude, 5.5f * dt); t.rotation = Quaternion.LookRotation(d); }
                Owner.kids[i].Speed = arrived ? 0 : 5.5f;
                t.localScale = new Vector3(1, !found.Contains(i) && arrived && counting <= 0 ? .62f : 1, 1); // crouched in the hiding place
            }
            if (counting > 0) { counting -= dt; return; }
            timeLeft -= dt;
            if (Input.GetKeyDown(KeyCode.E))
                foreach (int i in hidden) if (!found.Contains(i) && World.DistanceXZ(G.player.transform.position, Owner.kids[i].transform.parent.position) < 2.6f) { found.Add(i); G.Notify($"Bhetiyo! Found {Owner.kidNames[i]}.", false); break; }
            if (found.Count == hidden.Count) { over = true; bool paid = P.DailyReward("hide", 30, 1); G.Notify($"You found everyone in {100 - timeLeft:0}s!{(paid ? "  +Rs 30" : "")}", true); }
            else if (timeLeft <= 0) { over = true; foreach (int i in hidden) found.Add(i); G.Notify("Time up! The kids come out laughing.", false); }
        }
        public override void Cancel() { foreach (int i in hidden) { var t = Owner.kids[i].transform.parent; t.position = Owner.KidSpot(i); t.localScale = Vector3.one; } base.Cancel(); }
        public override void Draw(float W, float H)
        {
            var r = new Rect(W / 2 - 260, 120, 520, 120); Retro.PaperPanel(r);
            DevaText.Label(new Rect(r.x + 24, r.y + 22, 470, 22), "LUKI-CHHUPI · लुकिछिपी", Retro.Eyebrow);
            if (counting > 0) GUI.Label(new Rect(r.x + 24, r.y + 44, 470, 60), $"Eyes closed… counting {Retro.Devanagari(Mathf.CeilToInt(10 - counting * 2))}", Retro.Heading);
            else if (over) GUI.Label(new Rect(r.x + 24, r.y + 44, 470, 60), $"Found {found.Count}/{hidden.Count}. SPACE to finish.", Retro.Heading);
            else GUI.Label(new Rect(r.x + 24, r.y + 44, 470, 60), $"Find them! {found.Count}/{hidden.Count} found · {Mathf.CeilToInt(timeLeft)}s\n<size=15>Walk up close and press E. Behind trees, the temple, the chautari…</size>", Retro.Body);
        }
    }

    // ───────────────────────── Changa chet (kite fight) ─────────────────────────
    public class KiteFight : Activity
    {
        public bool dashainOnly; float tension, cut, snapTimer, gust; bool over, won; GameObject mine, rival; LineRenderer myLine, rivalLine; Vector3 anchor, rivalHand;
        public override string Prompt => "Fly a changa (kite fight)  ·  चङ्गा चेट";
        public override bool Available => !dashainOnly || Owner.festivals.Today == "Dashain";
        public override void Begin()
        {
            tension = .5f; cut = 0; over = false; won = false; snapTimer = 0;
            var p = G.player.transform; anchor = p.position + p.forward * 26 + Vector3.up * 22; rivalHand = p.position + p.right * 30 + p.forward * 18 + Vector3.up * 9;
            mine = Kite(new Color(.75f, .1f, .1f)); rival = Kite(new Color(.1f, .3f, .7f));
            myLine = Line(mine); rivalLine = Line(rival); G.player.figure.ArmOut = 70; G.rig.pitch = -6; G.rig.distance = 9;
        }
        GameObject Kite(Color c)
        {
            var go = G.models.Instantiate("kite_changa", null, Vector3.zero, 0, 1.2f);
            if (!go) { var g = new MeshBuilder(); var top = new Vector3(0, .7f, 0); var l = new Vector3(-.5f, 0, 0); var rr = new Vector3(.5f, 0, 0); var b = new Vector3(0, -.7f, 0); g.Facing(new Vector3(0, 0, -1), top, rr, b, l, c, new Surface(.8f)); g.Facing(new Vector3(0, 0, 1), top, rr, b, l, c, new Surface(.8f)); go = new GameObject("Changa"); go.AddComponent<MeshFilter>().sharedMesh = g.Build("Changa"); go.AddComponent<MeshRenderer>().sharedMaterial = Materials.VertexLit; }
            return go;
        }
        LineRenderer Line(GameObject on) { var lr = on.AddComponent<LineRenderer>(); lr.widthMultiplier = .02f; lr.positionCount = 2; lr.material = new Material(Shader.Find("Sprites/Default")); lr.startColor = lr.endColor = new Color(.95f, .92f, .85f); return lr; }
        public override void Tick(float dt)
        {
            if (over) { if (Input.GetKeyDown(KeyCode.Space)) { Cleanup(); Begin(); } return; }
            gust = Mathf.PerlinNoise(Time.time * .35f, 3.1f) - .5f;
            tension += (Input.GetKey(KeyCode.Space) ? .55f : -.38f) * dt + gust * .5f * dt; tension = Mathf.Clamp01(tension);
            bool inBand = tension > .45f && tension < .78f; if (inBand) cut += dt / 9;
            snapTimer = tension > .96f ? snapTimer + dt : 0;
            float t = Time.time;
            var myPos = anchor + new Vector3(Mathf.Sin(t * .7f) * 3 - 3, (tension - .5f) * 10 + Mathf.Sin(t * 1.3f) * 1.5f, 0);
            var rivalPos = anchor + new Vector3(3 + Mathf.Sin(t * .9f + 1) * 2.5f, Mathf.Cos(t * .8f) * 2 + 1, 2 - cut * 2);
            mine.transform.position = myPos; mine.transform.rotation = Quaternion.LookRotation(G.player.transform.position - myPos) * Quaternion.Euler(0, 0, Mathf.Sin(t * 2) * 15);
            rival.transform.position = rivalPos; rival.transform.rotation = Quaternion.LookRotation(rivalHand - rivalPos) * Quaternion.Euler(0, 0, Mathf.Sin(t * 2.4f) * 20);
            var hand = G.player.transform.position + Vector3.up * 1.6f; myLine.SetPositions(new[] { hand, myPos }); rivalLine.SetPositions(new[] { rivalHand, rivalPos });
            if (cut >= 1) { over = true; won = true; P.kitesCut++; bool paid = P.DailyReward("kite", 40, 2); G.Notify($"Changa chet! You cut the blue kite.{(paid ? "  +Rs 40" : "")}", true); rival.AddComponent<FallingKite>(); rivalLine.enabled = false; }
            else if (snapTimer > .35f) { over = true; G.Notify("Manja tutyo! Your string snapped. Pull less hard.", false); mine.AddComponent<FallingKite>(); myLine.enabled = false; }
            else if (tension < .03f) { over = true; G.Notify("Your kite dived into a rooftop. Keep the string tighter.", false); mine.AddComponent<FallingKite>(); }
        }
        void Cleanup() { if (mine) Object.Destroy(mine); if (rival) Object.Destroy(rival); }
        public override void Cancel() { Cleanup(); G.rig.pitch = 12; G.rig.distance = 6; base.Cancel(); }
        public override void Draw(float W, float H)
        {
            Panel(W, H, "DASHAIN PASTIME · चङ्गा चेट", "Kite fight over the valley"); var r = Inner(W, H);
            GUI.Label(new Rect(r.x, r.y, r.width, 26), "Hold SPACE to pull the manja, let go to give slack. Stay in the green to cut the blue kite.", Retro.Small);
            GUI.Label(new Rect(r.x, r.y + 32, 120, 26), "Tension", Retro.Body); Retro.Meter(new Rect(r.x + 120, r.y + 32, r.width - 120, 28), tension, .45f, .78f);
            GUI.Label(new Rect(r.x, r.y + 72, 120, 26), "Cut", Retro.Body); Retro.Meter(new Rect(r.x + 120, r.y + 72, r.width - 120, 28), cut, -1, -1, Retro.FlagBlue);
            if (over) GUI.Label(new Rect(r.x, r.y + 112, r.width, 30), won ? "Jityo! SPACE for another round." : "Haryo. SPACE to fly again.", Retro.Body);
        }
    }
    public class FallingKite : MonoBehaviour { float t; void Update() { t += Time.deltaTime; transform.position += new Vector3(Mathf.Sin(t * 3) * .1f, -3 * Time.deltaTime, 0); transform.Rotate(0, 0, 220 * Time.deltaTime); if (t > 8) Destroy(gameObject); } }

    // ───────────────────────── Dashain ping ─────────────────────────
    public class PingSwing : Activity
    {
        float angle, velocity, best; GameObject seat;
        public override string Prompt => "Ride the Dashain ping  ·  पिङ";
        public override bool Available => Owner.festivals.Today == "Dashain";
        public override void Begin() { angle = 0; velocity = .2f; best = 0; seat = GameObject.Find("Ping Seat"); G.player.body.enabled = false; }
        public override void Tick(float dt)
        {
            // Pump at the bottom of the arc, in the direction of travel.
            if (Input.GetKeyDown(KeyCode.Space) && Mathf.Abs(angle) < .25f) velocity += Mathf.Sign(velocity == 0 ? 1 : velocity) * .35f;
            velocity -= 9.81f / 6f * Mathf.Sin(angle) * dt; velocity *= 1 - .08f * dt; angle += velocity * dt; angle = Mathf.Clamp(angle, -1.45f, 1.45f);
            best = Mathf.Max(best, Mathf.Abs(angle) * Mathf.Rad2Deg);
            if (seat) { seat.transform.localRotation = Quaternion.Euler(angle * Mathf.Rad2Deg, 0, 0); G.player.transform.position = seat.transform.TransformPoint(new Vector3(0, -6.2f, 0)); }
        }
        public override void Cancel()
        {
            P.pingBest = Mathf.Max(P.pingBest, best); if (best > 60 && P.DailyReward("ping", 20, 1)) G.Notify($"You touched {best:0}° on the ping! +Rs 20", true);
            if (seat) seat.transform.localRotation = Quaternion.identity; G.player.Teleport(position + Vector3.back * 2 + Vector3.up * .05f); base.Cancel();
        }
        public override void Draw(float W, float H)
        {
            Panel(W, H, "DASHAIN · पिङ", "Bamboo ping swing", 280); var r = Inner(W, H, 280);
            GUI.Label(new Rect(r.x, r.y, r.width, 26), "Press SPACE each time you pass the bottom to pump higher. Esc to get off.", Retro.Small);
            Retro.Meter(new Rect(r.x, r.y + 36, r.width, 30), Mathf.Abs(angle) / 1.45f); GUI.Label(new Rect(r.x, r.y + 72, r.width, 30), $"Height {Mathf.Abs(angle) * Mathf.Rad2Deg:0}°  ·  best {Mathf.Max(best, P.pingBest):0}°", Retro.Body);
        }
    }

    // ───────────────────────── Hari's salon ─────────────────────────
    public class Salon : Activity
    {
        static readonly (string style, string line)[] Styles = { ("Spiky gel cut", "Gel lagaidinchhu, bhai. Exactly like the film poster on the wall."), ("Side parting, hero style", "Ekdam hero banaidinchhu. Girls will look twice at Durbar Square."), ("Simple short, exam season", "Exam aaudaichha? Short and neat, ama-buwa khusi.") };
        float time; int pick = -1;
        public override string Prompt => "Haircut at Hari's Salon · Rs 50  ·  सैलुन";
        public override void Begin() { time = 0; pick = -1; if (P.cash < 50) { G.Notify("A haircut costs Rs 50.", false); End(); return; } Owner.SalonTape(true); }
        public override void Tick(float dt)
        {
            if (pick < 0) { for (int i = 0; i < 3; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i)) Choose(i); return; }
            time += dt; if (time >= 7)
            {
                P.cash -= 50; bool rep = P.DailyReward("salon", 0, 1); P.minutes = (P.minutes + 25) % 1440; Owner.SalonTape(false);
                G.Notify($"Fresh {Styles[pick].style.ToLower()}! Rs 50 paid.{(rep ? "  +1 reputation" : "")}", true); End();
            }
        }
        void Choose(int i) { pick = i; time = 0; }
        public override void Cancel() { Owner.SalonTape(false); base.Cancel(); }
        public override void Draw(float W, float H)
        {
            Panel(W, H, "HARI SALON · हरि सैलुन", "Hari dai: \"Bhai, kasto katne?\"", 340); var r = Inner(W, H, 340);
            if (pick < 0)
            {
                for (int i = 0; i < 3; i++) if (GUI.Button(new Rect(r.x, r.y + i * 52, r.width, 44), $"{Retro.Devanagari(i + 1)}   {Styles[i].style}", Retro.Ticket)) Choose(i);
                if (Game.Instance.audioDirector.MusicClip == null) GUI.Label(new Rect(r.x, r.y + 162, r.width, 24), "The cassette is empty: generate music with npm run assets:audio for old songs.", Retro.Small);
            }
            else { GUI.Label(new Rect(r.x, r.y, r.width, 60), $"\"{Styles[pick].line}\"", Retro.Body); GUI.Label(new Rect(r.x, r.y + 70, r.width, 24), "Snip snip… the tape plays an old adhunik geet.", Retro.Small); Retro.Meter(new Rect(r.x, r.y + 104, r.width, 26), time / 7); }
        }
    }

    // ───────────────────────── Cassette ─────────────────────────
    public class CassetteTape : Activity
    {
        GameObject playing;
        public override string Prompt => playing ? "Stop the cassette  ·  टेप" : "Play the cassette  ·  टेप";
        public override void Begin()
        {
            if (playing) { Object.Destroy(playing); playing = null; G.Notify("Tape stopped. Kachak!", false); }
            else { playing = Cassette.Play(position + Vector3.up * 1f); G.Notify(playing ? "Kachak… the tape starts: an old Nepali adhunik geet, slightly warped." : "No tapes yet. Generate music with npm run assets:audio, then npm run unity:data.", false); }
            End();
        }
        public override void Draw(float W, float H) { }
    }

    // ───────────────────────── Cyber cafe ─────────────────────────
    public class CyberCafe : Activity
    {
        enum Tab { Desktop, Chat, Mail, Lan } Tab tab; float session; int chatStep; readonly List<string> chat = new List<string>(); Rect target; float lanTime; int lanScore; bool lan;
        static readonly (string me, string them)[][] Script =
        {
            new[] { ("hi", "hiii :) asl?"), ("asl?? lol. 17/m/pokhara", "same same! 16/f/ktm. i'm at cyber, load shedding at home") },
            new[] { ("did u see the new film songs?", "yes!! downloading mp3, 128kbps takes 20 min haha"), ("brb, dial-up feels slow today", "ok ok. bf/gf? jk jk :P") },
            new[] { ("gotta go, rs 20 finished", "noooo. add me, we chat tomorrow same time ;)"), ("ttyl, take care", "ttyl!! :) :) :)") }
        };
        public override string Prompt => "Cyber · Rs 20 for 30 minutes  ·  साइबर";
        public override void Begin()
        {
            if (P.cash < 20) { G.Notify("Cyber costs Rs 20 for 30 minutes.", false); End(); return; }
            P.cash -= 20; session = 60; tab = Tab.Desktop; chatStep = 0; chat.Clear(); lan = false; lanScore = 0;
        }
        public override void Tick(float dt)
        {
            session -= dt; if (lan) { lanTime -= dt; if (lanTime <= 0) { lan = false; P.lanBest = Mathf.Max(P.lanBest, lanScore); if (lanScore >= 12 && P.DailyReward("lan", 10, 1)) G.Notify($"LAN champion! {lanScore} frags. +Rs 10", true); } }
            if (session <= 0) { P.minutes = (P.minutes + 30) % 1440; G.Notify("\"Time over, bhai!\" Your 30 minutes at the cyber are up.", false); End(); }
        }
        public override void Draw(float W, float H)
        {
            // Beige CRT monitor with a teal desktop.
            var mon = new Rect(W / 2 - 520, H / 2 - 400, 1040, 760); Retro.Fill(mon, new Color(.82f, .79f, .7f)); Retro.Fill(new Rect(mon.x + 10, mon.yMax - 46, mon.width - 20, 36), new Color(.74f, .71f, .62f));
            GUI.Label(new Rect(mon.x + 30, mon.yMax - 44, 400, 32), "SAMTRON  ·  CRT 15\"", new GUIStyle(Retro.Eyebrow) { normal = { textColor = new Color(.35f, .32f, .28f) } });
            var screen = new Rect(mon.x + 40, mon.y + 36, mon.width - 80, mon.height - 110); Retro.Fill(screen, new Color(0, .5f, .5f));
            var bar = new Rect(screen.x, screen.yMax - 34, screen.width, 34); Retro.Fill(bar, new Color(.75f, .75f, .75f));
            if (GUI.Button(new Rect(bar.x + 4, bar.y + 4, 90, 26), "Start", Retro.TicketGold)) tab = Tab.Desktop;
            GUI.Label(new Rect(bar.xMax - 330, bar.y + 4, 320, 26), $"Session left {Mathf.CeilToInt(session)}s   ·   Esc: log off", new GUIStyle(Retro.Small) { alignment = TextAnchor.MiddleRight, normal = { textColor = Color.black } });
            var area = new Rect(screen.x + 20, screen.y + 20, screen.width - 40, screen.height - 74);
            if (tab == Tab.Desktop)
            {
                string[] icons = { "Messenger", "Inbox (3)", "LAN Shooter" };
                for (int i = 0; i < 3; i++) if (GUI.Button(new Rect(area.x, area.y + i * 90, 160, 70), icons[i], Retro.TicketGold)) tab = (Tab)(i + 1);
                GUI.Label(new Rect(area.x + 200, area.y, 600, 200), "<b>Welcome to Phewa Cyber</b>\nRs 20 = 30 minutes\nPrint Rs 5 per page · CD write Rs 50\nPlease do not download big files, dial-up is slow!!", new GUIStyle(Retro.Body) { normal = { textColor = Color.white } });
            }
            else if (tab == Tab.Chat) Chat(area);
            else if (tab == Tab.Mail) Mail(area);
            else Lan(area);
        }
        void Window(Rect r, string title) { Retro.Fill(r, new Color(.83f, .82f, .78f)); Retro.Fill(new Rect(r.x, r.y, r.width, 28), new Color(0, 0, .5f)); GUI.Label(new Rect(r.x + 8, r.y + 3, r.width - 16, 24), title, new GUIStyle(Retro.Label) { fontStyle = FontStyle.Bold }); }
        void Chat(Rect a)
        {
            Window(a, "Messenger  —  chatting with kanchhi_sweet_16"); var black = new GUIStyle(Retro.Body) { normal = { textColor = Color.black } };
            float y = a.y + 40; foreach (var line in chat.Skip(Mathf.Max(0, chat.Count - 9))) { GUI.Label(new Rect(a.x + 14, y, a.width - 28, 28), line, black); y += 32; }
            if (chatStep < Script.Length)
                for (int i = 0; i < Script[chatStep].Length; i++)
                    if (GUI.Button(new Rect(a.x + 14, a.yMax - 100 + i * 46, a.width - 28, 40), Script[chatStep][i].me, Retro.Ticket))
                    { var (me, them) = Script[chatStep][i]; chat.Add($"<b>you:</b> {me}"); chat.Add($"<b>kanchhi_sweet_16:</b> {them}"); chatStep++; if (chatStep == Script.Length && P.DailyReward("chat", 0, 1)) G.Notify("New online friend added. +1 reputation", true); }
        }
        void Mail(Rect a)
        {
            Window(a, "Inbox"); var black = new GUIStyle(Retro.Body) { normal = { textColor = Color.black } };
            GUI.Label(new Rect(a.x + 14, a.y + 40, a.width - 28, a.height - 50),
                "<b>FW: FW: FW: forward this to 10 friends!!!</b>\nIf you don't forward in 10 min your exam will go bad… (sent to 47 people)\n\n" +
                "<b>Maya Gurung — Tole meeting</b>\nNamaste! Tole sabha on Saturday at the chautari. Bring ideas for the Dashain ping. — Maya\n\n" +
                "<b>Junu — photos</b>\nScanned the Phewa photos at the studio. Sending soon, attachment is 2 MB so it might take an hour :)", black);
        }
        void Lan(Rect a)
        {
            Window(a, "LAN Shooter  —  Phewa Cyber LAN (4 players)");
            if (!lan)
            {
                GUI.Label(new Rect(a.x + 14, a.y + 44, a.width - 28, 60), $"Click the targets as fast as you can for 15 seconds. Best: {P.lanBest} frags", new GUIStyle(Retro.Body) { normal = { textColor = Color.black } });
                if (GUI.Button(new Rect(a.x + 14, a.y + 110, 220, 50), "Join game", Retro.TicketGold)) { lan = true; lanTime = 15; lanScore = 0; MoveTarget(a); }
                return;
            }
            GUI.Label(new Rect(a.x + 14, a.y + 34, 400, 30), $"Frags {lanScore}  ·  {lanTime:0.0}s", new GUIStyle(Retro.Body) { normal = { textColor = Color.black } });
            if (GUI.Button(target, "✕", Retro.Ticket)) { lanScore++; MoveTarget(a); }
        }
        void MoveTarget(Rect a) => target = new Rect(Random.Range(a.x + 20, a.xMax - 80), Random.Range(a.y + 70, a.yMax - 80), 60, 60);
        public override void Cancel() { if (session > 0) P.minutes = (P.minutes + (60 - session) / 2) % 1440; base.Cancel(); }
    }

    // ───────────────────────── Junkiri (fireflies) ─────────────────────────
    public class FireflyCatch : MonoBehaviour
    {
        class Fly { public Transform t; public Vector3 home; public float phase; }
        readonly List<Fly> flies = new List<Fly>(); Mesh mesh; Activities owner; public CatchActivity activity = new CatchActivity();
        public class CatchActivity : Activity
        {
            public FireflyCatch field; public Transform target;
            public override string Prompt => $"Catch a junkiri  ·  जुनकिरी ({Game.Instance.progress.fireflies % 10}/10 in the jar)";
            public override bool Available => target != null;
            public override void Begin() { field.Catch(target); End(); }
            public override void Draw(float W, float H) { }
        }
        public void Attach(Activities owner) { this.owner = owner; activity.field = this; activity.Attach(owner); activity.id = "junkiri"; activity.radius = 1.9f; }

        bool Habitat(Vector3 p, string district) => district == "rural" || district == "coast" || World.DistanceXZ(p, -120, 214) < 90 || World.DistanceXZ(p, 40, 40) < 35;

        void Update()
        {
            var G = Game.Instance; if (G == null || !G.Ready) return;
            var p = G.player.transform.position; bool on = G.dayNight.Night > .55f && Habitat(p, World.DistrictAt(p.x, p.z).id);
            if (mesh == null) { var g = new MeshBuilder(); g.Sphere(0, 0, 0, .05f, .05f, .05f, new Color(.75f, 1f, .3f), new Surface(.5f, 0, 6), 6); mesh = g.Build("Junkiri"); }
            while (on && flies.Count < 36) Spawn(p);
            Transform nearest = null; float best = activity.radius;
            for (int i = flies.Count - 1; i >= 0; i--)
            {
                var f = flies[i];
                if (!on || (f.home - p).magnitude > 30) { Destroy(f.t.gameObject); flies.RemoveAt(i); continue; }
                f.phase += Time.deltaTime; var t = f.phase;
                f.t.position = f.home + new Vector3(Mathf.Sin(t * .7f) * 1.5f, Mathf.Sin(t * 1.3f) * .4f, Mathf.Cos(t * .5f) * 1.5f);
                f.t.localScale = Vector3.one * (Mathf.Sin(t * 3.1f) > .2f ? 1 : .2f); // blink
                float d = Vector3.Distance(f.t.position, p + Vector3.up * 1.2f); if (d < best + .8f) { best = d; nearest = f.t; }
            }
            activity.target = nearest; if (nearest) activity.position = new Vector3(nearest.position.x, 0, nearest.position.z);
        }

        void Spawn(Vector3 around)
        {
            var go = new GameObject("Junkiri"); go.transform.SetParent(transform, false); go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = Materials.VertexLit; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var a = Random.Range(0, Mathf.PI * 2); var d = Random.Range(3f, 24f);
            var home = around + new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d); home.y = World.TerrainHeight(home.x, home.z) + Random.Range(.6f, 2.4f);
            flies.Add(new Fly { t = go.transform, home = home, phase = Random.Range(0, 10) });
        }

        public void Catch(Transform t)
        {
            var G = Game.Instance; int i = flies.FindIndex(f => f.t == t); if (i < 0) return; Destroy(t.gameObject); flies.RemoveAt(i);
            G.progress.fireflies++;
            if (G.progress.fireflies % 10 == 0) { bool paid = G.progress.DailyReward("junkiri", 10, 1); G.Notify($"A jar full of junkiri! ({G.progress.fireflies} caught in total){(paid ? "  +Rs 10" : "")}", true); }
        }
    }

    // ───────────────────────── Festival calendar ─────────────────────────
    public class Festivals : MonoBehaviour
    {
        public string Today { get; private set; } = "";
        readonly List<GameObject> decor = new List<GameObject>(); int shownDay = -1;
        public static string ForDay(int day) => (day % 6) switch { 2 => "Dashain", 4 => "Tihar", 5 => "Bandh", _ => "" };

        void Update()
        {
            var G = Game.Instance; if (G == null || !G.Ready) return;
            if (G.progress.day != shownDay) { shownDay = G.progress.day; Today = ForDay(shownDay); Rebuild(G); if (G.Started && Today != "") G.Notify(Today == "Dashain" ? "Dashain aayo! Kites fill the sky and the ping is up in Durbar Square." : Today == "Tihar" ? "Shubha Tihar! Diyo and lights glow across the tole tonight." : "Nepal bandh today: shutters down, roads empty, and the kids are playing cricket on the main road.", true); }
            foreach (var d in decor) if (d && d.name == "Sky Kite") { var k = d.GetComponent<SkyKite>(); if (k) k.Tick(); }
        }

        void Rebuild(Game G)
        {
            foreach (var d in decor) if (d) Destroy(d); decor.Clear();
            if (Today == "Dashain")
            {
                // Ping: four bamboo poles tied at the top with a swinging rope seat.
                var g = new MeshBuilder(); var bamboo = new Color(.62f, .55f, .3f);
                foreach (var (dx, dz) in new[] { (-1.8f, -1.2f), (1.8f, -1.2f), (-1.8f, 1.2f), (1.8f, 1.2f) }) { var a = new Vector3(18 + dx, 0, 30 + dz); var top = new Vector3(18, 7, 30); for (int i = 0; i < 14; i++) { var p = Vector3.Lerp(a, top, i / 14f); g.Box(p.x, p.y, p.z, .08f, .3f, .08f, bamboo); } }
                g.PrayerFlags(new Vector3(18, 7, 30), new Vector3(30, 3, 30), .6f); g.PrayerFlags(new Vector3(18, 7, 30), new Vector3(6, 3, 30), .6f);
                decor.Add(Static("Ping Frame", g.Build("Ping")));
                var seatPivot = new GameObject("Ping Seat"); seatPivot.transform.position = new Vector3(18, 6.9f, 30);
                var sg = new MeshBuilder(); sg.Box(-.4f, -3.2f, 0, .02f, 3.1f, .02f, new Color(.5f, .4f, .25f)); sg.Box(.4f, -3.2f, 0, .02f, 3.1f, .02f, new Color(.5f, .4f, .25f)); sg.Box(0, -6.3f, 0, .45f, .05f, .18f, new Color(.4f, .25f, .12f));
                var seat = Static("Seat", sg.Build("Seat")); seat.transform.SetParent(seatPivot.transform, false); decor.Add(seatPivot);
                for (int i = 0; i < 14; i++) { var k = G.models.Instantiate("kite_changa", null, Vector3.zero, 0, 1.4f) ?? Static("Sky Kite", KiteMesh(i)); k.name = "Sky Kite"; k.AddComponent<SkyKite>().seed = i; decor.Add(k); }
            }
            else if (Today == "Bandh")
            {
                // Street cricket on the empty road: brick wicket, two kids and a bouncing plastic ball.
                var g = new MeshBuilder(); g.Box(80, .12f, 32, .12f, .12f, .22f, new Color(.6f, .25f, .15f)); g.Box(80, .36f, 32, .12f, .12f, .22f, new Color(.6f, .25f, .15f));
                decor.Add(Static("Brick wicket", g.Build("Wicket")));
                for (int i = 0; i < 2; i++) { var kid = new GameObject(i == 0 ? "Batter" : "Bowler"); kid.transform.SetParent(transform, false); kid.transform.position = new Vector3(80, 0, i == 0 ? 33.2f : 50); kid.transform.rotation = Quaternion.Euler(0, i == 0 ? 0 : 180, 0); Figure.Create(kid.transform, null, i == 0 ? new Color(.1f, .3f, .6f) : new Color(.6f, .1f, .1f), new Color(.55f, .34f, .2f), new Color(.02f, .02f, .02f), null, null, 1.25f); decor.Add(kid); }
                var bg = new MeshBuilder(); bg.Sphere(0, 0, 0, .05f, .05f, .05f, new Color(.95f, .9f, .2f), new Surface(.4f), 8); var ball = Static("Cricket ball", bg.Build("Ball")); ball.AddComponent<BouncingBall>(); decor.Add(ball);
            }
            else if (Today == "Tihar")
            {
                // Diyo rows around the square and festoon lights from the pagoda.
                var g = new MeshBuilder(); var flame = new Color(1, .6f, .15f);
                for (float x = 16; x <= 64; x += 1.6f) foreach (var z in new[] { 14.4f, 65.6f }) { g.Cylinder(x, .06f, z, .07f, .05f, new Color(.45f, .2f, .1f)); g.Sphere(x, .16f, z, .025f, .05f, .025f, flame, new Surface(.5f, 0, 5), 6); }
                var crown = new Vector3(47, 16, 47); var lights = new[] { new Color(1, .2f, .2f), new Color(.2f, 1, .3f), new Color(.3f, .4f, 1), new Color(1, .9f, .2f) };
                foreach (var (dx, dz) in new[] { (-17f, -17f), (17f, -17f), (-17f, 17f), (17f, 17f), (0f, -20f), (-20f, 0f) })
                    for (int i = 1; i < 40; i++) { float t = i / 40f; var p = Vector3.Lerp(crown, new Vector3(47 + dx, 3.5f, 47 + dz), t); p.y -= 1.2f * 4 * t * (1 - t); g.Sphere(p.x, p.y, p.z, .06f, .06f, .06f, lights[i % 4], new Surface(.5f, 0, -6), 6); }
                decor.Add(Static("Tihar Lights", g.Build("Tihar")));
            }
        }
        static Mesh KiteMesh(int i) { var g = new MeshBuilder(); var c = Color.HSVToRGB(i * .13f % 1, .8f, .9f); g.Facing(Vector3.back, new Vector3(0, .8f, 0), new Vector3(.6f, 0, 0), new Vector3(0, -.8f, 0), new Vector3(-.6f, 0, 0), c, new Surface(.8f)); g.Facing(Vector3.forward, new Vector3(0, .8f, 0), new Vector3(.6f, 0, 0), new Vector3(0, -.8f, 0), new Vector3(-.6f, 0, 0), c, new Surface(.8f)); return g.Build("Sky kite"); }
        GameObject Static(string name, Mesh m) { var go = new GameObject(name); go.transform.SetParent(transform, false); go.AddComponent<MeshFilter>().sharedMesh = m; go.AddComponent<MeshRenderer>().sharedMaterial = Materials.VertexLit; return go; }
    }

    public class BouncingBall : MonoBehaviour { void Update() { float t = Mathf.PingPong(Time.time * .45f, 1); transform.position = new Vector3(80 + Mathf.Sin(Time.time) * .3f, .1f + Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2)) * 1.2f, Mathf.Lerp(49, 33.6f, t)); } }

    public class SkyKite : MonoBehaviour
    {
        public int seed;
        public void Tick()
        {
            var p = Game.Instance.player.transform.position; float t = Time.time * .2f + seed;
            transform.position = new Vector3(p.x + Mathf.Sin(seed * 2.1f) * 70 + Mathf.Sin(t) * 6, 45 + seed % 5 * 6 + Mathf.Sin(t * 1.7f) * 3, p.z + Mathf.Cos(seed * 2.1f) * 70 + Mathf.Cos(t * .8f) * 6);
            transform.rotation = Quaternion.Euler(-20, seed * 40 + Mathf.Sin(t * 2) * 25, Mathf.Sin(t * 3) * 20);
        }
    }
}

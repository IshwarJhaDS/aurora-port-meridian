// Vintage Nepali game UI (immediate mode, no prefabs): a crimson film-poster title
// screen, dhaka-bordered paper panels, a school-notebook objective, a Casio LCD clock,
// rupee stamp, FM-radio toasts, ticket-style choices and a hand-drawn tole map.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PhewaValley
{
    public class GameUI : MonoBehaviour
    {
        enum Panel { None, Map, Journal, Menu }
        Panel panel; bool photo, confirmReset, wheel; public bool ShowWork;
        readonly List<(string text, bool success, float born)> toasts = new List<(string, bool, float)>();
        string say = ""; Vector2 journalScroll, mapScroll; string selectedPlace;

        Game G => Game.Instance;
        public bool PanelOpen => panel != Panel.None || wheel;
        public bool BlocksCamera => PanelOpen || ShowWork || wheel || G != null && (G.InDialogue || G.activities?.Current is CyberCafe || G.heroes != null && G.heroes.Switching) || !(G?.Started ?? false);

        static readonly Dictionary<string, string> DevaDistrict = new Dictionary<string, string> { ["downtown"] = "दरबार टोल", ["coast"] = "फेवा ताल किनार", ["rural"] = "सराङकोट डाँडा", ["industrial"] = "घाट बजार", ["suburbs"] = "नेवा टोल", ["highway"] = "पृथ्वी राजमार्ग" };

        public void Toast(string text, bool success) { toasts.Add((text, success, Time.unscaledTime)); if (toasts.Count > 3) toasts.RemoveAt(0); }
        public void MissionsChanged() { }
        public void DialogueChanged(Conversation c) { if (c == null) say = ""; }

        void Update()
        {
            toasts.RemoveAll(t => Time.unscaledTime - t.born > 6);
            if (G == null || !G.Ready) return;
            if (!G.Started) { if (Input.GetKeyDown(KeyCode.Return)) G.Begin(); return; }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (wheel) wheel = false;
                else if (G.activities.Current != null) G.activities.Current.Cancel();
                else if (G.InDialogue) G.dialogue.Close(); else if (photo) { photo = false; G.rig.photo = false; } else if (ShowWork) ShowWork = false;
                else panel = panel == Panel.None ? Panel.Menu : Panel.None;
                confirmReset = false; return;
            }
            if (G.InDialogue) { if (GUIUtility.keyboardControl == 0) for (int i = 0; i < 6; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { var o = G.dialogue.Current.options.ElementAtOrDefault(i); if (o != null) G.dialogue.Choose(o.id); } return; }
            if (GUIUtility.keyboardControl != 0 || G.activities.Current != null) return;
            if (Input.GetKeyDown(KeyCode.F2)) G.progress.showFirstVisitHints = !G.progress.showFirstVisitHints;
            if (Input.GetKeyDown(KeyCode.F1)) { hudHidden = !hudHidden; if (hudHidden) Toast("HUD hidden · F1 to bring it back", false); }
            if (Input.GetKeyDown(KeyCode.M)) panel = panel == Panel.Map ? Panel.None : Panel.Map;
            if (Input.GetKeyDown(KeyCode.J)) panel = panel == Panel.Journal ? Panel.None : Panel.Journal;
            if (Input.GetKeyDown(KeyCode.P) && panel == Panel.None) { photo = !photo; G.rig.photo = photo; if (photo && World.DistanceXZ(G.player.transform.position, G.data.PoiPosition("lookout")) < 25) G.missions.Event("photo", "lookout"); }
            if (Input.GetKeyDown(KeyCode.V) && panel == Panel.None && !photo) Wipe(() => { G.rig.CycleMode(); Toast($"Camera · {G.rig.ModeName}", false); });
            if (Input.GetKeyDown(KeyCode.Tab) && panel == Panel.None) wheel = !wheel;
            if (wheel) for (int i = 0; i < 3; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { wheel = false; G.heroes.Switch(i); }
            if (photo && Input.GetKeyDown(KeyCode.Return)) Capture();
            if (ShowWork && G.progress.Mission("cafe")?.status != "active") ShowWork = false;
        }

        void Capture()
        {
            var dir = Path.Combine(Application.persistentDataPath, "Postcards"); Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"Postcard_from_Phewa_Valley_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
            ScreenCapture.CaptureScreenshot(file); Toast($"Postcard saved · {file}", true);
        }

        void OnGUI()
        {
            Retro.Init();
            float scale = Screen.height / 1080f; GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float W = Screen.width / scale, H = 1080;
            if (G == null || !G.Started) { Welcome(W, H); return; }
            DrawWipe(W, H);
            if (photo) { Retro.Fill(new Rect(0, 0, W, 70), Color.black); Retro.Fill(new Rect(0, H - 70, W, 70), Color.black); GUI.Label(new Rect(40, H - 52, 1100, 40), "POSTCARD · move the mouse to frame, scroll to zoom · ENTER to capture · P to return", new GUIStyle(Retro.Eyebrow) { normal = { textColor = Retro.Paper } }); return; }
            if (!hudHidden) { WorldLabels(scale); Hud(W, H); FirstVisitHints(); Radar(W, H); RadioPanel(W); }
            MemoryPopup(W); TalePopup(W); Toasts(W, H);
            if (G.activities.Current != null) G.activities.DrawCurrent(W, H);
            else if (G.InDialogue) DialogueBox(W, H);
            if (ShowWork) WorkPanel(W, H);
            if (panel == Panel.Journal) Journal(W, H); else if (panel == Panel.Map) Map(W, H); else if (panel == Panel.Menu) Menu(W, H);
            if (wheel) Wheel(W, H);
        }

        // ───────────── Title screen: a crimson cinema poster ─────────────
        void Welcome(float W, float H)
        {
            Retro.Fill(new Rect(0, 0, W, H), Retro.CrimsonDark);
            for (int i = 0; i < 18; i++) Retro.Fill(new Rect(0, i * 60, W, 30), new Color(1, 1, 1, .015f)); // poster print bands
            GUI.DrawTexture(new Rect(0, H - 460, W, 380), Retro.MountainTex, ScaleMode.StretchToFill);
            Retro.Dhaka(new Rect(0, 0, W, 26)); Retro.Dhaka(new Rect(0, H - 26, W, 26));
            var paperText = new GUIStyle(Retro.Eyebrow) { normal = { textColor = Retro.Gold }, fontSize = 18 };
            DevaText.Label(new Rect(110, 70, 1200, 30), "AURORA PICTURES PRESENTS  ·  एक नेपाली कथा", paperText);
            DevaText.Label(new Rect(104, 104, 1400, 170), "फेवा उपत्यका", new GUIStyle(Retro.Deva20) { fontSize = 120, normal = { textColor = Retro.Paper } });
            GUI.Label(new Rect(110, 262, 1400, 100), "PHEWA VALLEY", new GUIStyle(Retro.Title) { fontSize = 84, normal = { textColor = Retro.Gold } });
            GUI.Label(new Rect(114, 360, 1000, 80), "Chiya, chungi, load shedding and a whole tole of stories.\nA nostalgic open world in 2000s Nepal.", new GUIStyle(Retro.Body) { fontSize = 24, normal = { textColor = Retro.Paper } });
            if (G == null || !G.Ready) { GUI.Label(new Rect(114, 480, 900, 40), G?.LoadingText ?? "Preparing the valley…", new GUIStyle(Retro.Body) { fontSize = 22, normal = { textColor = Retro.Gold } }); return; }
            if (GUI.Button(new Rect(114, 470, 420, 76), "ENTER THE VALLEY  ↗", new GUIStyle(Retro.TicketGold) { fontSize = 26 })) G.Begin();
            DevaText.Label(new Rect(560, 486, 400, 44), "खेल सुरु गरौं", new GUIStyle(Retro.Deva20) { fontSize = 28, normal = { textColor = Retro.Gold } });
            int models = G.models.Loaded.Count();
            GUI.Label(new Rect(114, 566, 1300, 60), $"Press ENTER  ·  WASD move · E interact · M map · J stories · Esc menu\n{(models > 0 ? $"{models} Tripo models installed" : "Using built-in models")}", new GUIStyle(Retro.Small) { normal = { textColor = new Color(.95f, .85f, .75f) } });
            // Old film-poster credit block.
            GUI.Label(new Rect(W - 560, H - 130, 500, 80), "STARRING  MAYA · LAXMAN DAI · TASHI · JUNU\nMUSIC ON CASSETTE  ·  NOW SHOWING", new GUIStyle(Retro.Eyebrow) { alignment = TextAnchor.LowerRight, normal = { textColor = Retro.Paper }, wordWrap = true });
        }

        // ───────────── HUD ─────────────
        void Hud(float W, float H)
        {
            var p = G.player.transform.position; var d = World.DistrictAt(p.x, p.z);
            // Location tag: paper with dhaka band, English and Devanagari names.
            var tag = new Rect(18, 18, 400, 112); Retro.PaperPanel(tag, false); Retro.Dhaka(new Rect(tag.x + 3, tag.y + 3, tag.width - 6, 10));
            GUI.Label(new Rect(tag.x + 18, tag.y + 18, 380, 40), d.name, Retro.Heading);
            DevaText.Label(new Rect(tag.x + 18, tag.y + 52, 380, 30), DevaDistrict.TryGetValue(d.id, out var dv) ? dv : "", Retro.Deva20);
            DevaText.Label(new Rect(tag.x + 18, tag.y + 82, 380, 24), $"{G.heroes.Current.name.Split(' ')[0]} · Day {G.progress.day}{(G.activities.festivals.Today != "" ? "  ·  " + G.activities.festivals.Today : "")}  ·  {G.dayNight.WeatherName}", Retro.Small);
            // Casio-style LCD clock.
            Retro.LcdPanel(new Rect(430, 24, 150, 50), World.Clock(G.progress.minutes));
            if (G.dayNight.PowerCut) DevaText.Label(new Rect(430, 78, 300, 24), "⚡ बत्ती गयो", new GUIStyle(Retro.Deva20) { fontSize = 17, normal = { textColor = Retro.Gold } });
            // Rupee stamp and reputation.
            var cash = new Rect(W - 300, 18, 282, 82); Retro.PaperPanel(cash, false);
            GUI.Label(new Rect(cash.x + 12, cash.y + 8, cash.width - 26, 44), $"रु {Mathf.FloorToInt(G.progress.cash):N0}", Retro.Stamp);
            DevaText.Label(new Rect(cash.x + 12, cash.y + 50, cash.width - 26, 24), $"इज्जत · reputation {G.progress.reputation}", new GUIStyle(Retro.Small) { alignment = TextAnchor.UpperRight });
            // Objective written in a school notebook.
            var t = G.missions.Tracked(); Vector3? goal = CurrentGoal();
            var card = new Rect(W - 450, 118, 432, 168); Retro.Notebook(card);
            string label = G.navTarget.HasValue ? G.navLabel : t?.def.title ?? "Explore the valley", text = G.navTarget.HasValue ? "Custom waypoint" : t?.step?.text ?? "Play, work and visit every tole.";
            if (G.job.OnDuty) { label = $"Passenger shift · {G.job.Fares} fares"; text = G.job.Status; }
            else if (G.events.Current is var ev && ev != null && ev.accepted && ev.kind == StreetEvents.Kind.Shopping) { label = "Spilled shopping"; text = $"{ev.items.Count} left to pick up · {Mathf.CeilToInt(ev.timer)}s"; }
            string dist = "";
            if (goal.HasValue)
            {
                var to = goal.Value - p; to.y = 0;
                if (G.progress.guidance == 1)
                {
                    // Explorer: no metres, no arrow. Only a rough heading, as a local would give it.
                    float bearing = Mathf.Repeat(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, 360);
                    string[] eight = { "north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west" };
                    text += to.magnitude < 25 ? "  (It's close by.)" : $"  (Follow the red kite, somewhere to the {eight[Mathf.RoundToInt(bearing / 45) % 8]}.)";
                }
                else
                {
                    var camF = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up);
                    float angle = Vector3.SignedAngle(camF, to, Vector3.up); string arrow = Mathf.Abs(angle) < 22 ? "↑" : Mathf.Abs(angle) > 158 ? "↓" : angle > 0 ? (angle < 68 ? "↗" : angle < 112 ? "→" : "↘") : (angle > -68 ? "↖" : angle > -112 ? "←" : "↙");
                    dist = $"{arrow} {Mathf.RoundToInt(to.magnitude)} m";
                }
            }
            var pen = new GUIStyle(Retro.Body) { normal = { textColor = new Color(.1f, .15f, .45f) }, fontSize = 18 };
            DevaText.Label(new Rect(card.x + 46, card.y + 12, 260, 24), "गृहकार्य · TODAY'S TASK", Retro.Eyebrow); GUI.Label(new Rect(card.x + 280, card.y + 12, 140, 24), dist, new GUIStyle(Retro.Eyebrow) { alignment = TextAnchor.UpperRight });
            GUI.Label(new Rect(card.x + 46, card.y + 38, 376, 30), label, new GUIStyle(Retro.Heading) { fontSize = 22, normal = { textColor = new Color(.1f, .15f, .45f) } });
            GUI.Label(new Rect(card.x + 46, card.y + 72, 376, 90), text, pen);
            // Night: jar of junkiri.
            if (G.dayNight.Night > .5f && G.progress.fireflies > 0) GUI.Label(new Rect(W - 450, 292, 432, 26), $"✦ junkiri in the jar: {G.progress.fireflies % 10}/10 · total {G.progress.fireflies}", new GUIStyle(Retro.Label) { alignment = TextAnchor.UpperRight, normal = { textColor = new Color(.8f, 1, .4f) } });
            // Interaction prompt as an old keyboard key.
            var (kind, ilabel, _) = G.interaction;
            if (kind != Interaction.None && !G.InDialogue && !PanelOpen)
            {
                var r = new Rect(W / 2 - 280, H - 170, 560, 64); Retro.PaperPanel(r, false); Retro.KeyCap(new Rect(r.x + 14, r.y + 12, 40, 40), "E");
                DevaText.Label(new Rect(r.x + 68, r.y + 10, 480, 44), ilabel, new GUIStyle(Retro.Body) { alignment = TextAnchor.MiddleLeft });
                if (G.hold.HasValue) Retro.Meter(new Rect(r.x + 68, r.y + 50, 470, 10), G.hold.Value.time / G.hold.Value.duration);
            }
            var hint = G.player.InCar ? "W/S drive · A/D steer · SPACE handbrake · H horn · Q radio · T passenger shift · V camera · E get out" : "WASD move · SHIFT run · E interact · G emote · V camera · TAB switch · M map · J stories · P postcard · F1 hide HUD";
            var bar = new Rect(18, H - 52, 960, 36); Retro.Fill(bar, new Color(.1f, .05f, .04f, .8f)); Retro.Dhaka(new Rect(bar.x, bar.y, 8, bar.height));
            GUI.Label(new Rect(bar.x + 18, bar.y + 7, 940, 26), hint, new GUIStyle(Retro.Small) { normal = { textColor = Retro.Paper } });
        }

        void FirstVisitHints()
        {
            if (G.guide == null || !G.progress.showFirstVisitHints || PanelOpen || ShowWork || G.InDialogue || G.activities.Current != null) return;
            var stage = G.guide.Stage(G.player.InCar); if (stage == FirstStep.Done) return;
            var card = new Rect(18, 154, 400, 184); Retro.PaperPanel(card, false);
            GUI.Label(new Rect(card.x + 16, card.y + 12, 368, 22), $"FIRST STEPS  {(int)stage + 1}/8  /  F2 hides tips", Retro.Eyebrow);
            GUI.Label(new Rect(card.x + 16, card.y + 40, 368, 34), G.guide.Title(G.player.InCar), new GUIStyle(Retro.Heading) { fontSize = 22 });
            GUI.Label(new Rect(card.x + 16, card.y + 80, 366, 96), G.guide.Hint(G.player.InCar), new GUIStyle(Retro.Body) { fontSize = 18, wordWrap = true });
        }

        /// <summary>Where the player is being sent: passenger job, waypoint, accepted street event or mission step.</summary>
        public Vector3? CurrentGoal()
        {
            if (G.job.Target.HasValue) return G.job.Target;
            if (G.navTarget.HasValue) return G.navTarget;
            if (G.events.Current is var ev && ev != null && ev.accepted && ev.kind == StreetEvents.Kind.Shopping) return ev.position;
            if (G.progress.showFirstVisitHints && G.progress.tracked == "arrival" && G.guide?.Stage(G.player.InCar) == FirstStep.FindCar) return G.car.transform.position;
            return G.missions.ObjectivePosition(G.citizens);
        }

        // ───────────── Kurosawa wipe ─────────────
        // A hard-edged line sweeps across the screen, pushing the old shot off and revealing the new one.
        Texture2D wipeTex; float wipeStart = -10; const float WipeTime = .7f;

        /// <summary>Freezes the current frame, runs <paramref name="change"/> (a camera cut), then wipes to the new shot.</summary>
        public void Wipe(System.Action change) => StartCoroutine(WipeRoutine(change));

        System.Collections.IEnumerator WipeRoutine(System.Action change)
        {
            yield return new WaitForEndOfFrame();
            if (wipeTex) Destroy(wipeTex);
            // Copy into an opaque sRGB texture: the raw capture carries alpha and is read as linear, which washed it out.
            var raw = ScreenCapture.CaptureScreenshotAsTexture();
            wipeTex = new Texture2D(raw.width, raw.height, TextureFormat.RGB24, false, false); wipeTex.SetPixels32(raw.GetPixels32()); wipeTex.Apply(false, true);
            Destroy(raw); wipeStart = Time.unscaledTime;
            change?.Invoke();
        }

        void DrawWipe(float W, float H)
        {
            if (!wipeTex) return;
            float t = (Time.unscaledTime - wipeStart) / WipeTime; if (t >= 1) { Destroy(wipeTex); wipeTex = null; return; }
            t = t * t * (3 - 2 * t); float edge = t * W;
            GUI.DrawTextureWithTexCoords(new Rect(edge, 0, W - edge, H), wipeTex, new Rect(t, 0, 1 - t, 1));
            Retro.Fill(new Rect(edge - 2, 0, 3, H), new Color(0, 0, 0, .55f)); // the wipe's leading edge
        }

        /// <summary>F1: hide the HUD to just look at the valley.</summary>
        public bool hudHidden;

        /// <summary>A folk tale card, like a page from an old storybook.</summary>
        void TalePopup(float W)
        {
            var pop = G.folklore.Popup; if (Time.time > pop.until) return;
            var r = new Rect(W / 2 - 420, 120, 840, 215); Retro.PaperPanel(r); Retro.Dhaka(new Rect(r.x + 3, r.y + 3, r.width - 6, 10));
            DevaText.Label(new Rect(r.x + 30, r.y + 24, 780, 24), "लोक कथा · LOK KATHA · A TALE OF THE VALLEY", Retro.Eyebrow);
            GUI.Label(new Rect(r.x + 30, r.y + 50, 780, 36), pop.title, Retro.Heading);
            GUI.Label(new Rect(r.x + 30, r.y + 90, 780, 115), pop.text, new GUIStyle(Retro.Body) { fontSize = 17, fontStyle = FontStyle.Italic, wordWrap = true });
        }

        /// <summary>North-up radar with roads, the lake, places, pastimes, jobs, events and other protagonists.</summary>
        void Radar(float W, float H)
        {
            if (G.progress.guidance == 1) return; // Explorer mode: read the land, follow the kite
            var r = new Rect(18, H - 330, 264, 264); Retro.PaperPanel(r, false); var m = new Rect(r.x + 8, r.y + 8, r.width - 16, r.height - 16);
            Retro.Fill(m, new Color(.83f, .8f, .66f));
            var p = G.player.transform.position; float range = 150, scale = m.width / (range * 2);
            Vector2 Map(float x, float z) => new Vector2(m.center.x + (x - p.x) * scale, m.center.y - (z - p.z) * scale);
            if (p.x + range > World.LakeEdge) { float lx = Mathf.Max(m.x, Map(World.LakeEdge, 0).x); Retro.Fill(new Rect(lx, m.y, m.xMax - lx, m.height), new Color(.35f, .55f, .62f)); }
            var road = new Color(.32f, .3f, .28f);
            for (int k = World.Min; k <= World.Max + 1; k++)
            {
                float c = k * 80;
                if (Mathf.Abs(c - p.x) < range && c < 440) { float x = Map(c, 0).x; Retro.Fill(new Rect(x - 3.5f * scale, m.y, 7 * scale, m.height), road); }
                if (Mathf.Abs(c - p.z) < range) { float y = Map(0, c).y; float x1 = Mathf.Min(m.xMax, Map(436, 0).x); if (x1 > m.x) Retro.Fill(new Rect(m.x, y - 3.5f * scale, x1 - m.x, 7 * scale), road); }
            }
            void Blip(Vector3 at, Color c, float size, string glyph = null, bool clampToEdge = false)
            {
                var q = Map(at.x, at.z); var d = q - m.center; float max = m.width / 2 - 6;
                if (Mathf.Abs(d.x) > max || Mathf.Abs(d.y) > max) { if (!clampToEdge) return; float k = max / Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)); q = m.center + d * k; }
                if (glyph == null) { Retro.Fill(new Rect(q.x - size / 2 - 1, q.y - size / 2 - 1, size + 2, size + 2), Retro.Ink); Retro.Fill(new Rect(q.x - size / 2, q.y - size / 2, size, size), c); }
                else GUI.Label(new Rect(q.x - 10, q.y - 12, 24, 24), glyph, new GUIStyle(Retro.Label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = c } });
            }
            foreach (var poi in G.data.pois) Blip(new Vector3(poi.x, 0, poi.z), Retro.Crimson, 8);
            foreach (var a in G.activities.all) if (a.Available && a.id != "junkiri" && a.id != "event") Blip(a.position, new Color(.2f, .55f, .2f), 6);
            for (int i = 0; i < G.heroes.heroes.Count; i++) if (G.heroes.heroes[i].idle) Blip(G.heroes.heroes[i].idle.transform.position, Retro.FlagBlue, 0, G.heroes.heroes[i].name.Substring(0, 1), true);
            if (G.events.Current != null) Blip(G.events.Current.position, new Color(.1f, .35f, .9f), 11, null, true);
            if (G.job.Target.HasValue) Blip(G.job.Target.Value, new Color(.15f, .75f, .25f), 13, null, true);
            var goal = G.navTarget ?? G.missions.ObjectivePosition(G.citizens); if (goal.HasValue && !G.job.OnDuty) Blip(goal.Value, Retro.Gold, 0, "◆", true);
            // The player arrow points where the camera looks.
            var old = GUI.matrix; GUIUtility.RotateAroundPivot(G.rig.transform.eulerAngles.y, m.center);
            GUI.Label(new Rect(m.center.x - 12, m.center.y - 14, 24, 24), "▲", new GUIStyle(Retro.Label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, normal = { textColor = Retro.Crimson } });
            GUI.matrix = old;
            GUI.Label(new Rect(m.center.x - 10, m.y + 2, 20, 18), "N", new GUIStyle(Retro.Eyebrow) { alignment = TextAnchor.UpperCenter });
        }

        /// <summary>A wooden 2000s radio set while driving.</summary>
        void RadioPanel(float W)
        {
            if (!G.player.InCar) return; var st = CarRadio.Stations[G.radio.station]; bool big = Time.time < G.radio.ShowUntil;
            var r = new Rect(W / 2 - (big ? 300 : 210), 18, big ? 600 : 420, big ? 108 : 58);
            Retro.Fill(new Rect(r.x + 5, r.y + 7, r.width, r.height), new Color(0, 0, 0, .4f)); Retro.Fill(r, new Color(.35f, .2f, .1f)); Retro.Fill(new Rect(r.x + 4, r.y + 4, r.width - 8, r.height - 8), new Color(.48f, .3f, .16f));
            Retro.LcdPanel(new Rect(r.x + 12, r.y + 10, 110, 38), st.freq);
            GUI.Label(new Rect(r.x + 134, r.y + 10, r.width - 150, 24), st.name.ToUpperInvariant(), new GUIStyle(Retro.Eyebrow) { normal = { textColor = Retro.Paper } });
            GUI.Label(new Rect(r.x + 134, r.y + 30, r.width - 150, 20), "Q · tune", new GUIStyle(Retro.Small) { fontSize = 13, normal = { textColor = new Color(.95f, .85f, .7f) } });
            if (big && G.radio.station < 3) GUI.Label(new Rect(r.x + 16, r.y + 58, r.width - 32, 44), G.radio.Ticker, new GUIStyle(Retro.Body) { fontSize = 16, fontStyle = FontStyle.Italic, normal = { textColor = Retro.Paper } });
        }

        void MemoryPopup(float W)
        {
            var pop = G.memories.Popup; if (Time.time > pop.until) return;
            var r = new Rect(W / 2 - 360, 130, 720, 190); Retro.PaperPanel(r);
            GUI.Label(new Rect(r.x + 30, r.y + 24, 660, 22), "SAMJHANA · A MEMORY OF THE 2000s", Retro.Eyebrow);
            GUI.Label(new Rect(r.x + 30, r.y + 46, 660, 36), pop.title, Retro.Heading);
            GUI.Label(new Rect(r.x + 30, r.y + 84, 660, 90), pop.text, new GUIStyle(Retro.Body) { fontStyle = FontStyle.Italic });
        }

        /// <summary>Character switch wheel: three lives in the same valley.</summary>
        void Wheel(float W, float H)
        {
            Retro.Fill(new Rect(0, 0, W, H), new Color(0, 0, 0, .45f));
            GUI.Label(new Rect(W / 2 - 400, H / 2 - 270, 800, 40), "SWITCH CHARACTER · press 1, 2 or 3 (TAB to close)", new GUIStyle(Retro.Eyebrow) { fontSize = 18, alignment = TextAnchor.UpperCenter, normal = { textColor = Retro.Paper } });
            var list = G.heroes.heroes;
            for (int i = 0; i < list.Count; i++)
            {
                var h = list[i]; bool current = i == G.progress.hero; var r = new Rect(W / 2 - 570 + i * 390, H / 2 - 210, 360, 400); Retro.PaperPanel(r);
                Retro.Fill(new Rect(r.x + 24, r.y + 28, 70, 70), h.outfit); GUI.Label(new Rect(r.x + 24, r.y + 28, 70, 70), Retro.Devanagari(i + 1), new GUIStyle(Retro.Title) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Retro.Paper } });
                GUI.Label(new Rect(r.x + 108, r.y + 30, 230, 34), h.name, Retro.Heading); GUI.Label(new Rect(r.x + 108, r.y + 66, 230, 24), h.role.ToUpperInvariant(), Retro.Eyebrow);
                GUI.Label(new Rect(r.x + 24, r.y + 118, 312, 160), h.bio, Retro.Body);
                var pos = current ? G.player.transform.position : h.idle ? h.idle.transform.position : h.home;
                GUI.Label(new Rect(r.x + 24, r.y + 286, 312, 24), $"Now in {World.DistrictAt(pos.x, pos.z).name}", Retro.Small);
                if (current) GUI.Label(new Rect(r.x + 24, r.y + 330, 312, 44), "Playing now", new GUIStyle(Retro.Heading) { fontSize = 20, alignment = TextAnchor.MiddleCenter, normal = { textColor = Retro.Crimson } });
                else if (GUI.Button(new Rect(r.x + 24, r.y + 330, 312, 48), $"Switch to {h.name.Split(' ')[0]}", Retro.TicketGold)) { wheel = false; G.heroes.Switch(i); }
            }
        }

        void WorldLabels(float scale)
        {
            var cam = Camera.main; if (!cam) return; var p = G.player.transform.position;
            void Label(Vector3 at, string text, GUIStyle style, bool plate)
            {
                var s = cam.WorldToScreenPoint(at); if (s.z <= 0) return;
                var size = style.CalcSize(new GUIContent(text)); var r = new Rect(s.x / scale - size.x / 2, (Screen.height - s.y) / scale - size.y, size.x, size.y);
                if (plate) Retro.Fill(new Rect(r.x - 8, r.y - 2, r.width + 16, r.height + 4), new Color(.38f, .03f, .05f, .82f));
                GUI.Label(r, text, style);
            }
            var nameStyle = new GUIStyle(Retro.Label) { fontSize = 15, normal = { textColor = Retro.Paper } };
            foreach (var c in G.citizens.agents) if (c.named && c.gameObject.activeSelf && World.DistanceXZ(c.transform.position, p) < 22) Label(c.transform.position + Vector3.up * 2.2f, c.displayName, nameStyle, true);
            var placeStyle = new GUIStyle(Retro.Eyebrow) { normal = { textColor = Retro.Gold } };
            foreach (var poi in G.data.pois) if (World.DistanceXZ(p, poi.x, poi.z) < 45) Label(G.data.PoiPosition(poi.id) + Vector3.up * 3.5f, poi.name.ToUpperInvariant(), placeStyle, true);
        }

        // FM-radio style notifications.
        void Toasts(float W, float H)
        {
            float y = 330;
            foreach (var t in toasts)
            {
                var r = new Rect(W - 560, y, 542, 62); Retro.PaperPanel(r, false); Retro.Fill(new Rect(r.x + 3, r.y + 3, 6, r.height - 6), t.success ? Retro.Gold : Retro.Crimson);
                GUI.Label(new Rect(r.x + 20, r.y + 6, 300, 18), t.success ? "◉ RADIO PHEWA 90.4 FM · KHUSI KHABAR" : "◉ RADIO PHEWA 90.4 FM", new GUIStyle(Retro.Eyebrow) { fontSize = 11 });
                GUI.Label(new Rect(r.x + 20, r.y + 22, r.width - 34, 38), t.text, new GUIStyle(Retro.Small) { fontSize = 15, normal = { textColor = Retro.Ink } });
                y += 70;
            }
        }

        void DialogueBox(float W, float H)
        {
            var c = G.dialogue.Current; var r = new Rect(W / 2 - 540, H - 490, 1080, 450); Retro.PaperPanel(r);
            GUILayout.BeginArea(new Rect(r.x + 32, r.y + 26, r.width - 64, r.height - 46));
            GUILayout.BeginHorizontal(); GUILayout.Label(c.agent.role.ToUpperInvariant(), Retro.Eyebrow); GUILayout.FlexibleSpace(); GUILayout.Label("GAFF · CONVERSATION · Esc to leave", Retro.Eyebrow); GUILayout.EndHorizontal();
            GUILayout.Label(c.agent.displayName, Retro.Heading); GUILayout.Space(4);
            GUILayout.Label($"“{c.text}”", new GUIStyle(Retro.Body) { fontSize = 20, fontStyle = FontStyle.Italic }); GUILayout.Space(8);
            for (int i = 0; i < c.options.Count; i++)
            {
                var o = c.options[i]; GUI.enabled = !o.disabled;
                if (GUILayout.Button($"{Retro.Devanagari(i + 1)}   {o.text}{(o.disabled ? "   · needs 4 reputation" : "")}", Retro.Ticket, GUILayout.Height(38))) { G.dialogue.Choose(o.id); GUI.enabled = true; GUILayout.EndArea(); return; }
                GUI.enabled = true; GUILayout.Space(3);
            }
            GUILayout.Space(6); GUILayout.BeginHorizontal();
            GUI.SetNextControlName("say"); say = GUILayout.TextField(say, 200, Retro.Field, GUILayout.Height(34));
            bool send = GUILayout.Button("Say ↵", Retro.TicketGold, GUILayout.Width(150), GUILayout.Height(34)) || Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "say";
            GUILayout.EndHorizontal();
            GUILayout.Label("Type a topic (work, weather, directions, the ghat…) or pick a reply with 1–6.", Retro.Small);
            GUILayout.EndArea();
            if (send && say.Trim().Length > 0) { var text = say; say = ""; G.dialogue.Say(text); }
        }

        void WorkPanel(float W, float H)
        {
            var state = G.progress.Mission("cafe"); if (state == null) return; var steps = G.missions.Steps("cafe");
            var r = new Rect(18, 330, 400, 330); Retro.PaperPanel(r);
            GUILayout.BeginArea(new Rect(r.x + 22, r.y + 24, r.width - 44, r.height - 40));
            GUILayout.BeginHorizontal(); GUILayout.Label("CHIYA PASAL · TEA SHOP", Retro.Eyebrow); GUILayout.FlexibleSpace(); GUILayout.Label($"{Mathf.CeilToInt(state.remaining)}s", Retro.Eyebrow); GUILayout.EndHorizontal();
            GUILayout.Label(state.step < steps.Length ? steps[state.step].text : "Shift complete", Retro.Body);
            if (G.work.HasValue) { var bar = GUILayoutUtility.GetRect(300, 14); Retro.Meter(bar, G.work.Value.time / G.work.Value.duration); }
            foreach (var (action, label) in new[] { ("brew", "☕  Brew milk chiya"), ("pack", "▣  Pack the momos"), ("serve", "↗  Serve the customer") }) { if (GUILayout.Button(label, Retro.Ticket, GUILayout.Height(42))) G.StartWork(action); GUILayout.Space(3); }
            if (GUILayout.Button("Step away (Esc)", Retro.TicketGold)) ShowWork = false;
            GUILayout.EndArea();
        }

        Rect Centered(float W, float H, float w, float h) => new Rect(W / 2 - w / 2, H / 2 - h / 2, w, h);

        void Journal(float W, float H)
        {
            var r = Centered(W, H, 980, 840); Retro.Notebook(r);
            GUILayout.BeginArea(new Rect(r.x + 56, r.y + 20, r.width - 90, r.height - 40));
            GUILayout.Label("MERO KAPI · MY COPY", Retro.Eyebrow); GUILayout.Label("Stories & work", new GUIStyle(Retro.Title) { fontSize = 38 });
            GUILayout.Label($"रु {Mathf.FloorToInt(G.progress.cash):N0} · {G.progress.reputation} reputation · honest-work bonus +{(G.progress.HonestBonus - 1) * 100:0}% · lattu wins {G.progress.lattuBest} · best chungi {G.progress.chungiBest} · kites cut {G.progress.kitesCut} · junkiri {G.progress.fireflies}", Retro.Small);
            journalScroll = GUILayout.BeginScrollView(journalScroll);
            var pen = new GUIStyle(Retro.Body) { normal = { textColor = new Color(.1f, .15f, .45f) } };
            foreach (var def in G.data.missions)
            {
                var state = G.progress.Mission(def.id); bool active = state?.status == "active", done = state?.status == "completed" && !def.repeatable, locked = !G.missions.Unlocked(def.id);
                var steps = G.missions.Steps(def.id); string step = active && state.step < steps.Length ? steps[state.step].text : null;
                GUILayout.Space(10); GUILayout.Label($"{def.category} · {(done ? "COMPLETED ✓" : locked ? "LOCKED" : active ? "IN PROGRESS" : def.repeatable ? "REPEATABLE" : "AVAILABLE")}", Retro.Eyebrow);
                GUILayout.Label((done ? "✓ " : "") + def.title, new GUIStyle(Retro.Heading) { fontSize = 22 }); GUILayout.Label(locked ? def.requirement : step ?? def.description, pen);
                GUILayout.BeginHorizontal(); GUILayout.Label($"रु {def.reward} + {def.reputation} REP", new GUIStyle(Retro.Body) { normal = { textColor = Retro.Crimson } }, GUILayout.Width(300)); GUILayout.FlexibleSpace();
                GUI.enabled = !locked && !done;
                if (GUILayout.Button(done ? "Complete" : active ? (G.progress.tracked == def.id ? "Tracking ✓" : "Track") : "Start ↗", Retro.Ticket, GUILayout.Width(170))) { G.navTarget = null; if (active) G.progress.tracked = def.id; else G.missions.Accept(def.id); }
                GUI.enabled = true; GUILayout.EndHorizontal();
            }
            GUILayout.Space(14); GUILayout.Label("PASTIMES · KHEL", Retro.Eyebrow);
            GUILayout.Label("Lattu, chungi and luki-chhupi with the kids in Durbar Square · Changa chet at Sarangkot (and the square during Dashain) · Ping during Dashain · Haircut at Hari Salon · Phewa Cyber · Cassette at the chiya pasal · Junkiri on dark nights by the stupa, lake and hills · Passenger shifts with T in any vehicle.", pen);
            GUILayout.Space(14); GUILayout.Label($"SAMJHANA · MEMORIES FOUND {G.progress.memories.Count}/{Memories.All.Length}", Retro.Eyebrow);
            foreach (var mem in Memories.All) GUILayout.Label(G.progress.memories.Contains(mem.id) ? $"✦ {mem.title}: {mem.text}" : "✧ ……… still hidden somewhere in the valley", pen);
            GUILayout.Space(14); GUILayout.Label($"LOK KATHA · FOLK TALES {G.progress.tales.Count}/{Folklore.All.Length}  (look for stone tablets under tall prayer-flag poles)", Retro.Eyebrow);
            foreach (var tale in Folklore.All) GUILayout.Label(G.progress.tales.Contains(tale.id) ? $"❧ {tale.title}: {tale.text}" : "❧ ……… a tale still waiting at one of the valley's old places", pen);
            GUILayout.EndScrollView();
            if (GUILayout.Button("Close (J)", Retro.TicketGold, GUILayout.Height(40))) panel = Panel.None;
            GUILayout.EndArea();
        }

        void Map(float W, float H)
        {
            var r = Centered(W, H, 1520, 880); Retro.PaperPanel(r);
            DevaText.Label(new Rect(r.x + 30, r.y + 24, 600, 24), "TOLE KO NAKSA · टोलको नक्सा", Retro.Eyebrow); GUI.Label(new Rect(r.x + 30, r.y + 46, 600, 44), "Phewa Valley", Retro.Heading);
            // Hand-drawn map on paper: x → right, z → down, the lake on the right.
            var m = new Rect(r.x + 30, r.y + 100, 760, 740); GUI.DrawTexture(m, Retro.PaperTex); Retro.Fill(new Rect(m.x, m.y, m.width, 2), Retro.Ink);
            Vector2 ToMap(float x, float z) => new Vector2(m.x + (x + 480) / 1040 * m.width, m.y + (560 - z) / 1040 * m.height); // north up
            var lake = ToMap(436, -480); Retro.Fill(new Rect(lake.x, m.y, m.xMax - lake.x, m.height), new Color(.35f, .55f, .62f, .75f));
            DevaText.Label(new Rect(lake.x + 10, m.y + m.height / 2, 160, 30), "फेवा ताल", new GUIStyle(Retro.Deva20) { normal = { textColor = Retro.FlagBlue } });
            for (int cx = World.Min; cx <= World.Max; cx++) for (int cz = World.Min; cz <= World.Max; cz++)
            {
                if (cx * 80 > 400) continue; var a = ToMap(cx * 80 + 10, cz * 80 + 10); var b = ToMap(cx * 80 + 70, cz * 80 + 70);
                var dc = World.DistrictAt(cx * 80 + 40, cz * 80 + 40).color; Retro.Fill(Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y)), new Color(dc.r, dc.g, dc.b, .45f));
            }
            foreach (var poi in G.data.pois) { var q = ToMap(poi.x, poi.z); Retro.Fill(new Rect(q.x - 6, q.y - 6, 12, 12), Retro.Crimson); GUI.Label(new Rect(q.x + 9, q.y - 12, 230, 24), poi.name, new GUIStyle(Retro.Small) { fontSize = 13, fontStyle = FontStyle.Bold, wordWrap = false }); }
            foreach (var a in G.activities.all) if (a.Available && !(a is FireflyCatch.CatchActivity)) { var q = ToMap(a.position.x, a.position.z); GUI.Label(new Rect(q.x - 7, q.y - 9, 20, 20), "✿", new GUIStyle(Retro.Body) { fontSize = 14, normal = { textColor = new Color(.2f, .45f, .2f) } }); }
            var pp = G.player.transform.position; var me = ToMap(pp.x, pp.z); Retro.Fill(new Rect(me.x - 7, me.y - 7, 14, 14), Retro.FlagBlue); DevaText.Label(new Rect(me.x + 9, me.y - 12, 80, 24), "तपाईं", new GUIStyle(Retro.Deva20) { fontSize = 15, normal = { textColor = Retro.FlagBlue } });
            if (G.navTarget.HasValue) { var n = ToMap(G.navTarget.Value.x, G.navTarget.Value.z); GUI.Label(new Rect(n.x - 10, n.y - 30, 30, 30), "◆", new GUIStyle(Retro.Heading) { normal = { textColor = Retro.Crimson } }); }
            GUILayout.BeginArea(new Rect(r.x + 820, r.y + 100, 670, 750));
            GUILayout.Label("PLACES   (✿ = pastimes)", Retro.Eyebrow);
            mapScroll = GUILayout.BeginScrollView(mapScroll, GUILayout.Height(570));
            foreach (var poi in G.data.pois)
            {
                bool sel = selectedPlace == poi.id;
                if (GUILayout.Button($"{(sel ? "◆" : "⌖")}  {poi.name}\n<size=13>{poi.description}</size>", sel ? Retro.TicketGold : Retro.Ticket, GUILayout.Height(66))) { selectedPlace = poi.id; G.Navigate(poi.id); }
                GUILayout.Space(3);
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            GUI.enabled = selectedPlace != null; if (GUILayout.Button("Micro bus · रु 10", Retro.TicketGold, GUILayout.Height(46)) && G.TakeMicroBus(selectedPlace)) panel = Panel.None; GUI.enabled = true;
            if (GUILayout.Button("Clear waypoint", Retro.Ticket, GUILayout.Height(46))) { G.navTarget = null; selectedPlace = null; }
            if (GUILayout.Button("Close (M)", Retro.Ticket, GUILayout.Height(46))) panel = Panel.None;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void Menu(float W, float H)
        {
            var r = Centered(W, H, 660, 760); Retro.PaperPanel(r);
            GUILayout.BeginArea(new Rect(r.x + 36, r.y + 30, r.width - 72, r.height - 56));
            GUILayout.Label("AURORA / PHEWA VALLEY", Retro.Eyebrow); GUILayout.Label("Chiya break · a tea break.", Retro.Heading);
            GUILayout.Label("Chiya, chungi, load shedding and a whole tole of stories.", Retro.Small); GUILayout.Space(12);
            if (GUILayout.Button("Return to the valley  ↗", Retro.TicketGold, GUILayout.Height(48))) panel = Panel.None;
            GUILayout.Space(4); if (GUILayout.Button("Save progress", Retro.Ticket, GUILayout.Height(40))) { G.Save(); Toast("Your visit has been saved.", true); }
            GUILayout.Space(4); if (GUILayout.Button("Recover your car", Retro.Ticket, GUILayout.Height(40))) { G.car.Recover(G.player.transform.position); panel = Panel.None; }
            GUILayout.Space(10);
            G.progress.sound = GUILayout.Toggle(G.progress.sound, $"  Spoken conversations {(G.audioDirector.HasVoices ? "(ElevenLabs voices)" : "(generate voices with npm run assets:audio)")}", Retro.Body);
            G.progress.showFirstVisitHints = GUILayout.Toggle(G.progress.showFirstVisitHints, "  First-visit guidance (F2)", Retro.Body);
            G.progress.ambience = GUILayout.Toggle(G.progress.ambience, "  Music, radio, cassettes & ambience", Retro.Body);
            GUILayout.BeginHorizontal(); GUILayout.Label("Traffic & crowds", Retro.Body, GUILayout.Width(250)); if (GUILayout.Button(new[] { "Low", "Normal", "High" }[G.progress.density], Retro.Ticket, GUILayout.Width(150))) G.progress.density = (G.progress.density + 1) % 3; GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(); GUILayout.Label("Graphics", Retro.Body, GUILayout.Width(250)); if (GUILayout.Button(CinematicLook.QualityNames[G.progress.graphics], Retro.Ticket, GUILayout.Width(150))) { G.progress.graphics = (G.progress.graphics + 1) % 3; G.look.Apply(G.progress.graphics); } GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(); GUILayout.Label("Draw distance", Retro.Body, GUILayout.Width(250)); if (GUILayout.Button(new[] { "Near", "Normal", "Far" }[G.progress.drawDistance], Retro.Ticket, GUILayout.Width(150))) { G.progress.drawDistance = (G.progress.drawDistance + 1) % 3; G.streamer.radius = new[] { 2, 3, 4 }[G.progress.drawDistance]; } GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(); GUILayout.Label("Film look", Retro.Body, GUILayout.Width(250)); if (GUILayout.Button(CinematicLook.FilmNames[G.progress.film], Retro.Ticket, GUILayout.Width(220))) { G.progress.film = (G.progress.film + 1) % CinematicLook.FilmNames.Length; G.look.film = G.progress.film; G.look.Apply(G.progress.graphics); } GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(); GUILayout.Label("Guidance", Retro.Body, GUILayout.Width(250)); if (GUILayout.Button(new[] { "Radar & distance", "Explorer (kite)" }[G.progress.guidance], Retro.Ticket, GUILayout.Width(220))) { G.progress.guidance = 1 - G.progress.guidance; Toast(G.progress.guidance == 1 ? "Explorer: no radar. Follow the red kite and read the land." : "Radar and distances are back.", false); } GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.Label($"Models:{(G.models.Loaded.Any() ? string.Join(", ", G.models.Loaded) : "built-in")}", Retro.Small);
            GUILayout.Label($"Tripo .glb folder:\n{ModelLibrary.ModelsFolder}", Retro.Small);
            GUILayout.FlexibleSpace();
            if (!confirmReset) { if (GUILayout.Button("Start a new visit", Retro.Ticket)) confirmReset = true; }
            else { GUILayout.Label("Your local progress will be replaced.", new GUIStyle(Retro.Body) { normal = { textColor = Retro.Crimson } }); GUILayout.BeginHorizontal(); if (GUILayout.Button("Start fresh", Retro.Ticket)) G.NewVisit(); if (GUILayout.Button("Keep exploring", Retro.TicketGold)) confirmReset = false; GUILayout.EndHorizontal(); }
            GUILayout.Space(4); if (GUILayout.Button("Quit to desktop", Retro.Ticket)) { G.Save(); Application.Quit(); }
            GUILayout.EndArea();
        }
    }
}

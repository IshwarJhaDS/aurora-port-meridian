// Entry point. Boots automatically in any scene (no prefabs or scene setup needed),
// loads world.json + optional Tripo models, builds the valley and runs the game loop.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PhewaValley
{
    public enum Interaction { None, ExitCar, Repair, Work, Talk, EnterCar, Place, Activity }

    public class Game : MonoBehaviour
    {
        public static Game Instance { get; private set; }
        public WorldData data; public Progress progress; public Missions missions; public Dialogue dialogue;
        public Citizens citizens; public PlayerController player; public CarController car; public CameraRig rig;
        public DayNight dayNight; public AudioDirector audioDirector; public Activities activities; public StreetLife streetLife; public CinematicLook look; public Protagonists heroes; public StreetEvents events; public CarRadio radio; public PassengerJob job; public Memories memories; public Folklore folklore; Activity nearActivity; float lastMinutes = -1; bool lastPowerCut; public ChunkStreamer streamer; public ModelLibrary models; public GameUI ui;
        public bool Ready { get; private set; } public bool Started { get; private set; } public string LoadingText = "Preparing the valley…";
        public (Interaction kind, string label, string target) interaction;
        public Vector3? navTarget; public string navLabel, navId;
        public (string action, float time, float duration)? work; public (string target, float time, float duration)? hold;
        public FirstVisitGuide guide; Vector3 lastGuidePosition; bool lastGuideInCar;
        float saveTimer;

        void LateUpdate() { if (Ready) Time.timeScale = Paused ? 0 : 1; }
        void OnDestroy() { Time.timeScale = 1; if (Instance == this) Instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = System.Environment.GetCommandLineArgs();
            if (System.Array.IndexOf(args, "-phewa-verify") >= 0) { new GameObject("Controller checks").AddComponent<ControllerChecks>(); return; }
            int preview = System.Array.IndexOf(args, "-phewa-preview");
            if (preview >= 0 && preview + 2 < args.Length) { var p = new GameObject("Model Preview").AddComponent<ModelPreview>(); p.input = args[preview + 1]; p.output = args[preview + 2]; return; }
            int clips = System.Array.IndexOf(args, "-phewa-clips");
            if (clips >= 0 && clips + 2 < args.Length) { var c = new GameObject("Clip Inspector").AddComponent<ClipInspector>(); c.input = args[clips + 1]; c.output = args[clips + 2]; return; }
            if (!FindAnyObjectByType<Game>()) new GameObject("Phewa Valley").AddComponent<Game>();
        }

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this; Application.targetFrameRate = 120;
            foreach (var c in FindObjectsByType<Camera>()) if (c.gameObject != gameObject) c.gameObject.SetActive(false);
            foreach (var l in FindObjectsByType<Light>()) if (l.type == LightType.Directional) l.gameObject.SetActive(false);
            ui = gameObject.AddComponent<GameUI>();
        }

        async void Start()
        {
            try { data = WorldData.Load(); }
            catch (System.Exception e) { LoadingText = e.Message; Debug.LogError(e); return; }
            progress = Progress.Load();
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-phewa-smoke") >= 0 && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-phewa-fresh") >= 0) progress = new Progress();
            progress.sessionOnly = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-phewa-smoke") >= 0;
            models = new ModelLibrary(transform);
            await models.LoadAll(data, text => LoadingText = text);
            var landmarks = new GameObject("Landmarks").transform; landmarks.SetParent(transform, false);
            var replaced = models.PlaceLandmarks(data, landmarks);

            BuildEnvironment(!replaced.Contains("boats"));
            dayNight = new GameObject("Day & Night").AddComponent<DayNight>(); dayNight.transform.SetParent(transform, false); dayNight.Init();

            player = new GameObject("Player").AddComponent<PlayerController>(); player.transform.SetParent(transform, false);
            player.transform.position = progress.player + Vector3.up * .1f;
            { var h = Protagonists.Roster; int i = Mathf.Clamp(progress.hero, 0, h.Count - 1); player.Init(models, h[i].model, h[i].outfit, i == 2 ? 1.6f : 1.75f); }
            car = new GameObject("Maruti").AddComponent<CarController>(); car.transform.SetParent(transform, false);
            car.transform.SetPositionAndRotation(progress.car, Quaternion.Euler(0, 180, 0)); car.Init(models);
            rig = new GameObject("Camera").AddComponent<CameraRig>(); rig.transform.SetParent(transform, false); rig.SetTarget(player.transform);
            look = gameObject.AddComponent<CinematicLook>(); look.Init(rig.GetComponent<Camera>(), progress.graphics);

            streamer = new GameObject("City").AddComponent<ChunkStreamer>(); streamer.transform.SetParent(transform, false);
            streamer.Init(new CityGenerator(data, replaced), player.transform, models, data); streamer.Prime(player.transform.position);

            missions = new Missions(data, progress, Notify); guide = new FirstVisitGuide(progress); lastGuidePosition = player.transform.position; missions.Changed += () => ui.MissionsChanged();
            citizens = new GameObject("Citizens").AddComponent<Citizens>(); citizens.transform.SetParent(transform, false);
            citizens.Init(data, player.transform, () => (car.transform.position, Mathf.Abs(car.speed)), models);
            activities = new GameObject("Pastimes").AddComponent<Activities>(); activities.transform.SetParent(transform, false); activities.Init();
            streetLife = new GameObject("Street Life").AddComponent<StreetLife>(); streetLife.transform.SetParent(transform, false);
            heroes = new GameObject("Protagonists").AddComponent<Protagonists>(); heroes.transform.SetParent(transform, false); heroes.Init();
            events = new GameObject("Street Events").AddComponent<StreetEvents>(); events.transform.SetParent(transform, false);
            radio = new GameObject("Car Radio").AddComponent<CarRadio>(); radio.transform.SetParent(transform, false);
            job = new GameObject("Passenger Job").AddComponent<PassengerJob>(); job.transform.SetParent(transform, false);
            memories = new GameObject("Samjhana").AddComponent<Memories>(); memories.transform.SetParent(transform, false);
            folklore = new GameObject("Lok Katha").AddComponent<Folklore>(); folklore.transform.SetParent(transform, false); folklore.Build();
            new GameObject("Guide kite").AddComponent<GuideKite>().transform.SetParent(transform, false);
            streamer.radius = new[] { 2, 3, 4 }[Mathf.Clamp(progress.drawDistance, 0, 2)];
            dialogue = new Dialogue(data, progress, missions, () => dayNight.Rain, Navigate);
            dialogue.Changed += OnDialogue;
            audioDirector = gameObject.AddComponent<AudioDirector>(); audioDirector.Init(data); audioDirector.Enabled = progress.ambience;
            Ready = true;
            if (models.errors.Count > 0) Notify($"{models.errors.Count} model(s) could not load. See the Console.", false);
            var args = System.Environment.GetCommandLineArgs(); int smoke = System.Array.IndexOf(args, "-phewa-smoke");
            if (smoke >= 0) StartCoroutine(SmokeTest(smoke + 1 < args.Length ? args[smoke + 1] : "phewa-smoke.png"));
        }

        /// <summary>Automated check: enters the valley, frames Durbar Square, saves a screenshot and quits.</summary>
        public float SmokeMinutes = 9 * 60 + 30; bool smokeTest; readonly List<Figure> lineup = new List<Figure>();
        System.Collections.IEnumerator SmokeTest(string file)
        {
            smokeTest = true;
            // Optional test framing: -phewa-time 20.5 -phewa-day 2 -phewa-at x z yaw -phewa-play lattu
            var args = System.Environment.GetCommandLineArgs(); string Arg(string name, int k = 1) { int i = System.Array.IndexOf(args, name); return i >= 0 && i + k < args.Length ? args[i + k] : null; }
            float F(string s) => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
            Begin(); progress.minutes = Arg("-phewa-time") != null ? F(Arg("-phewa-time")) * 60 : SmokeMinutes; lastMinutes = progress.minutes;
            if (Arg("-phewa-day") != null) progress.day = int.Parse(Arg("-phewa-day"));
            if (Arg("-phewa-graphics") != null) { progress.graphics = int.Parse(Arg("-phewa-graphics")); look.Apply(progress.graphics); }
            rig.yaw = 225; rig.pitch = 8; rig.distance = 9;
            if (Arg("-phewa-at", 3) != null) { var at = new Vector3(F(Arg("-phewa-at")), 0, F(Arg("-phewa-at", 2))); at.y = World.TerrainHeight(at.x, at.z) + .1f; player.body.enabled = false; player.transform.position = at; player.transform.rotation = Quaternion.Euler(0, F(Arg("-phewa-at", 3)), 0); player.body.enabled = true; rig.yaw = F(Arg("-phewa-at", 3)); streamer.Prime(at); }
            yield return new WaitForSeconds(1);
            if (Arg("-phewa-switch") != null) { heroes.Switch(int.Parse(Arg("-phewa-switch"))); yield return new WaitForSeconds(3.2f); }
            if (Arg("-phewa-drive") != null) { car.Recover(player.transform.position); car.transform.position = player.transform.position + player.transform.right * 2.5f; interaction = (Interaction.EnterCar, "", null); Interact(); rig.yaw = car.transform.eulerAngles.y; car.Launch(F(Arg("-phewa-speed") ?? "9")); radio.ShowUntil = Time.time + 30; if (Arg("-phewa-drive") == "shift") job.Toggle(this); yield return new WaitForSeconds(1.5f); }
            var play = Arg("-phewa-play"); if (play != null) { var a = activities.all.Find(x => x.id == play); if (a != null) { activities.Play(a); for (int i = 0; i < 40; i++) { a.Tick(.05f); } } }
            if (Arg("-phewa-nohud") != null) ui.enabled = false;
            if (Arg("-phewa-guidance") != null) progress.guidance = int.Parse(Arg("-phewa-guidance"));
            progress.film = Arg("-phewa-film") != null ? int.Parse(Arg("-phewa-film")) : 0;
            if (Arg("-phewa-cam") != null) rig.footMode = int.Parse(Arg("-phewa-cam"));
            if (Arg("-phewa-talk") != null) { var c = citizens.Nearest(player.transform.position); if (c == null) { float best = 1e9f; foreach (var a in citizens.agents) { float d2 = (a.transform.position - player.transform.position).sqrMagnitude; if (d2 < best) { best = d2; c = a; } } } if (c != null) { c.transform.position = player.transform.position + player.transform.forward * 2.2f; player.figure.Emote("namaste"); dialogue.Open(c); } }
            smokeTest = true; // keystrokes typed elsewhere must not move the test subject
            if (Arg("-phewa-autodrive", 2) != null) { car.autoDrive = true; car.autoThrottle = F(Arg("-phewa-autodrive")); car.autoSteer = F(Arg("-phewa-autodrive", 2)); }
            if (Arg("-phewa-move") != null) player.autoMove = F(Arg("-phewa-move"));
            yield return new WaitForSeconds(2);
            int frames0 = Time.frameCount; float t0 = Time.realtimeSinceStartup; yield return new WaitForSeconds(4);
            float avg = (Time.frameCount - frames0) / (Time.realtimeSinceStartup - t0);
            if (Arg("-phewa-emote", 2) != null) { player.figure.Emote(Arg("-phewa-emote")); yield return new WaitForSeconds(F(Arg("-phewa-emote", 2))); }
            if (Arg("-phewa-lineup") != null)
            {
                // Every installed character model in a row, facing the camera and looking at it (checks heads and rigs).
                var ids = data.characters.Where(c => c.id != "car" && models.Has(c.id)).Select(c => c.id).ToList();
                int half = int.Parse(Arg("-phewa-lineup")), per = 5; ids = ids.Skip(half * per).Take(per).ToList();
                player.figure.gameObject.SetActive(false); rig.footMode = 3; rig.yaw = player.transform.eulerAngles.y; rig.pitch = 6;
                var fwd = player.transform.forward; var side = player.transform.right;
                for (int i = 0; i < ids.Count; i++)
                {
                    var at = player.transform.position + fwd * 3.4f + side * ((i - (ids.Count - 1) / 2f) * 1.05f); at.y = World.TerrainHeight(at.x, at.z);
                    var go = new GameObject("Lineup " + ids[i]); go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(-fwd));
                    var f = Figure.Create(go.transform, ids[i], Color.gray, new Color(.55f, .35f, .22f), Color.black, null, models, 1.7f);
                    f.LookAt = player.transform.position + Vector3.up * 1.62f; Debug.Log($"[smoke] lineup {i}: {ids[i]}"); lineup.Add(f);
                }
            }
            if (Arg("-phewa-wipe") != null) { ui.Wipe(() => rig.CycleMode()); yield return new WaitForSeconds(F(Arg("-phewa-wipe"))); }
            if (Arg("-phewa-dive") != null) { player.diveNow = true; yield return new WaitForSeconds(F(Arg("-phewa-dive"))); }
            if (Arg("-phewa-knock") != null) { player.Knock(-player.transform.forward * 9); yield return new WaitForSeconds(F(Arg("-phewa-knock"))); }
            if (Arg("-phewa-jump") != null) { player.jumpNow = true; yield return new WaitForSeconds(F(Arg("-phewa-jump"))); }
            yield return null; yield return null;
            if (Arg("-phewa-render-camera") != null) CaptureCamera(file);
            else ScreenCapture.CaptureScreenshot(file);
            yield return new WaitForSeconds(1.5f);
            Debug.Log($"[smoke] player speed={player.Speed:0.00} airborne={player.figure.Airborne} grounded={player.body.isGrounded} y={player.transform.position.y:0.00}");
            Debug.Log($"[smoke] ok chunks={streamer.Resident} citizens={citizens.agents.Count} models={models.Loaded.Count()} fps={avg:0} (4 s average)");
            Application.Quit();
        }

        // Hidden-window smoke tests need an explicit render target instead of an unpresented swap chain.
        void CaptureCamera(string path)
        {
            var camera = rig.GetComponent<Camera>(); var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var target = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally { camera.targetTexture = previousTarget; RenderTexture.active = previousActive; RenderTexture.ReleaseTemporary(target); Destroy(image); }
        }

        void BuildEnvironment(bool proceduralBoats)
        {
            var land = new GameObject("Himalaya & Valley"); land.transform.SetParent(transform, false);
            land.AddComponent<MeshFilter>().sharedMesh = Landmarks.Landscape(proceduralBoats);
            var r = land.AddComponent<MeshRenderer>(); r.sharedMaterial = Materials.VertexLit; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var lake = GameObject.CreatePrimitive(PrimitiveType.Plane); lake.name = "Phewa Lake"; lake.transform.SetParent(transform, false);
            lake.transform.position = new Vector3(740, -1.05f, 40); lake.transform.localScale = new Vector3(61, 1, 140); Destroy(lake.GetComponent<Collider>());
            lake.GetComponent<MeshRenderer>().sharedMaterial = Materials.Water;
            // Invisible edges of the playable valley (the lake shore and the outer limits).
            void Wall(Vector3 c, Vector3 size) { var w = new GameObject("Boundary"); w.transform.SetParent(transform, false); w.transform.position = c; w.AddComponent<BoxCollider>().size = size; }
            Wall(new Vector3(436, 5, 40), new Vector3(1, 20, 1100)); Wall(new Vector3(-482, 5, 40), new Vector3(1, 20, 1100));
            Wall(new Vector3(-22, 5, -482), new Vector3(920, 20, 1)); Wall(new Vector3(-22, 5, 562), new Vector3(920, 20, 1));
            var floor = new GameObject("Safety floor"); floor.transform.SetParent(transform, false); floor.transform.position = new Vector3(-22, -.6f, 40); floor.AddComponent<BoxCollider>().size = new Vector3(920, 1, 1100);
        }

        public void Begin() { if (!Ready) return; Started = true; Notify("Swagatam! Welcome to Phewa Valley. Maya didi is waiting in Durbar Square.", false); }

        public void Notify(string text, bool success) { ui.Toast(text, success); if (success) audioDirector?.Effect("success"); }

        public bool Paused => !Started || ui.PanelOpen;
        public bool InDialogue => dialogue?.Current != null;

        void Update()
        {
            if (!Ready) return;
            float dt = Time.deltaTime; var p = player.transform.position;
            player.inputEnabled = !smokeTest && !Paused &&!InDialogue && !ui.ShowWork && work == null && !(activities.Current?.Freeze ?? false) && !(heroes != null && heroes.Switching);
            if (!Paused)
            {
                bool driving = player.InCar;
                float travelled = Vector3.Distance(p, lastGuidePosition);
                if (driving == lastGuideInCar && travelled <= 40 * dt + .2f) guide.Travel(travelled, driving);
                lastGuidePosition = p; lastGuideInCar = driving;
                progress.minutes = (progress.minutes + dt * .55f) % 1440;
                // A new day starts whenever the clock wraps (including time skips at the salon or cyber).
                if (lastMinutes >= 0 && progress.minutes < lastMinutes - 1) { progress.day++; Notify($"Day {progress.day} · {World.Clock(progress.minutes)}", false); }
                lastMinutes = progress.minutes;
                missions.Update(dt, p, !InDialogue);
                HandleKeys();
                if (hold.HasValue)
                {
                    var step = missions.RepairNear(p); var h = hold.Value;
                    if (!Input.GetKey(KeyCode.E) || step == null || step.target != h.target) hold = null;
                    else { h.time += dt; hold = h; if (h.time >= h.duration) { missions.Event("repair", h.target); hold = null; } }
                }
                if (work.HasValue)
                {
                    var w = work.Value;
                    if (World.DistanceXZ(p, data.PoiPosition("cafe")) > 8) { work = null; Notify("Return to the counter to continue your order.", false); }
                    else { w.time += dt; work = w; if (w.time >= w.duration) { missions.Event("work", w.action, "cafe"); work = null; } }
                }
                if (navTarget.HasValue && World.DistanceXZ(p, navTarget.Value) < 5) { Notify($"Arrived · {navLabel}", false); navTarget = null; }
                saveTimer += dt; if (saveTimer > 10) { Save(); saveTimer = 0; }
            }
            interaction = Started ? FindInteraction() : default;
            var damaged = RepairLamps().Where(id => !progress.repaired.Contains(id)).ToList();
            dayNight.Tick(progress.minutes, progress.day, p, streamer.lamps.Values, damaged);
            look.film = progress.film; look.Tick(dayNight.Night, dayNight.Rain, p, progress.minutes, dayNight.PowerCut);
            if (Started && dayNight.PowerCut != lastPowerCut) Notify(dayNight.PowerCut ? $"Batti gayo! Load shedding until {dayNight.PowerCutUntil}. Light a candle…" : "Batti aayo! The power is back.", false);
            lastPowerCut = dayNight.PowerCut;
            audioDirector.Enabled = progress.ambience;
            audioDirector.Tick(World.DistrictAt(p.x, p.z).id, p, dayNight.Rain, dayNight.Night, Paused);
        }

        IEnumerable<string> RepairLamps() { foreach (var s in data.Mission("repair")?.variants[0].steps ?? new StepData[0]) if (s.kind == "repair") yield return s.target; }

        void HandleKeys()
        {
            if (Input.GetKeyDown(KeyCode.E) && !InDialogue && activities.Current == null) Interact();
            player.figure.Talking = InDialogue;
            // In conversation: turn to face the person and look them in the eye.
            var partner = InDialogue ? dialogue.Current.agent : null;
            player.figure.LookAt = partner ? partner.transform.position + Vector3.up * 1.55f : Time.time < waveUntil && waveAt ? waveAt.position + Vector3.up * 1.55f : (Vector3?)null;
            if (partner && !player.InCar)
            {
                var face = partner.transform.position - player.transform.position; face.y = 0;
                if (face.sqrMagnitude > .01f) player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(face), Time.deltaTime * 5);
            }
            if (InDialogue) return;
            if (Input.GetKeyDown(KeyCode.G) && !player.InCar && activities.Current == null) NextEmote();
            WaveAtFriends();
            if (Input.GetKeyDown(KeyCode.R)) { car.Recover(player.transform.position); Notify("Your Maruti is ready on the nearest road.", false); }
            if (Input.GetKeyDown(KeyCode.H) && player.InCar) audioDirector.Effect("bus_horn");
        }

        /// <summary>G cycles the protagonist's emotes (needs a model with its own animation clips).</summary>
        // Labels match what each Tripo clip really shows (checked frame by frame); "namaste" is procedural.
        static readonly (string[] clips, string label)[] Emotes =
        {
            (new[] { "namaste" }, "Namaste"), (new[] { "Nepali namaste" }, "Wave"), (new[] { "greet_02" }, "Wave hello"), (new[] { "greet_01" }, "Walk & wave"),
            (new[] { "bow" }, "Bow"), (new[] { "agree" }, "Thumbs up"), (new[] { "clap" }, "Clap"), (new[] { "greet_03" }, "Point"), (new[] { "complain_02" }, "Complain"),
            (new[] { "flip" }, "Cartwheel"), (new[] { "chop" }, "Chop"), (new[] { "box_01", "box_02", "box_03" }, "Shadow-box"), (new[] { "front_kick_01", "front_kick_02" }, "Kick"),
            (new[] { "fall" }, "Collapse"),
        };
        int emoteIndex;
        void NextEmote()
        {
            if (!player.figure.HasClips) { Notify("Emotes need an animated model (a Tripo Animation export).", false); return; }
            for (int tries = 0; tries < Emotes.Length; tries++)
            {
                var e = Emotes[emoteIndex++ % Emotes.Length]; var clip = e.clips[Random.Range(0, e.clips.Length)];
                bool ok = clip == "fall"
                    ? player.figure.Action("fall", .3f, 2.9f, 1.2f, Act.PinXZ | Act.NoPlant) && player.figure.Then("dive", 1.75f, 2.71f, .9f, Act.PinXZ | Act.NoPlant) // collapse, then get back up
                    : clip == "greet_01" ? player.figure.Action("greet_01", 0, 3.5f, 1, Act.WhileMoving)
                    : player.figure.Emote(clip);
                if (ok) { ui.Toast($"{e.label} · press G for the next one", false); return; }
            }
        }

        // Walking past a familiar face: Kiran waves as he goes (greet_01 is a walk with a wave).
        readonly Dictionary<string, float> waved = new Dictionary<string, float>(); Transform waveAt; float waveUntil;
        void WaveAtFriends()
        {
            if (player.InCar || InDialogue || !player.figure.HasClips || player.figure.Emoting || player.Speed < .6f || player.Speed > 2.6f) return;
            foreach (var a in citizens.agents)
            {
                if (!a || !a.named || a.talking) continue;
                float d = (a.transform.position - player.transform.position).magnitude; if (d > 6 || d < 1.5f) continue;
                var to = a.transform.position - player.transform.position; to.y = 0; if (Vector3.Angle(player.transform.forward, to) > 70) continue;
                if (waved.TryGetValue(a.Id, out var last) && Time.time - last < 120) continue;
                waved[a.Id] = Time.time; player.figure.Action("greet_01", .4f, 3.2f, 1, Act.WhileMoving); waveAt = a.transform; waveUntil = Time.time + 2.6f; break;
            }
        }

        (Interaction, string, string) FindInteraction()
        {
            var p = player.transform.position; nearActivity = null;
            if (activities.Current != null) return (Interaction.None, null, null);
            if (player.InCar) return (Interaction.ExitCar, "Get out of the car", null);
            var repair = missions.RepairNear(p); if (repair != null) return (Interaction.Repair, "Repair streetlight · hold", repair.target);
            var cafe = progress.Mission("cafe");
            if (cafe?.status == "active" && World.DistanceXZ(p, data.PoiPosition("cafe")) < 7) { var steps = missions.Steps("cafe"); return (Interaction.Work, cafe.step < steps.Length ? steps[cafe.step].text : "Tea shop counter", null); }
            var c = citizens.Nearest(p); if (c) return (Interaction.Talk, $"Talk to {c.displayName}", c.Id);
            nearActivity = activities.Near(p); if (nearActivity != null) return (Interaction.Activity, nearActivity.Prompt, nearActivity.id);
            if (World.DistanceXZ(p, car.transform.position) < 4) return (Interaction.EnterCar, "Drive your Maruti", null);
            var poi = data.pois.FirstOrDefault(x => World.DistanceXZ(p, x.x, x.z) < 3.8f);
            if (poi != null) return (Interaction.Place, $"{(poi.id == "warehouse" ? "Inspect" : "Visit")} {poi.name}", poi.id);
            return (Interaction.None, null, null);
        }

        public void Interact()
        {
            if (!Ready || Paused || InDialogue || player.Busy || heroes.Switching) return;
            var (kind, _, target) = interaction;
            switch (kind)
            {
                case Interaction.Talk: { var who = citizens.Get(target); player.figure.Emote("namaste"); ui.Wipe(() => dialogue.Open(who)); break; } // wipe into a telephoto two-shot
                case Interaction.EnterCar:
                    if (Mathf.Abs(car.speed) > 2) { Notify("Wait for the car to stop.", false); break; }
                    player.Teleport(player.transform.position);
                    car.driver = player; player.body.enabled = false; player.figure.gameObject.SetActive(false); player.car = car; rig.SetTarget(car.transform); rig.distance = 8;
                    Notify("Maruti · WASD to drive, Space handbrake, H horn, E to get out.", false); break;
                case Interaction.ExitCar:
                    if (Mathf.Abs(car.speed) > 2) { Notify("Slow down before getting out.", false); break; }
                    if (!car.TryExit(player, out var exit)) { Notify("Both doors are blocked. Move to a clear spot.", false); break; }
                    car.driver = null; player.car = null; player.Teleport(exit);
                    player.figure.gameObject.SetActive(true); rig.SetTarget(player.transform); rig.distance = 6;
                    guide.Parked(); break;
                case Interaction.Repair: hold = (target, 0, 2.2f); break;
                case Interaction.Activity: activities.Play(nearActivity); break;
                case Interaction.Work: ui.ShowWork = true; break;
                case Interaction.Place:
                    if (target == "plaza" || target == "park") audioDirector.Effect("bell");
                    missions.Event("interact", target); Notify(data.Poi(target).description, false); break;
            }
        }

        public void StartWork(string action)
        {
            var state = progress.Mission("cafe"); if (work.HasValue || state?.status != "active") return;
            var poi = data.Poi("cafe"); float hour = progress.minutes / 60;
            if (World.DistanceXZ(player.transform.position, data.PoiPosition("cafe")) > 7 || hour < poi.openFrom || hour >= poi.openTo) { Notify("Go to Himalayan Chiya Pasal while the tea shop is open.", false); return; }
            var steps = missions.Steps("cafe"); if (state.step < 0 || state.step >= steps.Length) return;
            var step = steps[state.step];
            if (step.target != action) { Notify($"Next, {step.text.Split('·').Last().Trim()}", false); return; }
            work = (action, 0, action == "brew" ? 2.6f : 1.4f);
        }

        public void Navigate(string id) { var p = data.Poi(id); if (p == null) return; navTarget = data.PoiPosition(id); navLabel = p.name; navId = id; Notify($"Waypoint set · {p.name}", false); }

        public bool TakeMicroBus(string id)
        {
            var destination = data.Poi(id); var p = player.transform.position;
            if (destination == null) { Notify("Select a named place first.", false); return false; }
            if (player.InCar || !data.pois.Any(x => World.DistanceXZ(p, x.x, x.z) < 16)) { Notify("Board the micro bus near a named stop, on foot.", false); return false; }
            if (progress.cash < 10) { Notify("A micro bus ride costs Rs 10.", false); return false; }
            progress.cash -= 10; var to = data.PoiPosition(id); if (id == "plaza") to = new Vector3(34, 0, 29);
            streamer.Prime(to); player.Teleport(to + Vector3.up * .1f);
            progress.minutes = (progress.minutes + 8) % 1440; dialogue.Close(); navTarget = null;
            missions.Update(0, to, false); Save(); audioDirector.Effect("bus_horn"); Notify($"Micro bus · {destination.name}   −Rs 10", false); return true;
        }

        void OnDialogue(Conversation c)
        {
            ui.DialogueChanged(c);
            if (c == null) { audioDirector.Stop(); return; }
            if (progress.sound) audioDirector.Speak(c.agent.voice ?? "resident-m", c.text);
        }

        public void Save()
        {
            if (!Ready || progress == null || smokeTest) return;
            progress.player = player.InCar ? car.transform.position + Vector3.left * 2 : player.transform.position; progress.car = car.transform.position;
            if (progress.heroPositions.Count > progress.hero) progress.heroPositions[progress.hero] = progress.player;
            progress.rain = dayNight.Rain; progress.wetness = dayNight.Wetness; progress.rainTarget = dayNight.rainTarget; progress.weatherSeconds = dayNight.rainTimer;
            progress.Save();
        }

        /// <summary>Clears the save and rebuilds the whole game from scratch.</summary>
        public void NewVisit()
        {
            Progress.Reset(); progress = null; Ready = false; Instance = null;
            Destroy(gameObject); new GameObject("Phewa Valley").AddComponent<Game>();
        }

        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationQuit() => Save();
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace PhewaValley
{
    [DefaultExecutionOrder(50)]
    public class HarborCombat : MonoBehaviour
    {
        public enum MissionStage { None, Inspect, Follow, Confront, Escape, Complete, Failed }
        enum Attack { None, Light, Heavy, Bow, Stealth }
        public MissionStage Stage { get; private set; }
        public readonly CombatVitals Vitals = new CombatVitals();
        public readonly List<HarborEnemy> Enemies = new List<HarborEnemy>();
        public bool BriefOpen { get; private set; }
        public bool Active => Stage != MissionStage.None;
        public bool OwnsView => Active || BriefOpen;
        public bool Guarding { get; private set; }
        public bool MovementLocked => BriefOpen || Stage == MissionStage.Complete || Stage == MissionStage.Failed || actionTime > 0 || hurtTime > 0;
        public Vector3 ReturnPosition { get; private set; }
        public bool Simulating => Active && Stage != MissionStage.Failed && Stage != MissionStage.Complete && !G.Paused;
        public Vector3 ObjectivePosition => Stage == MissionStage.Inspect ? origin + new Vector3(-6, 0, -5) :
            Stage == MissionStage.Follow && courier ? courier.transform.position :
            Stage == MissionStage.Escape ? origin + new Vector3(0, 0, -16) : origin + new Vector3(0, 0, 17);
        public const string Receipt = "mission:harbor-shadows";
        readonly Vector3 origin = new Vector3(-560, 0, 120);
        readonly Vector3[] route = { new Vector3(-6, 0, 1), new Vector3(-6, 0, 7), new Vector3(1, 0, 7), new Vector3(8, 0, 7), new Vector3(8, 0, 15), new Vector3(8, 0, 21) };
        readonly List<Mesh> ownedMeshes = new List<Mesh>();
        GameObject arena, sword, bow, clue;
        CharacterController courier;
        Figure courierFigure;
        Mesh arrowMesh;
        Quaternion returnRotation;
        int routeIndex;
        float actionTime, actionElapsed, impactAt, hurtTime, immuneUntil, feedbackTime;
        Attack pending;
        HarborEnemy stealthTarget;
        bool inspecting;
        string feedback = "";
        Game G => Game.Instance;
        Act ActionFlags => Act.PinXZ | Act.WhileMoving;

        void Update()
        {
            if (!G || !G.Ready || !G.Started) return;
            if (!Active)
            {
                if (Input.GetKeyDown(KeyCode.F5) && !G.Paused && !G.InDialogue && !G.ui.PhotoActive &&
                    !G.ui.ShowWork && !G.work.HasValue && G.activities.Current == null && !G.heroes.Switching &&
                    !(G.dhaneActions && G.dhaneActions.Open))
                {
                    if (G.heroes.Current.id != "dhane") G.Notify("Switch to Dhane with Tab, then 4, to play Harbor Shadows.", false);
                    else if (!G.player.InCar && !G.player.Busy) BriefOpen = !BriefOpen;
                }
                if (BriefOpen && Input.GetKeyDown(KeyCode.Escape)) BriefOpen = false;
                return;
            }
            if (G.Paused) return;
            if (Stage == MissionStage.Complete || Stage == MissionStage.Failed)
            {
                if (Input.GetKeyDown(KeyCode.Return)) { if (Stage == MissionStage.Failed) Restart(); else Leave(); }
                return;
            }
            float dt = Mathf.Min(Time.deltaTime, .1f);
            actionTime = Mathf.Max(0, actionTime - dt); hurtTime = Mathf.Max(0, hurtTime - dt);
            feedbackTime = Mathf.Max(0, feedbackTime - dt);
            if (pending != Attack.None)
            {
                actionElapsed += dt;
                if (actionElapsed >= impactAt) ResolveAttack();
            }
            if (inspecting && actionTime == 0)
            {
                inspecting = false; Stage = MissionStage.Follow; clue.SetActive(false);
                Say("A forged shipping seal. Follow the courier to the ledger.");
            }
            if (Stage == MissionStage.Follow) FollowCourier(dt);
            if (Stage == MissionStage.Confront && Enemies.TrueForAll(e => !e.Alive))
            {
                Stage = MissionStage.Escape; Say("Ledger recovered. Reach the south gate.");
            }
            SetGuard(Stage == MissionStage.Confront && Input.GetKey(KeyCode.Q) && actionTime == 0 && hurtTime == 0 && !G.player.Busy);
            Vitals.Tick(dt, Guarding);
            G.player.CombatSpeed = Guarding ? .45f : 1;
            if (bow) bow.SetActive(pending == Attack.Bow || actionTime > 0 && !sword.activeSelf);
            if (actionTime == 0 && sword) { sword.SetActive(true); bow.SetActive(false); }
            if (Input.GetKeyDown(KeyCode.E)) InteractObjective();
            if (Stage == MissionStage.Confront && !G.ui.BlocksCamera)
            {
                if (Input.GetMouseButtonDown(0)) TryAttack(false);
                if (Input.GetMouseButtonDown(1)) TryAttack(true);
                if (Input.GetKeyDown(KeyCode.F)) TryBow();
                if (Input.GetKeyDown(KeyCode.LeftAlt)) TryDodge();
                if (Input.GetKeyDown(KeyCode.T)) TryStealth();
            }
            if (G.player.transform.position.y < origin.y - 4 || Vector3.Distance(G.player.transform.position, origin) > 55)
                Fail("You left the courtyard. Retry the mission or return to the city.");
        }

        public bool BeginMission()
        {
            if (Active || !G.Ready || G.heroes.Current.id != "dhane" || G.player.InCar || G.player.Busy || G.InDialogue) return false;
            BriefOpen = false; ReturnPosition = G.player.transform.position; returnRotation = G.player.transform.rotation;
            BuildArena(); Restart(); return true;
        }

        public void Restart()
        {
            if (!arena) return;
            foreach (var enemy in Enemies) if (enemy) Destroy(enemy.gameObject);
            Enemies.Clear();
            SpawnEnemy("Lookout", new Vector3(-4, .06f, 16), 60, 0);
            SpawnEnemy("Enforcer", new Vector3(4, .06f, 19), 75, .8f);
            Vitals.Reset(); Guarding = false; pending = Attack.None; inspecting = false;
            actionTime = hurtTime = 0; immuneUntil = 0; Stage = MissionStage.Inspect;
            routeIndex = 0; courier.gameObject.SetActive(true); courier.enabled = false;
            courier.transform.position = origin + new Vector3(-6, .06f, -1); courier.enabled = true;
            courierFigure.ResetPose(); clue.SetActive(true);
            Physics.SyncTransforms();
            G.player.Teleport(origin + new Vector3(0, .06f, -14));
            G.player.transform.rotation = Quaternion.identity; G.player.CombatSpeed = 1;
            G.player.figure.Talking = false; G.player.figure.LookAt = null;
            G.rig.SetTarget(G.player.transform); G.rig.yaw = 0; G.rig.pitch = 14; G.rig.distance = 6;
            Say("Inspect the shipping seal on the crate. Approach it and press E.");
        }

        public void Leave()
        {
            if (!Active) { BriefOpen = false; return; }
            Stage = MissionStage.None; pending = Attack.None; actionTime = hurtTime = 0; Guarding = false;
            if (sword) Destroy(sword); if (bow) Destroy(bow);
            if (arena) Destroy(arena);
            Enemies.Clear(); DisposeMeshes();
            G.streamer.Prime(ReturnPosition); G.player.Teleport(ReturnPosition);
            G.player.transform.rotation = returnRotation; G.player.CombatSpeed = 1;
            G.rig.yaw = returnRotation.eulerAngles.y; G.Save();
        }

        public bool InteractObjective()
        {
            if (!Simulating || actionTime > 0 || Vector3.Distance(G.player.transform.position, ObjectivePosition) > 2.5f) return false;
            if (Stage == MissionStage.Inspect)
            {
                inspecting = true; actionTime = Mathf.Clamp(G.player.figure.ClipDuration("inspect_clue"), 1, 3);
                G.player.figure.PlayClip("inspect_clue", ActionFlags);
                G.audioDirector.Effect("ui_confirm", .4f); return true;
            }
            if (Stage == MissionStage.Escape)
            {
                Stage = MissionStage.Complete;
                bool paid = G.progress.Reward(Receipt, 150, 3);
                Say(paid ? "Ledger delivered. Mission complete. You earned a reward and reputation." : "Practice run complete. Your original reward is already recorded.");
                G.audioDirector.Effect("success"); G.Save(); return true;
            }
            return false;
        }

        void FollowCourier(float dt)
        {
            if (Vector3.Distance(G.player.transform.position, courier.transform.position) > 13) { courierFigure.Speed = 0; return; }
            var target = origin + route[routeIndex]; var direction = target - courier.transform.position; direction.y = 0;
            if (direction.magnitude < .35f)
            {
                if (routeIndex < route.Length - 1) routeIndex++;
                else if (Vector3.Distance(G.player.transform.position, courier.transform.position) < 6)
                {
                    Stage = MissionStage.Confront; courier.gameObject.SetActive(false);
                    Say("The guards have the ledger. Sneak behind them with C + T, or fight.");
                }
                return;
            }
            courier.transform.rotation = Quaternion.Slerp(courier.transform.rotation, Quaternion.LookRotation(direction), dt * 8);
            courier.Move((direction.normalized * 1.9f + Vector3.down * 2) * dt); courierFigure.Speed = 1.9f;
        }

        bool ReadyToAttack(float cost)
        {
            if (!Simulating || Stage != MissionStage.Confront || actionTime > 0 || hurtTime > 0 || Guarding || G.player.Busy) return false;
            if (Vitals.Spend(cost)) return true;
            Say("Recover stamina before attacking."); return false;
        }

        void StartAttack(Attack kind, string clip, float duration, float hitTime)
        {
            pending = kind; actionTime = duration; impactAt = hitTime; actionElapsed = 0;
            FaceTarget();
            float length = G.player.figure.ClipDuration(clip);
            G.player.figure.PlayClip(clip, ActionFlags, length > 0 ? length / duration : 1);
            G.audioDirector.ActionCue(kind == Attack.Bow ? "Archery" : "Blade", kind == Attack.Bow ? "bow_draw" : clip);
        }

        public bool TryAttack(bool heavy)
        {
            if (!ReadyToAttack(heavy ? 25 : 12)) return false;
            StartAttack(heavy ? Attack.Heavy : Attack.Light, heavy ? "heavy_cut" : "diagonal_cut", heavy ? .95f : .58f, heavy ? .38f : .2f);
            return true;
        }

        public bool TryBow()
        {
            if (!ReadyToAttack(18)) return false;
            sword.SetActive(false); bow.SetActive(true);
            StartAttack(Attack.Bow, "bow_draw", .95f, .45f); return true;
        }

        public bool TryDodge()
        {
            if (!Simulating || actionTime > 0 || hurtTime > 0 || G.player.Busy || Vitals.Stamina < 22) return false;
            var camera = G.rig.transform;
            var forward = Vector3.ProjectOnPlane(camera.forward, Vector3.up).normalized;
            var direction = forward * Input.GetAxisRaw("Vertical") + camera.right * Input.GetAxisRaw("Horizontal"); direction.y = 0;
            if (direction.sqrMagnitude < .1f) direction = -G.player.transform.forward;
            if (!G.player.TryCombatDodge(direction.normalized)) return false;
            Vitals.Spend(22); immuneUntil = Time.time + .32f; Guarding = false;
            G.audioDirector.Effect("cloth_whoosh", .4f); return true;
        }

        public bool TryStealth()
        {
            if (!G.player.Crouching) { Say("Hold C and approach an unaware guard from behind."); return false; }
            HarborEnemy target = null;
            foreach (var enemy in Enemies)
            {
                var difference = G.player.transform.position - enemy.transform.position; difference.y = 0;
                if (enemy.Alive && !enemy.Aware && difference.magnitude < 1.8f && Vector3.Dot(enemy.transform.forward, difference.normalized) < -.5f &&
                    ClearPath(G.player.transform.position + Vector3.up, enemy.transform.position + Vector3.up, G.player.transform, enemy.transform)) target = enemy;
            }
            if (!target || !ReadyToAttack(20)) return false;
            stealthTarget = target; StartAttack(Attack.Stealth, "stealth_grab", 1.1f, .5f); return true;
        }

        void FaceTarget()
        {
            HarborEnemy nearest = null; float best = 24;
            var forward = Vector3.ProjectOnPlane(G.rig.transform.forward, Vector3.up).normalized;
            foreach (var enemy in Enemies)
            {
                var direction = enemy.transform.position - G.player.transform.position; direction.y = 0;
                if (enemy.Alive && direction.magnitude < best && Vector3.Dot(forward, direction.normalized) > .2f)
                { nearest = enemy; best = direction.magnitude; }
            }
            if (nearest)
            {
                var direction = nearest.transform.position - G.player.transform.position; direction.y = 0;
                G.player.transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        void ResolveAttack()
        {
            var attack = pending; pending = Attack.None;
            if (attack == Attack.Bow)
            {
                G.player.figure.PlayClip("bow_release", ActionFlags, Mathf.Max(1, G.player.figure.ClipDuration("bow_release") / .4f));
                var from = G.player.transform.position + Vector3.up * 1.25f + G.player.transform.forward * .65f;
                var direction = G.player.transform.forward;
                HarborEnemy target = null; float nearest = 24;
                foreach (var enemy in Enemies)
                {
                    var delta = enemy.transform.position + Vector3.up * 1.1f - from;
                    if (enemy.Alive && delta.magnitude < nearest && Vector3.Dot(direction, delta.normalized) > .92f &&
                        ClearPath(from, enemy.transform.position + Vector3.up * 1.1f, G.player.transform, enemy.transform))
                    { target = enemy; nearest = delta.magnitude; }
                }
                if (target) direction = (target.transform.position + Vector3.up * 1.1f - from).normalized;
                var arrow = new GameObject("Arrow"); arrow.transform.SetParent(arena.transform, false);
                arrow.transform.SetPositionAndRotation(from, Quaternion.LookRotation(direction));
                arrow.AddComponent<MeshFilter>().sharedMesh = arrowMesh; arrow.AddComponent<MeshRenderer>().sharedMaterial = Materials.VertexLit;
                arrow.AddComponent<HarborArrow>().Init(this, direction);
                G.audioDirector.Effect("bow_release", .55f); return;
            }
            if (attack == Attack.Stealth)
            {
                if (stealthTarget && stealthTarget.Alive && Vector3.Distance(G.player.transform.position, stealthTarget.transform.position) < 2.2f)
                { stealthTarget.TakeDamage(200); Say("Silent takedown."); }
                return;
            }
            float range = attack == Attack.Heavy ? 2.6f : 2.2f;
            foreach (var enemy in Enemies)
            {
                var delta = enemy.transform.position - G.player.transform.position; delta.y = 0;
                if (enemy.Alive && delta.magnitude <= range && Vector3.Dot(G.player.transform.forward, delta.normalized) >= .35f &&
                    ClearPath(G.player.transform.position + Vector3.up * 1.2f, enemy.transform.position + Vector3.up * 1.1f, G.player.transform, enemy.transform))
                    enemy.TakeDamage(attack == Attack.Heavy ? 35 : 20);
            }
        }

        public void SetGuard(bool held)
        {
            Guarding = held && Simulating && actionTime == 0 && hurtTime == 0 && !G.player.Busy;
            if (!Guarding) return;
            FaceTarget();
            if (!G.player.figure.Emoting) G.player.figure.PlayClip("guard_hold", ActionFlags);
        }

        public float ReceiveHit(float damage, Vector3 from)
        {
            if (!Simulating || Time.time < immuneUntil) return 0;
            var direction = from - G.player.transform.position; direction.y = 0;
            bool frontal = Vector3.Dot(G.player.transform.forward, direction.normalized) > .25f;
            float taken = Vitals.Hit(damage, Guarding && frontal, false);
            if (taken == 0) { G.player.figure.PlayClip("guard_impact", ActionFlags); G.audioDirector.Effect("cloth_whoosh", .35f); Say("Blocked."); return 0; }
            pending = Attack.None; inspecting = false; actionTime = 0; Guarding = false;
            hurtTime = Vitals.GuardBroken ? .85f : .28f; immuneUntil = Time.time + .22f;
            G.player.figure.PlayClip("hit_light", ActionFlags);
            G.audioDirector.Effect("land_heavy", .35f); Say(Vitals.GuardBroken ? "Guard broken. Recover stamina." : "Hit. Guard or dodge the next attack.");
            if (Vitals.Health <= 0) Fail("Dhane was overwhelmed. Retry with guard, dodge, bow, or a stealth approach.");
            return taken;
        }

        public static bool ClearPath(Vector3 from, Vector3 to, Transform self, Transform target)
        {
            var direction = to - from;
            foreach (var hit in Physics.RaycastAll(from, direction.normalized, direction.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(self) && !hit.transform.IsChildOf(target)) return false;
            return true;
        }

        void Fail(string reason) { Stage = MissionStage.Failed; pending = Attack.None; Guarding = false; Say(reason); G.player.figure.ResetPose(); }
        void Say(string text) { feedback = text; feedbackTime = 5; }

        GameObject MeshObject(string name, MeshBuilder builder, Transform parent, Vector3 position)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent, false); obj.transform.localPosition = position;
            var mesh = builder.Build(name); ownedMeshes.Add(mesh);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = Materials.VertexLit; return obj;
        }

        GameObject Box(string name, Vector3 position, Vector3 size, Color color)
        {
            var g = new MeshBuilder(); g.Box(0, 0, 0, size.x / 2, size.y / 2, size.z / 2, color);
            var obj = MeshObject(name, g, arena.transform, position); obj.AddComponent<BoxCollider>().size = size; return obj;
        }

        GameObject Weapon(Figure figure, bool isBow)
        {
            var g = new MeshBuilder();
            if (isBow)
            {
                for (int i = 0; i < 13; i++)
                {
                    float y = (i - 6) * .075f; float z = .14f * (1 - y * y / .22f);
                    g.Box(0, y, z, .018f, .045f, .018f, new Color(.3f, .12f, .045f));
                }
                g.Box(0, 0, 0, .003f, .48f, .003f, new Color(.75f, .7f, .55f));
            }
            else
            {
                g.Box(0, .28f, 0, .03f, .3f, .012f, new Color(.7f, .76f, .8f), Surface.Metal);
                g.Box(0, -.09f, 0, .025f, .09f, .025f, new Color(.16f, .07f, .035f));
                g.Box(0, 0, 0, .09f, .018f, .025f, new Color(.45f, .35f, .13f), Surface.Metal);
            }
            Transform hand = null;
            foreach (var bone in figure.GetComponentsInChildren<Transform>())
                if (bone.name.EndsWith(isBow ? "LeftHand" : "RightHand", System.StringComparison.OrdinalIgnoreCase)) { hand = bone; break; }
            var weapon = MeshObject(isBow ? "Bow" : "Blade", g, hand ? hand : figure.transform, Vector3.zero);
            var scale = weapon.transform.parent.lossyScale;
            weapon.transform.localScale = new Vector3(1 / Mathf.Max(.0001f, Mathf.Abs(scale.x)), 1 / Mathf.Max(.0001f, Mathf.Abs(scale.y)), 1 / Mathf.Max(.0001f, Mathf.Abs(scale.z)));
            if (!hand) weapon.transform.localPosition = new Vector3(.32f, 1.1f, .15f);
            return weapon;
        }

        void BuildArena()
        {
            arena = new GameObject("Harbor Shadows courtyard"); arena.transform.position = origin;
            var stone = new Color(.4f, .39f, .34f); var plaster = new Color(.53f, .48f, .37f);
            Box("Courtyard paving", new Vector3(0, -.25f, 2), new Vector3(36, .5f, 44), stone);
            Box("West warehouse", new Vector3(-19, 3, 2), new Vector3(2, 6, 44), plaster);
            Box("East warehouse", new Vector3(19, 3, 2), new Vector3(2, 6, 44), plaster);
            Box("North warehouse", new Vector3(0, 3, 25), new Vector3(40, 6, 2), plaster);
            Box("South wall left", new Vector3(-10, 1.7f, -20), new Vector3(16, 3.4f, 1), plaster);
            Box("South wall right", new Vector3(10, 1.7f, -20), new Vector3(16, 3.4f, 1), plaster);
            for (int i = 0; i < 7; i++)
            {
                Box("Warehouse window", new Vector3(-15 + i * 5, 3.5f, 23.9f), new Vector3(2, 1.8f, .1f), new Color(.035f, .07f, .08f));
                Box("Paving seam", new Vector3(0, .002f, -16 + i * 6), new Vector3(36, .004f, .035f), stone * .7f);
            }
            Box("Shipping crate", new Vector3(-6, .55f, -5), new Vector3(1.4f, 1.1f, 1.4f), new Color(.3f, .18f, .08f));
            clue = Box("Forged shipping seal", new Vector3(-6, 1.14f, -5), new Vector3(.65f, .035f, .4f), new Color(.85f, .75f, .45f));
            Box("Vaultable pallets", new Vector3(0, .5f, 2), new Vector3(4, 1, 2), new Color(.3f, .22f, .12f));
            Box("Storage crates", new Vector3(-11, .8f, 10), new Vector3(2.2f, 1.6f, 3), new Color(.3f, .22f, .12f));
            foreach (int side in new[] { -1, 1 })
            {
                var light = new GameObject("Courtyard light").AddComponent<Light>(); light.transform.SetParent(arena.transform, false);
                light.transform.localPosition = new Vector3(side * 12, 4, 5); light.type = LightType.Point; light.range = 23; light.intensity = 1.4f; light.color = new Color(1, .82f, .58f);
            }
            var actor = new GameObject("Courier"); actor.transform.SetParent(arena.transform, false); actor.transform.localPosition = new Vector3(-6, .06f, -1);
            courier = actor.AddComponent<CharacterController>(); courier.height = 1.75f; courier.radius = .3f; courier.center = Vector3.up * .875f;
            courierFigure = Figure.Create(actor.transform, "resident_m", Color.gray, new Color(.55f, .35f, .22f), Color.black, null, G.models);
            sword = Weapon(G.player.figure, false); bow = Weapon(G.player.figure, true); bow.SetActive(false);
            var arrow = new MeshBuilder(); arrow.Box(0, 0, 0, .012f, .012f, .3f, new Color(.45f, .25f, .09f));
            arrow.Box(0, 0, .32f, .035f, .015f, .05f, Color.gray, Surface.Metal);
            arrowMesh = arrow.Build("Arrow"); ownedMeshes.Add(arrowMesh);
        }

        void SpawnEnemy(string name, Vector3 at, float health, float delay)
        {
            var obj = new GameObject(name); obj.transform.SetParent(arena.transform, false); obj.transform.localPosition = at;
            var enemy = obj.AddComponent<HarborEnemy>(); enemy.Init(this, G.models, health, delay); Enemies.Add(enemy);
            Weapon(enemy.figure, false);
        }

        void DisposeMeshes() { foreach (var mesh in ownedMeshes) if (mesh) Destroy(mesh); ownedMeshes.Clear(); }
        void OnDestroy() { if (arena) Destroy(arena); if (sword) Destroy(sword); if (bow) Destroy(bow); DisposeMeshes(); }

        void OnGUI()
        {
            if (!G || !G.Ready || !G.Started || G.ui.PanelOpen) return;
            var previous = GUI.matrix; float scale = Screen.height / 900f;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale); float width = Screen.width / scale;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true, normal = { textColor = Color.white } };
            var button = new GUIStyle(GUI.skin.button) { fontSize = 20 };
            if (!Active && !BriefOpen)
            {
                if (G.heroes.Current.id == "dhane") Panel(new Rect(width / 2 - 180, 820, 360, 45), "F5  Harbor Shadows mission", style);
            }
            else if (BriefOpen)
            {
                var rect = new Rect(width / 2 - 310, 210, 620, 430); Backdrop(rect);
                GUILayout.BeginArea(new Rect(rect.x + 24, rect.y + 20, rect.width - 48, rect.height - 40));
                GUILayout.Label("HARBOR SHADOWS", new GUIStyle(style) { fontSize = 30, fontStyle = FontStyle.Bold });
                GUILayout.Label("A forged shipping seal leads Dhane to a guarded warehouse courtyard. Follow the courier, recover the ledger, and make it back to the gate.", style);
                GUILayout.Space(18); GUILayout.Label("WASD move • Mouse aim\nLeft / right click light / heavy attack\nQ guard • Left Alt dodge • F bow shot\nHold C + T behind an unaware guard\nE inspect / extract • Esc pause", style);
                GUILayout.Space(18);
                if (GUILayout.Button(G.progress.receipts.Contains(Receipt) ? "Replay mission" : "Start mission", button, GUILayout.Height(42))) BeginMission();
                if (GUILayout.Button("Keep exploring", button, GUILayout.Height(36))) BriefOpen = false;
                GUILayout.EndArea();
            }
            else
            {
                Backdrop(new Rect(20, 20, 610, 150));
                GUI.Label(new Rect(36, 28, 580, 30), "HARBOR SHADOWS", new GUIStyle(style) { fontSize = 24, fontStyle = FontStyle.Bold });
                string objective = Stage == MissionStage.Inspect ? "Inspect the seal on the marked crate. Press E nearby." :
                    Stage == MissionStage.Follow ? "Follow the courier. Stay within 13 metres." :
                    Stage == MissionStage.Confront ? "Recover the ledger. Defeat or silently disable both guards." :
                    Stage == MissionStage.Escape ? "Reach the south gate and press E to deliver the ledger." :
                    Stage == MissionStage.Complete ? "Mission complete. Press Enter to return to the city." : "Mission failed. Press Enter to retry.";
                GUI.Label(new Rect(36, 64, 575, 54), objective, style);
                GUI.Label(new Rect(36, 123, 570, 30), $"Health {Vitals.Health:0}/100     Stamina {Vitals.Stamina:0}/100", style);
                if (feedbackTime > 0) Panel(new Rect(width / 2 - 330, 740, 660, 65), feedback, style);
                Panel(new Rect(20, 830, width - 40, 50), "LMB light • RMB heavy • Q guard • Alt dodge • F bow • C + T stealth • E objective • Esc pause", style);
                if (Stage == MissionStage.Failed)
                {
                    if (GUI.Button(new Rect(width / 2 - 210, 380, 200, 48), "Retry", button)) Restart();
                    if (GUI.Button(new Rect(width / 2 + 10, 380, 200, 48), "Return to city", button)) Leave();
                }
                if (Stage == MissionStage.Complete && GUI.Button(new Rect(width / 2 - 130, 380, 260, 48), "Return to city", button)) Leave();
                DrawWorldLabels(width, style);
            }
            GUI.matrix = previous;
        }

        void DrawWorldLabels(float width, GUIStyle style)
        {
            var camera = G.rig.GetComponent<Camera>();
            if (Stage == MissionStage.Inspect || Stage == MissionStage.Follow || Stage == MissionStage.Escape)
            {
                var screen = camera.WorldToScreenPoint(ObjectivePosition + Vector3.up * 1.6f);
                if (screen.z > 0)
                {
                    float x = screen.x / Screen.height * 900; float y = 900 - screen.y / Screen.height * 900;
                    string label = Stage == MissionStage.Follow ? "Courier" : Stage == MissionStage.Escape ? "Exit gate" : "Shipping seal";
                    Panel(new Rect(Mathf.Clamp(x - 80, 20, width - 180), Mathf.Clamp(y, 180, 690), 160, 35), label, style);
                }
            }
            if (Stage != MissionStage.Confront) return;
            foreach (var enemy in Enemies)
            {
                if (!enemy.Alive) continue;
                var screen = camera.WorldToScreenPoint(enemy.transform.position + Vector3.up * 2.2f); if (screen.z <= 0) continue;
                float x = screen.x / Screen.height * 900, y = 900 - screen.y / Screen.height * 900;
                if (x < 0 || x > width || y < 170 || y > 760) continue;
                var tint = GUI.color; GUI.color = enemy.WindingUp ? new Color(1, .7f, .1f) : new Color(1, .35f, .28f);
                GUI.DrawTexture(new Rect(x - 60, y + 25, 120 * enemy.Health / enemy.MaxHealth, 5), Texture2D.whiteTexture); GUI.color = tint;
                GUI.Label(new Rect(x - 100, y - 4, 200, 32), enemy.WindingUp ? "Attack incoming" : enemy.Aware ? enemy.name : "Unaware guard", style);
            }
        }

        static void Backdrop(Rect rect)
        {
            var color = GUI.color; GUI.color = new Color(.025f, .035f, .05f, .94f); GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = color;
        }
        static void Panel(Rect rect, string text, GUIStyle style) { Backdrop(rect); GUI.Label(new Rect(rect.x + 10, rect.y + 5, rect.width - 20, rect.height - 10), text, style); }
    }
}

// People of the valley: named characters from world.json plus wandering residents.
// Each uses a Tripo character model when one is installed, otherwise a built-in figure.
using System.Collections.Generic;
using UnityEngine;

namespace PhewaValley
{
    /// <summary>Simple jointed figure (or a loaded model) that walks, idles and turns.</summary>
    [DefaultExecutionOrder(100)]
    public class Figure : MonoBehaviour
    {
        Transform armL, armR, legL, legR, body; ClipAnimator clips; ProceduralAnimator anim;
        /// <summary>Set while in conversation: drives gestures. LookAt turns the head toward a point.</summary>
        public bool Talking; public Vector3? LookAt; float phase;
        /// <summary>True when the model brought its own animation clips (emotes are available).</summary>
        public IReadOnlyList<string> ClipNames => clips?.ClipNames;
        public bool HasClip(string name) => clips != null && clips.Find(name) != null;
        public float ClipDuration(string name) => clips?.Duration(name) ?? 0;
        public bool PlayClip(string name, Act flags, float speed = 1) => clips != null && clips.PlayClip(name, flags, speed);
        public bool QueueClip(string name, Act flags, float speed = 1) => clips != null && clips.QueueClip(name, flags, speed);
        public bool Crouching;
        public bool HasClips => clips != null;
        /// <summary>Plays a one-off clip such as "Nepali namaste"; false if this model cannot.</summary>
        public bool Emote(string clip) => clips != null && clips.Emote(clip);
        /// <summary>Off the ground (jumping or falling); set by the player controller.</summary>
        public bool Airborne;
        /// <summary>Take-off: plays a hop timed to the expected time in the air.</summary>
        public void Jump(float airTime) => clips?.Jump(airTime);
        /// <summary>Plays part of a clip as an action (dive, climb, knock-down...). False if this model has no such clip.</summary>
        public bool Action(string clip, float from, float to, float speed, Act flags) => clips != null && clips.Action(clip, from, to, speed, flags);
        /// <summary>Queues a clip after the current action.</summary>
        public bool Then(string clip, float from, float to, float speed, Act flags) => clips != null && clips.Then(clip, from, to, speed, flags);
        /// <summary>Going up steps or a steep slope (switches to the stair run).</summary>
        public bool Climbing;
        public bool Busy => clips != null && clips.Busy;
        public bool Emoting => clips != null && clips.Emoting;
        public void ResetPose() { clips?.ResetPose(); Speed = 0; Airborne = Climbing = Crouching = false; }
        public float Speed { get; set; }
        /// <summary>Degrees the arms lift from the sides (used for waving, kite flying, playing).</summary>
        public float ArmOut { get; set; }

        public static Figure Create(Transform parent, string modelId, Color outfit, Color skin, Color hair, Color? topi, ModelLibrary models, float height = 1.7f)
        {
            var go = new GameObject("Figure"); go.transform.SetParent(parent, false); var f = go.AddComponent<Figure>();
            var model = models?.InstantiateCharacter(modelId, go.transform, height);
            if (model != null) { var a = model.GetComponentInChildren<Animation>(); if (a != null && a.GetClipCount() > 0) f.clips = new ClipAnimator(model.transform, a); var rig = model.GetComponent<RigBones>(); if (rig) f.anim = new ProceduralAnimator(rig); f.body = model.transform; return f; }
            var mat = Materials.VertexLit; var pants = new Color(.07f, .09f, .105f);
            Transform Part(string name, Vector3 pivot, System.Action<MeshBuilder> draw)
            {
                var p = new GameObject(name); p.transform.SetParent(go.transform, false); p.transform.localPosition = pivot;
                var mb = new MeshBuilder(); draw(mb); p.AddComponent<MeshFilter>().sharedMesh = mb.Build(name); p.AddComponent<MeshRenderer>().sharedMaterial = mat; return p.transform;
            }
            f.body = Part("Torso", Vector3.zero, g =>
            {
                g.Box(0, 1.2f, 0, .2f, .3f, .12f, outfit); g.Box(0, .88f, 0, .19f, .06f, .11f, pants);
                g.Sphere(0, 1.68f, 0, .13f, .15f, .13f, skin, new Surface(.7f), 10); g.Box(0, 1.5f, 0, .05f, .05f, .05f, skin);
                g.Sphere(0, 1.76f, -.02f, .14f, .09f, .14f, hair, new Surface(.9f), 8);
                if (topi.HasValue) { g.Box(0, 1.86f, 0, .14f, .06f, .13f, topi.Value); g.Box(0, 1.91f, -.01f, .1f, .025f, .12f, topi.Value * 1.3f); }
            });
            f.armL = Part("ArmL", new Vector3(-.26f, 1.45f, 0), g => { g.Box(0, -.27f, 0, .06f, .27f, .07f, outfit); g.Sphere(0, -.58f, 0, .05f, .05f, .05f, skin); });
            f.armR = Part("ArmR", new Vector3(.26f, 1.45f, 0), g => { g.Box(0, -.27f, 0, .06f, .27f, .07f, outfit); g.Sphere(0, -.58f, 0, .05f, .05f, .05f, skin); });
            f.legL = Part("LegL", new Vector3(-.1f, .85f, 0), g => { g.Box(0, -.4f, 0, .08f, .4f, .09f, pants); g.Box(0, -.82f, .04f, .08f, .04f, .13f, new Color(.05f, .04f, .03f)); });
            f.legR = Part("LegR", new Vector3(.1f, .85f, 0), g => { g.Box(0, -.4f, 0, .08f, .4f, .09f, pants); g.Box(0, -.82f, .04f, .08f, .04f, .13f, new Color(.05f, .04f, .03f)); });
            go.transform.localScale = Vector3.one * (height / 1.75f);
            return f;
        }

        void Update()
        {
            if (clips != null) { clips.Crouching = Crouching; clips.LookAt = LookAt; clips.Airborne = Airborne; clips.Climbing = Climbing; clips.Tick(Time.deltaTime, Speed, Talking); return; }
            phase += Time.deltaTime * (2 + Speed * 2.4f);
            float swing = Mathf.Sin(phase) * Mathf.Clamp01(Speed / 3) * 40;
            if (anim != null) { anim.Tick(transform, Time.deltaTime, Speed, ArmOut, Talking, LookAt); return; }
            if (legL) { legL.localRotation = Quaternion.Euler(swing, 0, 0); legR.localRotation = Quaternion.Euler(-swing, 0, 0); armL.localRotation = Quaternion.Euler(-swing * .8f, 0, 0); armR.localRotation = Quaternion.Euler(swing * .8f, 0, 0); }
            if (body && Speed < .2f) body.localPosition = new Vector3(0, Mathf.Sin(phase * .5f) * .006f, 0);
        }

        void LateUpdate() { clips?.LateTick(); }
    }

    public class Citizen : MonoBehaviour
    {
        public string Id, displayName, role, place, line, voice; public bool named, talking, fleeing; public Color outfit;
        public Figure figure; internal List<Vector3> path = new List<Vector3>(); internal float wait, panic, walkSpeed = 1.2f; internal Vector3 fleeFrom, home;
    }

    public class Citizens : MonoBehaviour
    {
        public readonly List<Citizen> agents = new List<Citizen>();
        Transform player; Func3 carSpeed; SeededRandom random = new SeededRandom(811);
        public delegate (Vector3 position, float speed) Func3();

        static readonly string[] Female = { "Sita", "Sarita", "Pema", "Anita", "Kabita", "Dolma", "Sunita", "Pratima" }, Male = { "Ram", "Hari", "Bishal", "Suman", "Prakash", "Dawa", "Rajesh", "Nabin" };
        static readonly string[] Last = { "Shrestha", "Gurung", "Tamang", "Magar", "Adhikari", "Rai", "Karki", "Bajracharya" };
        static readonly string[] Jobs = { "Office worker", "Shopkeeper", "Nurse", "Courier", "Boatman", "Student", "Trekking guide", "Tailor" };
        static readonly Color[] Skins = { new Color(.58f, .36f, .23f), new Color(.45f, .27f, .16f), new Color(.66f, .46f, .31f), new Color(.52f, .33f, .21f) };
        static readonly Color[] Outfits = { new Color(.12f, .22f, .3f), new Color(.35f, .19f, .12f), new Color(.26f, .29f, .14f), new Color(.25f, .17f, .3f), new Color(.55f, .08f, .12f), new Color(.57f, .44f, .25f) };
        static readonly Color[] Topis = { new Color(.42f, .1f, .09f), new Color(.04f, .04f, .045f), new Color(.22f, .14f, .3f) };

        public void Init(WorldData data, Transform player, Func3 car, ModelLibrary models)
        {
            this.player = player; carSpeed = car;
            foreach (var c in data.citizens)
            {
                var outfit = new Color(c.outfit[0], c.outfit[1], c.outfit[2]);
                Color? topi = c.topi != null && c.topi.Length == 3 ? new Color(c.topi[0], c.topi[1], c.topi[2]) : (Color?)null;
                var a = Make(c.id, c.name, c.role, c.place, c.line, new Vector3(c.x, World.TerrainHeight(c.x, c.z), c.z), outfit, topi, true, c.id, models);
                a.voice = c.id;
            }
            for (int i = 0; i < 48; i++)
            {
                bool female = i % 2 == 0; var first = female ? random.Pick(Female) : random.Pick(Male);
                Color? topi = !female && random.Next() < .55f ? random.Pick(Topis) : (Color?)null;
                var start = RandomNode();
                var a = Make($"resident-{i}", $"{first} {random.Pick(Last)}", random.Pick(Jobs), random.Pick(new[] { "cafe", "clinic", "depot", "market", "harbor", "park" }), null, start, random.Pick(Outfits), topi, false, female ? "resident_f" : "resident_m", models);
                a.voice = female ? "resident-f" : "resident-m"; a.walkSpeed = random.Range(1f, 1.45f); a.wait = random.Range(0, 4);
            }
        }

        Citizen Make(string id, string name, string role, string place, string line, Vector3 position, Color outfit, Color? topi, bool named, string modelId, ModelLibrary models)
        {
            var go = new GameObject(name) { layer = 2 }; go.transform.SetParent(transform, false); go.transform.position = position + Vector3.up * .02f; // layer 2: the camera ignores people
            go.transform.rotation = Quaternion.Euler(0, random.Range(0, 360), 0);
            var a = go.AddComponent<Citizen>(); a.home = go.transform.position; a.Id = id; a.displayName = name; a.role = role; a.place = place; a.line = line; a.named = named; a.outfit = outfit;
            var hair = random.Next() < .2f ? new Color(.18f, .16f, .13f) : new Color(.025f, .02f, .018f);
            a.figure = Figure.Create(go.transform, modelId, outfit, random.Pick(Skins), hair, topi, models, named && modelId != null ? 1.7f : random.Range(1.58f, 1.76f));
            var col = go.AddComponent<CapsuleCollider>(); col.center = new Vector3(0, .9f, 0); col.height = 1.8f; col.radius = .3f;
            agents.Add(a); return a;
        }

        Vector3 RandomNode()
        {
            int cx = Mathf.FloorToInt(random.Range(-3, 5)), cz = Mathf.FloorToInt(random.Range(-2, 4));
            float x = cx * 80 + (random.Next() < .5f ? 8.6f : 71.4f), z = cz * 80 + (random.Next() < .5f ? 8.6f : 71.4f);
            return new Vector3(Mathf.Min(x, 420), 0, z);
        }

        // Residents walk the sidewalk lattice: each hop moves along x or z to the next sidewalk line.
        Vector3 NextNode(Vector3 p)
        {
            float Snap(float v) { float b = Mathf.Floor(v / 80) * 80, l = v - b; return b + (l < 40 ? 8.6f : 71.4f); }
            float x = Snap(p.x), z = Snap(p.z); bool alongX = random.Next() < .5f; float dir = random.Next() < .5f ? -1 : 1;
            float Hop(float v) { float local = ((v % 80) + 80) % 80; return v + dir * (local < 40 ? (dir < 0 ? 17.2f : 62.8f) : (dir < 0 ? 62.8f : 17.2f)); }
            if (alongX) x = Mathf.Clamp(Hop(x), -231.4f, 391.4f); else z = Mathf.Clamp(Hop(z), -151.4f, 311.4f);
            return new Vector3(x, World.TerrainHeight(x, z), z);
        }

        public Citizen Get(string id) => agents.Find(a => a.Id == id);
        public Citizen Nearest(Vector3 p, float max = 2.6f)
        {
            Citizen best = null; float bestD = max;
            foreach (var a in agents) { if (!a.gameObject.activeSelf || a.fleeing) continue; float d = World.DistanceXZ(a.transform.position, p); if (d < bestD) { bestD = d; best = a; } }
            return best;
        }

        public void PanicAt(Vector3 point, float radius = 28) { foreach (var a in agents) if (World.DistanceXZ(a.transform.position, point) < radius) { a.panic = 6; a.fleeFrom = point; a.path.Clear(); } }

        void Update()
        {
            if (!player) return; float dt = Time.deltaTime; var car = carSpeed(); var pp = player.position;
            foreach (var a in agents)
            {
                var t = a.transform; float dist = World.DistanceXZ(t.position, pp);
                bool visible = dist < 170; if (a.gameObject.activeSelf != visible) a.gameObject.SetActive(visible); if (!visible) continue;
                float speed = 0;
                a.figure.Talking = a.talking; a.figure.LookAt = a.talking || dist < 5 ? pp + Vector3.up * 1.6f : (Vector3?)null;
                if (a.talking) { Face(t, pp - t.position, dt * 5); a.figure.Speed = 0; continue; }
                if (car.speed > 5 && World.DistanceXZ(t.position, car.position) < 7) { a.panic = Mathf.Max(a.panic, 2); a.fleeFrom = car.position; }
                a.panic = Mathf.Max(0, a.panic - dt); a.fleeing = a.panic > 0;
                if (a.fleeing)
                {
                    var away = t.position - a.fleeFrom; away.y = 0; away = away.sqrMagnitude < .01f ? Vector3.forward : away.normalized;
                    speed = 3.6f; Move(t, away, speed, dt);
                }
                else if (!a.named)
                {
                    if (a.wait > 0) a.wait -= dt;
                    else
                    {
                        if (a.path.Count == 0) a.path.Add(NextNode(t.position));
                        var target = a.path[0]; var d = target - t.position; d.y = 0;
                        if (d.magnitude < .4f) { a.path.RemoveAt(0); if (random.Next() < .3f) a.wait = random.Range(1.5f, 5); }
                        else { speed = a.walkSpeed; Move(t, d.normalized, speed, dt); }
                    }
                }
                else if (World.DistanceXZ(t.position, a.home) > .15f)
                {
                    var homeward = a.home - t.position; homeward.y = 0;
                    speed = a.walkSpeed;
                    Move(t, homeward.normalized, speed, Mathf.Min(dt, homeward.magnitude / speed));
                }
                else if (dist < 6) Face(t, pp - t.position, dt * 2);
                a.figure.Speed = speed;
            }
        }

        static void Move(Transform t, Vector3 dir, float speed, float dt)
        {
            var p = t.position + dir * speed * dt; p.x = Mathf.Min(p.x, 430); p.y = World.TerrainHeight(p.x, p.z) + .02f; t.position = p; Face(t, dir, dt * 8);
        }
        static void Face(Transform t, Vector3 dir, float k) { dir.y = 0; if (dir.sqrMagnitude < 1e-4f) return; t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(dir), Mathf.Clamp01(k)); }
    }
}

// Plays a rigged character's own animation clips, such as a Tripo "Animation" export with
// walk, run, standing_relax, look_around and gestures, plus a procedural layer (namaste, head look-at). Walk and run are blended by
// speed with foot-synced playback. The clips' forward root motion is cancelled, so the body
// stays on its character controller. Emotes play once and are interrupted by moving.
using System.Collections.Generic;
using UnityEngine;

namespace PhewaValley
{
    /// <summary>Root drift of each clip, measured when the model loads (serialized, so clones keep it).</summary>
    public class ClipMotion : MonoBehaviour
    {
        public Transform hips; public List<string> clips = new List<string>(); public List<Vector3> drift = new List<Vector3>();
        /// <summary>Foot and toe bones, and how high the lowest of them sits above the sole when standing.</summary>
        public List<Transform> feet = new List<Transform>(); public float footLift;
        /// <summary>Hips height (model-root space) in the relaxed standing pose.</summary>
        public float hipsRestY;
        /// <summary>Hips height gained over each clip (the stair run climbs), and the resting hips position (x, z).</summary>
        public List<float> rise = new List<float>(); public List<Vector3> offsets = new List<Vector3>(); public Vector2 hipsRestXZ;

        /// <summary>
        /// Measures the clips, then stands the model on its real soles: the idle pose is baked to a mesh
        /// (skinned bounds from the file are not reliable) and the holder is moved so the lowest vertex sits at y = 0.
        /// Returns the standing height through <paramref name="height"/>.
        /// </summary>
        public static ClipMotion Measure(Transform root, Animation anim, Transform holder, out float height)
        {
            height = 0;
            var m = Measure(root, anim); if (m == null) return null;
            if (Baked(root, out float minY, out float maxY))
            {
                holder.localPosition -= Vector3.up * minY; height = maxY - minY;
                float lowest = float.MaxValue; foreach (var f in m.feet) lowest = Mathf.Min(lowest, root.InverseTransformPoint(f.position).y);
                m.footLift = m.feet.Count > 0 ? Mathf.Max(0, lowest) : 0;
                var rest = root.InverseTransformPoint(m.hips.position); m.hipsRestY = rest.y; m.hipsRestXZ = new Vector2(rest.x, rest.z);
            }
            return m;
        }

        static bool Baked(Transform root, out float minY, out float maxY)
        {
            minY = float.MaxValue; maxY = float.MinValue; var mesh = new Mesh();
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.BakeMesh(mesh, false); var t = smr.transform;
                foreach (var v in mesh.vertices) { float y = root.InverseTransformPoint(t.TransformPoint(v)).y; if (y < minY) minY = y; if (y > maxY) maxY = y; }
            }
            Object.Destroy(mesh);
            return maxY > minY;
        }

        static ClipMotion Measure(Transform root, Animation anim)
        {
            Transform hips = null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) { var n = t.name.ToLowerInvariant(); if (n.EndsWith("hips") || n == "pelvis") { hips = t; break; } }
            if (hips == null) return null;
            var m = root.gameObject.AddComponent<ClipMotion>(); m.hips = hips;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) { var n = t.name.ToLowerInvariant(); if (n.EndsWith("foot") || n.EndsWith("toebase") || n.EndsWith("toe_end") || n.StartsWith("foot_") || n.StartsWith("ball_")) m.feet.Add(t); }
            AnimationState idle = null;
            foreach (AnimationState s in anim)
            {
                var c = s.clip; if (c == null) continue;
                c.SampleAnimation(anim.gameObject, 0); var a = root.InverseTransformPoint(hips.position);
                // Looping clips wrap time: sampling at exactly c.length would read frame 0 again.
                c.SampleAnimation(anim.gameObject, c.length - 1e-4f); var b = root.InverseTransformPoint(hips.position);
                m.offsets.Add(a);
                var d = b - a; m.rise.Add(d.y); d.y = 0; m.clips.Add(s.name); m.drift.Add(d);
                if (ClipAnimator.Key(s.name) == "standingrelax") idle = s;
            }
            // Leave the template in a natural standing pose rather than the last sampled frame.
            if (idle != null) idle.clip.SampleAnimation(anim.gameObject, 0);
            var restPosition = root.InverseTransformPoint(hips.position);
            for (int i = 0; i < m.offsets.Count; i++) { var offset = m.offsets[i] - restPosition; offset.y = 0; m.offsets[i] = offset; }
            anim.playAutomatically = false;
            return m;
        }
    }

    /// <summary>How an action clip is played.</summary>
    [System.Flags]
    public enum Act
    {
        None = 0,
        /// <summary>Keep the hips over the character: the controller moves the body (dive, climb, knock-down).</summary>
        PinXZ = 1,
        /// <summary>Remove the clip's own upward lift (jump, climb): the controller does the lifting.</summary>
        CancelLift = 2,
        /// <summary>Not cancelled by moving (jumps, walking waves, actions the controller is running).</summary>
        WhileMoving = 4,
        /// <summary>Ends as soon as the body lands.</summary>
        EndOnLand = 8,
        /// <summary>Do not plant the feet (the body goes to the ground or into the air).</summary>
        NoPlant = 16,
    }

    public class ClipAnimator
    {
        readonly Animation anim; readonly Transform root, hips; readonly ClipMotion motion;
        float groundOffset, lastSpeed, sampleDelta;
        readonly Dictionary<AnimationState, Act> actionFlags = new Dictionary<AnimationState, Act>();
        readonly List<Transform> poseBones = new List<Transform>();
        readonly List<Vector3> restPositions = new List<Vector3>(), restScales = new List<Vector3>();
        readonly List<Quaternion> restRotations = new List<Quaternion>();
        readonly CharacterController controller; readonly Vector3 baseLocal; static readonly RaycastHit[] hits = new RaycastHit[8];
        readonly AnimationState walk, run, idle, look, jump, upstairs, walkSeam, runSeam;
        readonly List<AnimationState> talkGestures = new List<AnimationState>();
        readonly Dictionary<AnimationState, Vector3> initialOffset = new Dictionary<AnimationState, Vector3>();
        readonly Dictionary<AnimationState, Vector3> drift = new Dictionary<AnimationState, Vector3>();
        readonly Dictionary<AnimationState, float> rise = new Dictionary<AnimationState, float>();
        readonly Dictionary<AnimationState, float> want = new Dictionary<AnimationState, float>();
        float phase, idleTimer = 14, talkTimer = 1.5f, walkSpeed = 1.3f, runSpeed = 4.2f, seamK, stairs; bool speedsKnown;
        // Bones for the procedural layer (namaste, head look-at).
        readonly Transform armL, foreL, handL, armR, foreR, handR, spine, neck, head, idxL, pinkL, midL, idxR, pinkR, midR;
        Vector3 lastLookTarget;
        float namaste = -1, lookWeight; const float NamasteLength = 3f;
        /// <summary>World point to turn the head toward (a conversation partner).</summary>
        public Vector3? LookAt;
        /// <summary>Set by the controller while the body is off the ground.</summary>
        public bool Airborne;
        /// <summary>Set by the controller while going up steps or a steep slope.</summary>
        public bool Climbing;

        class Play { public AnimationState s; public float from, to, speed; public Act flags; }
        Play current; readonly Queue<Play> queue = new Queue<Play>();

        // Clips that are too long to play in full as a gesture.
        static readonly Dictionary<string, float> MaxLength = new Dictionary<string, float> { ["clap"] = 4.5f, ["greet_03"] = 5f, ["flip"] = 3.6f, ["chop"] = 3.4f };

        public static string Key(string s) => s.ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");

        public ClipAnimator(Transform root, Animation anim)
        {
            this.root = root; this.anim = anim;
            motion = root.GetComponent<ClipMotion>(); hips = motion ? motion.hips : null; baseLocal = root.localPosition;
            anim.playAutomatically = false; anim.Stop(); anim.cullingType = AnimationCullingType.AlwaysAnimate;
            // Own the sampling clock. Unity's automatic legacy evaluation otherwise races controller
            // updates and reapplies procedural offsets to the previous pose when paused/offscreen.
            anim.enabled = false; controller = root.GetComponentInParent<CharacterController>();
            foreach (var bone in root.GetComponentsInChildren<Transform>(true)) if (bone != root)
            { poseBones.Add(bone); restPositions.Add(bone.localPosition); restRotations.Add(bone.localRotation); restScales.Add(bone.localScale); }
            walk = Find("walk"); run = Find("run"); look = Find("look_around"); jump = Find("jump"); upstairs = Find("run_upstairs");
            idle = Find("standing_relax") ?? Find("idle") ?? walk;
            foreach (var g in new[] { "agree", "greet_03" }) { var s = Find(g); if (s != null) talkGestures.Add(s); }
            // A second copy of each locomotion clip, held on its first frame, smooths the jump where a loop
            // restarts (Tripo's run ends 19.5 degrees away from where it starts in the right arm).
            if (walk != null) { anim.AddClip(walk.clip, "walk~seam"); walkSeam = anim["walk~seam"]; }
            if (run != null) { anim.AddClip(run.clip, "run~seam"); runSeam = anim["run~seam"]; }
            foreach (AnimationState s in anim)
            {
                s.enabled = false; s.weight = 0; s.wrapMode = WrapMode.Loop; s.speed = 0; want[s] = 0;
                string source = s == walkSeam ? walk.name : s == runSeam ? run.name : s.name;
                int i = motion ? motion.clips.IndexOf(source) : -1;
                initialOffset[s] = i >= 0 && i < motion.offsets.Count ? motion.offsets[i] : Vector3.zero;
                drift[s] = i >= 0 ? motion.drift[i] : Vector3.zero; rise[s] = i >= 0 && i < motion.rise.Count ? motion.rise[i] : 0;
            }
            if (idle != null) { idle.enabled = true; idle.weight = 1; }
            Transform Bone(string n) { foreach (var t in root.GetComponentsInChildren<Transform>(true)) { var s = t.name; int c = s.IndexOf(':'); if ((c >= 0 ? s.Substring(c + 1) : s).Equals(n, System.StringComparison.OrdinalIgnoreCase)) return t; } return null; }
            armL = Bone("LeftArm"); foreL = Bone("LeftForeArm"); handL = Bone("LeftHand"); armR = Bone("RightArm"); foreR = Bone("RightForeArm"); handR = Bone("RightHand");
            spine = Bone("Spine2") ?? Bone("Spine1"); neck = Bone("Neck"); head = Bone("Head");
            idxL = Bone("LeftHandIndex1"); pinkL = Bone("LeftHandPinky1"); midL = Bone("LeftHandMiddle1"); idxR = Bone("RightHandIndex1"); pinkR = Bone("RightHandPinky1"); midR = Bone("RightHandMiddle1");
        }

        public AnimationState Find(string name)
        {
            var k = Key(name);
            foreach (AnimationState s in anim) if (Key(s.name) == k) return s;
            return null;
        }

        /// <summary>Ground speed (m/s) a locomotion clip covers at normal playback, at this model's scale.</summary>
        float NaturalSpeed(AnimationState s, float fallback)
        {
            if (s == null || s.length <= 0) return fallback;
            float v = root.TransformVector(drift[s]).magnitude / s.length;
            return v > .2f ? v : fallback;
        }

        /// <summary>
        /// Plays a gesture once. "namaste" is procedural (palms pressed together at the chest with a small bow),
        /// because Tripo's clip named "Nepali namaste" is really a wave. Returns false if there is no such clip.
        /// </summary>
        public bool Emote(string name)
        {
            if (Key(name) == "namaste") { if (armL == null || armR == null) return false; Clear(); namaste = 0; return true; }
            var s = Find(name); if (s == null) return false;
            return Action(name, 0, MaxLength.TryGetValue(s.name, out var max) ? max : s.length, 1, Act.None);
        }

        /// <summary>Plays part of a clip (from..to seconds) at a speed, replacing whatever action was playing.</summary>
        public bool Action(string name, float from, float to, float speed, Act flags)
        {
            var s = Find(name); if (s == null) return false;
            if (!ValidRange(s, from, to, speed)) return false;
            Clear(); Start(new Play { s = s, from = Mathf.Max(0, from), to = Mathf.Min(to, s.length), speed = speed, flags = flags }); return true;
        }

        /// <summary>Queues a clip to play after the current action (for chains such as fall → get up → complain).</summary>
        public bool Then(string name, float from, float to, float speed, Act flags)
        {
            var s = Find(name); if (s == null) return false;
            if (!ValidRange(s, from, to, speed)) return false;
            var p = new Play { s = s, from = Mathf.Max(0, from), to = Mathf.Min(to, s.length), speed = speed, flags = flags };
            if (current == null) Start(p); else queue.Enqueue(p);
            return true;
        }

        static bool ValidRange(AnimationState s, float from, float to, float speed) =>
            !float.IsNaN(from) && !float.IsNaN(to) && !float.IsNaN(speed) && !float.IsInfinity(speed) &&
            from >= 0 && from < s.length && to > from && Mathf.Min(to, s.length) > from && speed > 0;

        void RestorePose()
        {
            root.localPosition = baseLocal;
            for (int i = 0; i < poseBones.Count; i++) if (poseBones[i])
            { poseBones[i].localPosition = restPositions[i]; poseBones[i].localRotation = restRotations[i]; poseBones[i].localScale = restScales[i]; }
        }

        void Clear() { queue.Clear(); current = null; namaste = -1; }
        public void ResetPose()
        {
            Clear(); actionFlags.Clear(); groundOffset = stairs = phase = lookWeight = 0; Airborne = Climbing = false;
            RestorePose();
            foreach (AnimationState s in anim) { s.weight = 0; s.enabled = false; }
            if (idle != null) { idle.enabled = true; idle.weight = 1; idle.time = 0; }
            anim.Sample();
        }
        void Start(Play p) { current = p; actionFlags[p.s] = p.flags; p.s.wrapMode = WrapMode.ClampForever; p.s.speed = 0; p.s.time = p.from; p.s.enabled = true; }

        /// <summary>Take-off: plays the biggest hop of the "jump" clip, stretched to the controller's time in the air.</summary>
        public void Jump(float airTime) => Action("jump", .45f, .95f, .5f / Mathf.Max(.2f, airTime), Act.CancelLift | Act.WhileMoving | Act.EndOnLand);

        public bool Emoting => current != null || namaste >= 0;
        /// <summary>True while an action that the controller is driving (or a jump) is playing.</summary>
        public bool Busy => current != null && (current.flags & Act.WhileMoving) != 0;
        bool Planting => current == null || (current.flags & (Act.NoPlant | Act.CancelLift)) == 0;

        public void Tick(float dt, float speed, bool talking)
        {
            dt = Mathf.Max(0, dt); sampleDelta = dt;
            // Advance each non-locomotion state exactly once, even during a cross-fade.
            foreach (AnimationState s in anim)
                if (s.enabled && s != walk && s != run && s != upstairs && s != walkSeam && s != runSeam && (current == null || s != current.s)) s.time += dt;
            if (!speedsKnown) { walkSpeed = NaturalSpeed(walk, 1.3f); runSpeed = Mathf.Max(walkSpeed + .5f, NaturalSpeed(run, 4.2f)); speedsKnown = true; }
            bool moving = speed > .2f; lastSpeed = speed;
            // Actions end at their out-point, on landing, or (gestures) when the player starts moving.
            if (current != null)
            {
                var c = current; c.s.time = Mathf.Min(c.to, c.s.time + dt * c.speed);
                bool done = c.s.time >= c.to - .001f;
                if ((c.flags & Act.EndOnLand) != 0 && !Airborne && c.s.time > c.from + .05f) done = true;
                if (moving && (c.flags & Act.WhileMoving) == 0) { done = true; queue.Clear(); }
                if (done) { current = null; if (queue.Count > 0) Start(queue.Dequeue()); }
            }
            if (namaste >= 0) { namaste += dt; if (moving || namaste > NamasteLength) namaste = -1; }

            foreach (AnimationState s in anim) want[s] = 0;
            seamK = 0;
            stairs = Mathf.MoveTowards(stairs, Climbing && moving && upstairs != null ? 1 : 0, dt * 4);
            if (current != null) want[current.s] = 1;
            else if (moving && walk != null)
            {
                // Blend walk -> run by speed, both locked to one phase so the feet stay in step;
                // on steps and steep slopes the stair-climbing run takes over.
                float blend = run == null ? 0 : Mathf.Clamp01((speed - walkSpeed) / (runSpeed - walkSpeed));
                float natural = Mathf.Lerp(walkSpeed, runSpeed, blend), duration = run == null ? walk.length : Mathf.Lerp(walk.length, run.length, blend);
                if (stairs > 0) duration = Mathf.Lerp(duration, upstairs.length * 1.6f, stairs);
                float rate = Mathf.Clamp(speed / natural, .55f, 1.9f);
                phase = (phase + dt * rate / Mathf.Max(.1f, duration)) % 1;
                seamK = Mathf.Clamp01((phase - .9f) / .1f); seamK = seamK * seamK * (3 - 2 * seamK);
                Hold(walk, phase); Hold(walkSeam, 0); want[walk] += (1 - blend) * (1 - stairs);
                if (run != null) { Hold(run, phase); Hold(runSeam, 0); want[run] += blend * (1 - stairs); }
                if (stairs > 0) { Hold(upstairs, phase); want[upstairs] += stairs; }
            }
            else
            {
                idleTimer -= dt; talkTimer -= dt;
                if (talking && namaste < 0 && talkTimer < 0 && talkGestures.Count > 0)
                {
                    // Conversation: stand relaxed and gesture now and then, instead of looping one gesture.
                    var g = talkGestures[Random.Range(0, talkGestures.Count)]; Start(new Play { s = g, from = 0, to = Mathf.Min(g.length, 3.6f), speed = 1 });
                    talkTimer = Random.Range(4.5f, 7.5f); want[g] = 1;
                }
                else if (!talking && namaste < 0 && look != null && idleTimer < 0) { Start(new Play { s = look, from = 0, to = look.length, speed = 1 }); idleTimer = Random.Range(18f, 34f); want[look] = 1; }
                else if (idle != null) { idle.wrapMode = WrapMode.Loop; want[idle] = 1; }
            }

            // Cross-fade every state toward its target weight, keeping the total at 1. Quick actions blend in faster.
            bool snappy = current != null && (current.flags & (Act.WhileMoving | Act.PinXZ)) != 0;
            float total = 0, fade = dt / (snappy ? .1f : .22f);
            foreach (AnimationState s in anim) { s.weight = Mathf.MoveTowards(s.weight, want[s], fade); total += s.weight; }
            foreach (AnimationState s in anim)
            {
                if (total > 0) s.weight /= total;
                bool on = s.weight > .001f;
                if (on && !s.enabled && s != walk && s != run && s != upstairs && (current == null || s != current.s) && s != walkSeam && s != runSeam) s.time = 0;
                s.enabled = on; if (!on) actionFlags.Remove(s);
            }
            // Split each locomotion weight with its seam copy near the end of the cycle.
            Split(walk, walkSeam); Split(run, runSeam);
        }

        static void Hold(AnimationState s, float normalized) { if (s == null) return; s.speed = 0; s.wrapMode = WrapMode.Loop; s.normalizedTime = normalized; }

        void Split(AnimationState main, AnimationState seam)
        {
            if (main == null || seam == null) return;
            float w = main.weight + seam.weight; main.weight = w * (1 - seamK); seam.weight = w * seamK;
            main.enabled = main.weight > .001f; seam.enabled = seam.weight > .001f;
        }

        /// <summary>After the animation has been applied: cancels root travel, plants the feet, then the procedural layer.</summary>
        public void LateTick()
        {
            RestorePose(); anim.Sample();
            if (hips == null) return;
            Vector3 offset = Vector3.zero; float pin = 0, lift = 0;
            foreach (AnimationState s in anim)
            {
                if (!s.enabled || s.weight <= 0 || s.length <= 0) continue;
                var flags = actionFlags.TryGetValue(s, out var appliedFlags) ? appliedFlags : Act.None;
                if ((flags & Act.CancelLift) != 0) lift += s.weight;
                if ((flags & Act.PinXZ) != 0) { pin += s.weight; continue; }
                float t = s.wrapMode == WrapMode.Loop ? Mathf.Repeat(s.time, s.length) / s.length : Mathf.Clamp01(s.time / s.length);
                offset += (initialOffset[s] + (drift[s] + Vector3.up * rise[s]) * t) * s.weight;   // forward travel, and the climb of the stair run
            }
            if (offset.sqrMagnitude > 0) hips.position -= root.TransformVector(offset);
            // Pinned actions: keep the hips above the character; the controller moves the body.
            if (pin > 0)
            {
                var local = root.InverseTransformPoint(hips.position);
                hips.position -= root.TransformVector(new Vector3(local.x - motion.hipsRestXZ.x, 0, local.z - motion.hipsRestXZ.y));
            }
            // Jumps and climbs lift the hips themselves; the controller already does the lifting, so keep only the tuck.
            if (lift > 0)
            {
                float up = root.InverseTransformPoint(hips.position).y - motion.hipsRestY;
                if (up > 0) hips.position -= root.TransformVector(Vector3.up * up * Mathf.Clamp01(lift));
            }
            PlantFeet();
            Namaste();
            HeadLook();
        }

        /// <summary>
        /// Keeps the soles on the real ground: finds the ground under the character (kerbs, slopes, steps)
        /// and eases the body up or down so the lowest foot touches it. Large gaps (jumping) are left alone.
        /// </summary>
        void PlantFeet()
        {
            if (motion == null || motion.feet.Count == 0) return;
            float target = 0;
            if (!Airborne && Planting)
            {
                float sole = float.MaxValue; foreach (var f in motion.feet) if (f) sole = Mathf.Min(sole, f.position.y);
                sole -= motion.footLift * root.lossyScale.y;
                var anchor = controller ? controller.transform.TransformPoint(controller.center - Vector3.up * controller.height * .5f) : root.position;
                int n = Physics.RaycastNonAlloc(anchor + Vector3.up * .25f, Vector3.down, hits, .65f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                float best = float.MaxValue, groundY = 0; bool found = false;
                for (int i = 0; i < n; i++)
                {
                    var h = hits[i]; if (root.IsChildOf(h.collider.transform) || h.normal.y < .65f) continue; // our own capsule
                    if (h.distance < best) { best = h.distance; groundY = h.point.y; found = true; }
                }
                if (found) { float wantOffset = groundY - sole; if (Mathf.Abs(wantOffset) < .4f) target = wantOffset; }
            }
            // Running has flight phases with both feet up: follow slowly there so the stride is not flattened.
            float rate = lastSpeed > 3.5f ? 4 : 16;
            groundOffset = Mathf.Lerp(groundOffset, target, 1 - Mathf.Exp(-rate * sampleDelta));
            float parentScale = root.parent ? root.parent.lossyScale.y : 1;
            root.localPosition = baseLocal + Vector3.up * (groundOffset / Mathf.Max(.001f, parentScale));
        }

        /// <summary>Namaste: a small bow of chest and head while both palms press together in front of the chest.</summary>
        void Namaste()
        {
            if (namaste < 0 || handL == null || handR == null || spine == null) return;
            float w = Mathf.SmoothStep(0, 1, Mathf.Clamp01(namaste / .45f)) * Mathf.SmoothStep(0, 1, Mathf.Clamp01((NamasteLength - namaste) / .5f));
            float bow = Mathf.SmoothStep(0, 1, Mathf.Clamp01((namaste - .7f) / .45f)) * Mathf.SmoothStep(0, 1, Mathf.Clamp01((2.3f - namaste) / .45f));
            Vector3 up = root.up, fwd = root.forward, side = root.right;
            float S = Mathf.Max(.2f, (head ? head.position.y - root.position.y : 1.55f) / 1.55f); // metres per "average adult"
            spine.rotation = Quaternion.AngleAxis(9 * bow * w, side) * spine.rotation;
            if (neck) neck.rotation = Quaternion.AngleAxis(6 * bow * w, side) * neck.rotation;
            var chest = Vector3.Lerp(spine.position, neck ? neck.position : spine.position + up * .25f * S, .25f) + fwd * .2f * S - up * .04f * S;
            Arm(armL, foreL, handL, chest - side * .028f * S - up * .07f * S, -1, w, S, midL, idxL, pinkL);
            Arm(armR, foreR, handR, chest + side * .028f * S - up * .07f * S, 1, w, S, midR, idxR, pinkR);
            if (head) head.rotation = Quaternion.AngleAxis(16 * bow * w, side) * head.rotation;
        }

        void Arm(Transform upper, Transform lower, Transform hand, Vector3 wrist, float sideSign, float w, float S, Transform mid, Transform index, Transform pinky)
        {
            if (upper == null || lower == null || hand == null) return;
            Vector3 up = root.up, fwd = root.forward, side = root.right * sideSign;
            var pole = upper.position + (-up * .45f + side * .3f - fwd * .1f) * S; // elbows down and a little out
            TwoBone(upper, lower, hand, wrist, pole, w);
            if (mid == null || index == null || pinky == null) return;
            // Fingers up and slightly forward; index-to-pinky pointing forward, so the palms face each other.
            var f = (mid.position - hand.position).normalized; var l = Vector3.ProjectOnPlane(pinky.position - index.position, f);
            var wantF = (up * .92f + fwd * .38f).normalized; var wantL = Vector3.ProjectOnPlane(fwd, wantF);
            if (l.sqrMagnitude < 1e-8f || wantL.sqrMagnitude < 1e-8f) return;
            var delta = Quaternion.LookRotation(wantF, wantL) * Quaternion.Inverse(Quaternion.LookRotation(f, l));
            hand.rotation = Quaternion.Slerp(hand.rotation, delta * hand.rotation, w);
        }

        /// <summary>Analytic two-bone IK: bends the elbow toward the pole so the wrist reaches the target.</summary>
        static void TwoBone(Transform upper, Transform lower, Transform end, Vector3 target, Vector3 pole, float w)
        {
            if (w <= 0) return;
            Vector3 a = upper.position, b = lower.position, c = end.position;
            float la = Vector3.Distance(a, b), lb = Vector3.Distance(b, c); if (la < 1e-4f || lb < 1e-4f) return;
            var toT = target - a; float d = Mathf.Clamp(toT.magnitude, Mathf.Abs(la - lb) + 1e-3f, la + lb - 1e-3f); var dir = toT.normalized;
            float ang = Mathf.Acos(Mathf.Clamp((la * la + d * d - lb * lb) / (2 * la * d), -1, 1)) * Mathf.Rad2Deg;
            var poleDir = Vector3.ProjectOnPlane(pole - a, dir); if (poleDir.sqrMagnitude < 1e-8f) poleDir = Vector3.ProjectOnPlane(Vector3.down, dir);
            var axis = Vector3.Cross(dir, poleDir).normalized;
            Vector3 e1 = a + Quaternion.AngleAxis(ang, axis) * dir * la, e2 = a + Quaternion.AngleAxis(-ang, axis) * dir * la;
            var elbow = (e1 - pole).sqrMagnitude < (e2 - pole).sqrMagnitude ? e1 : e2;
            upper.rotation = Quaternion.Slerp(upper.rotation, Quaternion.FromToRotation(b - a, elbow - a) * upper.rotation, w);
            b = lower.position; c = end.position;
            lower.rotation = Quaternion.Slerp(lower.rotation, Quaternion.FromToRotation(c - b, a + dir * d - b) * lower.rotation, w);
        }

        /// <summary>Turns neck and head toward LookAt (limited like a real neck), easing in and out.</summary>
        void HeadLook()
        {
            if (head == null) return;
            lookWeight = Mathf.MoveTowards(lookWeight, LookAt.HasValue && (current == null || (current.flags & Act.WhileMoving) != 0 && (current.flags & Act.PinXZ) == 0) ? 1 : 0, sampleDelta * 2.5f);
            if (LookAt.HasValue) lastLookTarget = LookAt.Value;
            if (lookWeight <= 0) return;
            var to = lastLookTarget - head.position; var local = root.InverseTransformDirection(to);
            float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -65, 65);
            float pitch = Mathf.Clamp(-Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -25, 25);
            var delta = Quaternion.AngleAxis(yaw * lookWeight, root.up) * Quaternion.AngleAxis(pitch * lookWeight, root.right);
            if (neck) neck.rotation = Quaternion.Slerp(Quaternion.identity, delta, .4f) * neck.rotation;
            head.rotation = Quaternion.Slerp(Quaternion.identity, delta, .6f) * head.rotation;
        }
    }

    /// <summary>
    /// Some Tripo textures paint skin a saturated orange. This shifts skin-coloured texels to a natural
    /// brown, leaving clothes, hair and shoes alone. Works on non-readable textures by reading them back from the GPU.
    /// </summary>
    public static class SkinTone
    {
        public static void Fix(GameObject model)
        {
            var done = new Dictionary<Texture, Texture2D>();
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat == null) continue;
                    foreach (var prop in new[] { "baseColorTexture", "_BaseMap", "_MainTex" })
                    {
                        if (!mat.HasProperty(prop)) continue; var tex = mat.GetTexture(prop); if (tex == null) continue;
                        if (!done.TryGetValue(tex, out var fixedTex)) done[tex] = fixedTex = Recolor(tex);
                        mat.SetTexture(prop, fixedTex);
                    }
                }
        }

        static Texture2D Recolor(Texture src)
        {
            int w = src.width, h = src.height;
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true, false) { name = src.name + " (skin)", wrapMode = src.wrapMode, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            try { Graphics.Blit(src, rt); RenderTexture.active = rt; t.ReadPixels(new Rect(0, 0, w, h), 0, 0); }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); }
            var px = t.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i]; Color.RGBToHSV(c, out float hue, out float s, out float v);
                // Orange skin: hue 0-32 degrees, strongly saturated, not dark.
                float m = (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.035f, .06f, Mathf.Abs(hue - .045f))))
                        * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.42f, .58f, s)) * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.3f, .45f, v));
                if (m <= 0) continue;
                var fixedColor = Color.HSVToRGB(Mathf.Lerp(hue, .058f, m), Mathf.Lerp(s, s * .66f, m), Mathf.Lerp(v, v * .7f, m));
                px[i] = new Color32((byte)(fixedColor.r * 255), (byte)(fixedColor.g * 255), (byte)(fixedColor.b * 255), c.a);
            }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }
    }
}

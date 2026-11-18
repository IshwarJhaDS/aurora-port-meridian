// Auto-rigging and procedural animation for unrigged T-pose characters (Tripo's default
// export). Builds a 15-bone humanoid skeleton (hips, spine, chest, neck, head, upper
// arms, forearms, thighs, shins) with smooth two-bone skin weights at every joint, then
// animates it procedurally: walk and run cycles with knee and elbow bend, hip bob and
// sway, counter-rotating shoulders, idle breathing and weight shifts, head look-at,
// talking gestures and leaning into turns. Rigged/animated Tripo exports keep their clips.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PhewaValley
{
    public class RigBones : MonoBehaviour
    {
        public Transform hips, spine, chest, neck, head, upperArmL, foreArmL, upperArmR, foreArmR, thighL, shinL, thighR, shinR;
        public float height, restArm = 74;
    }

    public static class AutoRig
    {
        public static RigBones Apply(Transform root)
        {
            var filters = root.GetComponentsInChildren<MeshFilter>(); if (filters.Length == 0) return null;
            var all = new List<Vector3>();
            foreach (var f in filters) { if (!f.sharedMesh || !f.sharedMesh.isReadable) return null; var m = root.worldToLocalMatrix * f.transform.localToWorldMatrix; foreach (var v in f.sharedMesh.vertices) all.Add(m.MultiplyPoint3x4(v)); }
            var b = new Bounds(all[0], Vector3.zero); foreach (var v in all) b.Encapsulate(v);
            float H = b.size.y, cx = b.center.x, cz = b.center.z, floor = b.min.y;
            if (b.size.x < H * .6f) return null; // arms not spread: not a T-pose
            var armYs = new List<float>(); foreach (var v in all) if (Mathf.Abs(v.x - cx) > H * .3f) armYs.Add(v.y);
            if (armYs.Count < 20) return null; armYs.Sort();
            // Landmarks from typical human proportions, anchored on the measured arm line.
            float armY = armYs[armYs.Count / 2], shoulderX = H * .14f, reach = b.size.x / 2, elbowX = shoulderX + (reach - shoulderX) * .48f;
            float hipY = floor + H * .5f, kneeY = floor + H * .27f, chestY = floor + H * .66f, neckY = Mathf.Max(armY + H * .035f, floor + H * .82f), headY = floor + H * .87f, legX = H * .055f;

            var rigRoot = new GameObject("PV_Rig").transform; rigRoot.SetParent(root, false);
            Transform Bone(string n, Transform parent, Vector3 at) { var t = new GameObject(n).transform; t.SetParent(parent, false); t.position = root.TransformPoint(at); return t; }
            var hips = Bone("PV_Hips", rigRoot, new Vector3(cx, hipY, cz));
            var spine = Bone("PV_Spine", hips, new Vector3(cx, hipY + H * .08f, cz));
            var chest = Bone("PV_Chest", spine, new Vector3(cx, chestY, cz));
            var neck = Bone("PV_Neck", chest, new Vector3(cx, neckY, cz));
            var head = Bone("PV_Head", neck, new Vector3(cx, headY, cz));
            var uaL = Bone("PV_UpperArmL", chest, new Vector3(cx - shoulderX, armY, cz)); var faL = Bone("PV_ForeArmL", uaL, new Vector3(cx - elbowX, armY, cz));
            var uaR = Bone("PV_UpperArmR", chest, new Vector3(cx + shoulderX, armY, cz)); var faR = Bone("PV_ForeArmR", uaR, new Vector3(cx + elbowX, armY, cz));
            var thL = Bone("PV_ThighL", hips, new Vector3(cx - legX, hipY, cz)); var shL = Bone("PV_ShinL", thL, new Vector3(cx - legX, kneeY, cz));
            var thR = Bone("PV_ThighR", hips, new Vector3(cx + legX, hipY, cz)); var shR = Bone("PV_ShinR", thR, new Vector3(cx + legX, kneeY, cz));
            var bones = new[] { hips, spine, chest, neck, head, uaL, faL, uaR, faR, thL, shL, thR, shR };
            const int Hips = 0, Spine = 1, Chest = 2, Neck = 3, Head = 4, UAL = 5, FAL = 6, UAR = 7, FAR = 8, THL = 9, SHL = 10, THR = 11, SHR = 12;

            float S(float a, float bb, float x) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, bb, x));
            foreach (var f in filters)
            {
                var src = f.sharedMesh; var mesh = Object.Instantiate(src); mesh.name = src.name + " (rigged)";
                var toRoot = root.worldToLocalMatrix * f.transform.localToWorldMatrix; var verts = src.vertices; var weights = new BoneWeight[verts.Length];
                float blend = H * .035f;
                for (int i = 0; i < verts.Length; i++)
                {
                    var p = toRoot.MultiplyPoint3x4(verts[i]); float dx = p.x - cx, ax = Mathf.Abs(dx); bool left = dx < 0;
                    int a = Hips, c = Hips; float w = 0; // weight of bone c, (1 - w) of bone a
                    bool armZone = p.y > hipY + H * .1f && ax > shoulderX - blend;
                    if (armZone)
                    {
                        int ua = left ? UAL : UAR, fa = left ? FAL : FAR;
                        if (ax < elbowX) { a = Chest; c = ua; w = S(shoulderX - blend, shoulderX + blend * 1.4f, ax); }
                        else { a = ua; c = fa; w = S(elbowX - blend, elbowX + blend, ax); }
                    }
                    else if (p.y < hipY + blend)
                    {
                        int th = left ? THL : THR, sh = left ? SHL : SHR;
                        if (p.y > kneeY + blend) { a = Hips; c = th; w = S(hipY + blend, hipY - blend * 1.5f, p.y); }
                        else { a = th; c = sh; w = S(kneeY + blend, kneeY - blend, p.y); }
                    }
                    else if (p.y < chestY) { a = Hips; c = Spine; w = S(hipY + blend, chestY - blend, p.y); }
                    else if (p.y < neckY) { a = Spine; c = Chest; w = S(chestY - blend, chestY + blend, p.y); }
                    else if (p.y < headY) { a = Chest; c = Neck; w = S(neckY - blend, neckY + blend, p.y); }
                    else { a = Neck; c = Head; w = S(headY - blend * .5f, headY + blend, p.y); }
                    var bw = new BoneWeight { boneIndex0 = a, weight0 = 1 - w, boneIndex1 = c, weight1 = w };
                    if (bw.weight1 > bw.weight0) { (bw.boneIndex0, bw.boneIndex1) = (bw.boneIndex1, bw.boneIndex0); (bw.weight0, bw.weight1) = (bw.weight1, bw.weight0); }
                    weights[i] = bw;
                }
                mesh.boneWeights = weights;
                var binds = new Matrix4x4[bones.Length]; for (int i = 0; i < bones.Length; i++) binds[i] = bones[i].worldToLocalMatrix * f.transform.localToWorldMatrix;
                mesh.bindposes = binds;
                var renderer = f.GetComponent<MeshRenderer>(); var materials = renderer ? renderer.sharedMaterials : new Material[0];
                var go = f.gameObject; Object.DestroyImmediate(renderer); Object.DestroyImmediate(f);
                var smr = go.AddComponent<SkinnedMeshRenderer>(); smr.sharedMesh = mesh; smr.sharedMaterials = materials; smr.bones = bones; smr.rootBone = hips;
                smr.localBounds = new Bounds(hips.InverseTransformPoint(root.TransformPoint(b.center)), b.size * 1.5f); smr.updateWhenOffscreen = false;
            }
            var rig = root.gameObject.AddComponent<RigBones>();
            rig.hips = hips; rig.spine = spine; rig.chest = chest; rig.neck = neck; rig.head = head; rig.upperArmL = uaL; rig.foreArmL = faL; rig.upperArmR = uaR; rig.foreArmR = faR;
            rig.thighL = thL; rig.shinL = shL; rig.thighR = thR; rig.shinR = shR; rig.height = H;
            return rig;
        }
    }

    /// <summary>
    /// Drives a real skeleton (Tripo's Mixamo-style rig) from the procedural driver skeleton:
    /// each frame the driver's model-space rotation is applied on top of the bone's own rest
    /// orientation, so Tripo's skin weights deform the mesh while our animator moves it.
    /// </summary>
    public class SkeletonDriver : MonoBehaviour
    {
        [System.Serializable] public struct Pair { public Transform driver, bone; public Quaternion offset; }
        public Pair[] pairs; public Transform driverHips, boneHips; public Vector3 hipsRest, boneHipsRest;
        void LateUpdate()
        {
            if (pairs == null) return;
            if (boneHips) boneHips.localPosition = boneHipsRest + boneHips.parent.InverseTransformVector(driverHips.parent.TransformVector(driverHips.localPosition - hipsRest));
            foreach (var p in pairs) if (p.bone) p.bone.rotation = p.driver.rotation * p.offset;
        }
    }

    public static class RigRetarget
    {
        /// <summary>Builds a driver skeleton matching a Mixamo-named rig. Returns the driver bones, or null if the names are not found.</summary>
        public static RigBones Apply(Transform root)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            // Accepts both Mixamo ("mixamorig:LeftArm") and Unreal mannequin ("upperarm_l") naming, as Tripo exports either.
            Transform Find(params string[] names)
            {
                foreach (var n in names) foreach (var t in all) { var tn = t.name; int c = tn.LastIndexOf(':'); if (c >= 0) tn = tn.Substring(c + 1); if (string.Equals(tn, n, System.StringComparison.OrdinalIgnoreCase)) return t; }
                return null;
            }
            var hips = Find("Hips", "pelvis"); var spine = Find("Spine1", "spine_02", "Spine", "spine_01"); var chest = Find("Spine2", "spine_04", "spine_03") ?? spine;
            var neck = Find("Neck", "neck_01"); var head = Find("Head", "head");
            var la = Find("LeftArm", "upperarm_l"); var lf = Find("LeftForeArm", "lowerarm_l"); var ra = Find("RightArm", "upperarm_r"); var rf = Find("RightForeArm", "lowerarm_r");
            var lu = Find("LeftUpLeg", "thigh_l"); var ll = Find("LeftLeg", "calf_l"); var ru = Find("RightUpLeg", "thigh_r"); var rl = Find("RightLeg", "calf_r");
            if (new[] { hips, spine, chest, neck, head, la, lf, ra, rf, lu, ll, ru, rl }.Any(t => t == null)) return null;
            // The animator's "L" side is the character's -x side; trust positions over names.
            if (root.InverseTransformPoint(la.position).x > root.InverseTransformPoint(ra.position).x) { (la, ra) = (ra, la); (lf, rf) = (rf, lf); }
            if (root.InverseTransformPoint(lu.position).x > root.InverseTransformPoint(ru.position).x) { (lu, ru) = (ru, lu); (ll, rl) = (rl, ll); }
            var r = root.GetComponentsInChildren<Renderer>(); var b = r[0].bounds; foreach (var x in r) b.Encapsulate(x.bounds);

            var driverRoot = new GameObject("PV_Driver").transform; driverRoot.SetParent(root, false);
            Transform D(string n, Transform parent, Transform match) { var t = new GameObject(n).transform; t.SetParent(parent, false); t.position = match.position; t.rotation = root.rotation; return t; }
            var dh = D("PV_Hips", driverRoot, hips); var ds = D("PV_Spine", dh, spine); var dc = D("PV_Chest", ds, chest); var dn = D("PV_Neck", dc, neck); var dhd = D("PV_Head", dn, head);
            var dla = D("PV_UpperArmL", dc, la); var dlf = D("PV_ForeArmL", dla, lf); var dra = D("PV_UpperArmR", dc, ra); var drf = D("PV_ForeArmR", dra, rf);
            var dlu = D("PV_ThighL", dh, lu); var dll = D("PV_ShinL", dlu, ll); var dru = D("PV_ThighR", dh, ru); var drl = D("PV_ShinR", dru, rl);
            var map = new (Transform d, Transform t)[] { (dh, hips), (ds, spine), (dc, chest), (dn, neck), (dhd, head), (dla, la), (dlf, lf), (dra, ra), (drf, rf), (dlu, lu), (dll, ll), (dru, ru), (drl, rl) };
            var driver = root.gameObject.AddComponent<SkeletonDriver>();
            driver.pairs = map.Where(m => m.d != ds || spine != chest).Select(m => new SkeletonDriver.Pair { driver = m.d, bone = m.t, offset = Quaternion.Inverse(m.d.rotation) * m.t.rotation }).ToArray();
            driver.driverHips = dh; driver.boneHips = hips; driver.hipsRest = dh.localPosition; driver.boneHipsRest = hips.localPosition;
            var rig = root.gameObject.AddComponent<RigBones>();
            rig.hips = dh; rig.spine = ds; rig.chest = dc; rig.neck = dn; rig.head = dhd; rig.upperArmL = dla; rig.foreArmL = dlf; rig.upperArmR = dra; rig.foreArmR = drf;
            rig.thighL = dlu; rig.shinL = dll; rig.thighR = dru; rig.shinR = drl; rig.height = b.size.y / Mathf.Max(.0001f, root.lossyScale.y);
            return rig;
        }
    }

    /// <summary>Procedural humanoid motion for an auto-rigged character.</summary>
    public class ProceduralAnimator
    {
        readonly RigBones r; readonly Vector3 hipsRest; float phase, gait, run, breath, lean, lastYaw, shift, gesture; readonly float seed;
        public ProceduralAnimator(RigBones rig) { r = rig; hipsRest = rig.hips.localPosition; seed = Random.Range(0f, 100f); }

        /// <param name="speed">Ground speed in m/s (walk ≈ 1.2–3, run ≈ 6).</param>
        /// <param name="armOut">Degrees the arms are raised (kite flying, balancing).</param>
        /// <param name="look">Optional world point for the head to turn toward.</param>
        public void Tick(Transform body, float dt, float speed, float armOut, bool talking, Vector3? look)
        {
            // Smoothly blend between idle, walk and run so transitions never pop.
            gait = Mathf.MoveTowards(gait, Mathf.Clamp01(speed / 1.4f), dt * 4);
            run = Mathf.MoveTowards(run, Mathf.Clamp01((speed - 3.2f) / 2.5f), dt * 3);
            float stride = Mathf.Lerp(1.25f, 2.1f, run) * r.height / 1.7f; // metres per full cycle
            phase += speed > .05f ? speed / stride * Mathf.PI * 2 * dt : 0; breath += dt * Mathf.Lerp(1.6f, 4.5f, run); shift += dt * .23f;
            float yaw = body.eulerAngles.y, turn = Mathf.DeltaAngle(lastYaw, yaw) / Mathf.Max(dt, 1e-4f); lastYaw = yaw;
            lean = Mathf.Lerp(lean, Mathf.Clamp(-turn * .04f * gait, -12, 12), dt * 5);
            float s = Mathf.Sin(phase), c = Mathf.Cos(phase), g = gait;

            // Legs: thigh swing, knee flex on the forward swing and a soft knee in stance.
            float thighAmp = Mathf.Lerp(26, 44, run) * g, kneeAmp = Mathf.Lerp(38, 85, run) * g;
            float idleKnee = (1 - g) * (3 + Mathf.Sin(shift) * 2);
            r.thighL.localRotation = Quaternion.Euler(-s * thighAmp - run * 8, 0, 0); r.thighR.localRotation = Quaternion.Euler(s * thighAmp - run * 8, 0, 0);
            r.shinL.localRotation = Quaternion.Euler(Mathf.Max(0, c) * kneeAmp + 4 * g + idleKnee, 0, 0); r.shinR.localRotation = Quaternion.Euler(Mathf.Max(0, -c) * kneeAmp + 4 * g + idleKnee * .6f, 0, 0);

            // Hips: bob twice per cycle, sway side to side, twist with the legs; idle weight shift.
            float bob = (Mathf.Abs(c) - .6f) * Mathf.Lerp(.035f, .07f, run) * g * r.height / 1.7f;
            float sway = Mathf.Sin(phase) * .025f * g + Mathf.Sin(shift) * .012f * (1 - g);
            r.hips.localPosition = hipsRest + new Vector3(sway, bob - run * .04f, 0);
            r.hips.localRotation = Quaternion.Euler(0, s * 7 * g, lean * .4f + Mathf.Sin(shift) * 2 * (1 - g));

            // Spine and chest: lean forward when running, counter-twist the shoulders, breathe.
            float breathe = Mathf.Sin(breath) * Mathf.Lerp(1.4f, 2.5f, run);
            r.spine.localRotation = Quaternion.Euler(run * 10 + g * 2, -s * 4 * g, lean * .5f);
            r.chest.localRotation = Quaternion.Euler(breathe * .6f + run * 4, -s * 7 * g, 0);

            // Arms: hang at the sides, swing opposite to the legs, elbows bend more when running.
            float down = r.restArm - armOut, armSwing = Mathf.Lerp(22, 48, run) * g;
            gesture = talking ? gesture + dt : 0; float talk = talking ? Mathf.Max(0, Mathf.Sin(gesture * 1.7f + seed)) * 35 : 0;
            r.upperArmL.localRotation = Quaternion.Euler(s * armSwing - talk * .4f, 0, down + Mathf.Sin(breath) * .8f);
            r.upperArmR.localRotation = Quaternion.Euler(-s * armSwing - talk * .2f, 0, -down - Mathf.Sin(breath) * .8f);
            float elbow = Mathf.Lerp(10, 80, run) * g + 8 + talk;
            r.foreArmL.localRotation = Quaternion.AngleAxis(elbow + Mathf.Max(0, s) * 15 * g, Vector3.up);
            r.foreArmR.localRotation = Quaternion.AngleAxis(-(elbow + Mathf.Max(0, -s) * 15 * g), Vector3.up);

            // Neck and head: stay level against the hip motion, glance around when idle, look at a target.
            // Signed angles throughout: going through Quaternion.eulerAngles turned -1 degree into 359, which
            // scaled by 0.4/0.6 bent the neck 140-215 degrees whenever the head dipped (the "botched" heads).
            float glance = (1 - g) * Mathf.Sin(shift * 1.7f + seed) * 18;
            float pitch = -run * 6 + Mathf.Sin(breath) * .5f, yawH = glance, roll = -lean * .3f;
            if (look.HasValue)
            {
                var local = body.InverseTransformPoint(look.Value);
                yawH = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -65, 65);
                pitch = Mathf.Clamp(-Mathf.Atan2(local.y - r.height * .9f, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -25, 25); roll = 0;
            }
            float k = 1 - Mathf.Exp(-dt * 6);
            r.neck.localRotation = Quaternion.Slerp(r.neck.localRotation, Quaternion.Euler(pitch * .4f, yawH * .4f, 0), k);
            r.head.localRotation = Quaternion.Slerp(r.head.localRotation, Quaternion.Euler(pitch * .6f, yawH * .6f, roll), k);
        }
    }
}

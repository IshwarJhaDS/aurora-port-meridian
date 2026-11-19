using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PhewaValley
{
    public static class CharacterAnimationChecks
    {
        static AnimationClip Clip(string name, float travel, float lift)
        {
            var clip = new AnimationClip { name = name, legacy = true };
            clip.SetCurve("Hips", typeof(Transform), "localPosition.x", AnimationCurve.Constant(0, 2, 0));
            clip.SetCurve("Hips", typeof(Transform), "localPosition.z", AnimationCurve.Linear(0, 0, 2, travel));
            clip.SetCurve("Hips", typeof(Transform), "localPosition.y", new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1 + lift), new Keyframe(2, 1)));
            return clip;
        }
        static Transform Child(Transform parent, string name, Vector3 local)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = local; return go.transform;
        }
        static float Sole(GameObject model)
        {
            var baked = new Mesh(); float low = float.MaxValue;
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.BakeMesh(baked, false);
                foreach (var vertex in baked.vertices) low = Mathf.Min(low, renderer.transform.TransformPoint(vertex).y);
            }
            UnityEngine.Object.Destroy(baked); return low;
        }
        public static IEnumerator Run(Transform parent, Action<bool, string> check)
        {
            var synthetic = new GameObject("Synthetic animation checks");
            var hips = Child(synthetic.transform, "Hips", Vector3.up);
            var spine = Child(hips, "Spine2", Vector3.up * .3f);
            var neck = Child(spine, "Neck", Vector3.up * .2f); var head = Child(neck, "Head", Vector3.up * .1f);
            var animation = synthetic.AddComponent<Animation>(); var motion = synthetic.AddComponent<ClipMotion>();
            motion.hips = hips; motion.hipsRestY = 1;
            var owned = new List<AnimationClip>();
            foreach (string name in new[] { "standing_relax", "walk", "run", "jump", "dive", "climb" })
            {
                float travel = name == "standing_relax" ? 0 : 2;
                var clip = Clip(name, travel, name == "jump" || name == "climb" ? 1 : 0);
                owned.Add(clip); animation.AddClip(clip, name); motion.clips.Add(name); motion.drift.Add(Vector3.forward * travel); motion.rise.Add(0);
            }
            var player = new ClipAnimator(synthetic.transform, animation) { Airborne = true };
            player.Jump(.7f); player.Tick(.4f, 0, false); player.LateTick();
            check(player.Busy, "character jump animation survives past its midpoint");
            check(!animation.enabled && hips.position.y > .9f, "character animation samples with its automatic clock disabled");
            player.Action("climb", 0, 2, 1, Act.PinXZ | Act.CancelLift | Act.WhileMoving);
            player.Tick(1, 0, false); player.LateTick();
            check(Mathf.Abs(hips.localPosition.z) < .001f && Mathf.Abs(hips.localPosition.y - 1) < .001f, "climb cancels both horizontal root travel and vertical lift");
            player.Action("dive", 0, .2f, 1, Act.PinXZ | Act.NoPlant);
            player.Tick(.1f, 0, false); player.LateTick(); player.Tick(.11f, 0, false); player.LateTick();
            check(Mathf.Abs(hips.localPosition.z) < .001f, "outgoing action stays pinned throughout its fade");
            check(!player.Action("jump", 3, 1, 1, Act.None) && !player.Action("jump", 0, 1, 0, Act.None), "invalid animation ranges and playback rates are rejected");
            player.ResetPose(); player.LookAt = new Vector3(3, 2, 4);
            for (int i = 0; i < 20; i++) { player.Tick(.02f, 0, false); player.LateTick(); }
            var before = hips.position; var rotation = head.rotation;
            for (int i = 0; i < 120; i++) { player.Tick(0, 0, false); player.LateTick(); }
            check(Vector3.Distance(before, hips.position) < .0001f && Quaternion.Angle(rotation, head.rotation) < .01f, "paused sampling cannot accumulate hip drift or head twisting");
            player.LookAt = null;
            for (int i = 0; i < 40; i++) { player.Tick(.02f, 0, false); player.LateTick(); }
            check(Quaternion.Angle(head.localRotation, Quaternion.identity) < .1f, "head look returns to the authored pose when its target clears");
            UnityEngine.Object.Destroy(synthetic); foreach (var clip in owned) UnityEngine.Object.Destroy(clip); yield return null;

            var library = new ModelLibrary(parent); var loading = library.LoadCharacter("player");
            while (!loading.IsCompleted) yield return null;
            check(!loading.IsFaulted && loading.Result, "actual Kiran model loads for character regression checks");
            if (loading.IsFaulted || !loading.Result) yield break;
            var actor = new GameObject("Real Kiran animation checks"); actor.transform.position = new Vector3(40, 0, 40);
            var model = library.InstantiateCharacter("player", actor.transform, 1.75f);
            var animator = new ClipAnimator(model.transform, model.GetComponentInChildren<Animation>());
            var actualMotion = model.GetComponent<ClipMotion>();
            check(actualMotion && actualMotion.feet.Count >= 2, "Kiran has measured hip motion and foot contacts");
            for (int i = 0; i < 45; i++) { animator.Tick(1 / 60f, 0, false); animator.LateTick(); }
            float sole = Sole(model);
            check(sole > -.09f && sole < .12f, $"Kiran idle sole is near the real floor ({sole:0.000} m)");
            float furthest = 0; bool finite = true;
            foreach (float speed in new[] { 1.2f, 1.8f, 5.8f })
            {
                for (int i = 0; i < 150; i++)
                {
                    animator.Tick(1 / 60f, speed, false); animator.LateTick();
                    var relative = model.transform.InverseTransformPoint(actualMotion.hips.position);
                    relative.y = 0; var rest = new Vector3(actualMotion.hipsRestXZ.x, 0, actualMotion.hipsRestXZ.y);
                    furthest = Mathf.Max(furthest, model.transform.TransformVector(relative - rest).magnitude);
                    finite &= !float.IsNaN(relative.x) && !float.IsNaN(relative.z);
                }
            }
            check(finite && furthest < .4f, $"Kiran walk/run root drift remains bounded ({furthest:0.000} m)");
            animator.Action("dive", .25f, 2.71f, 1.25f, Act.PinXZ | Act.NoPlant | Act.WhileMoving);
            animator.Tick(.3f, 0, false); animator.LateTick(); var frozen = actualMotion.hips.position;
            for (int i = 0; i < 120; i++) { animator.Tick(0, 0, false); animator.LateTick(); }
            check(Vector3.Distance(frozen, actualMotion.hips.position) < .0001f, "actual Kiran dive pose stays fixed while paused");
            animator.ResetPose(); animator.Emote("namaste");
            for (int i = 0; i < 70; i++) { animator.Tick(.02f, 0, true); animator.LateTick(); }
            var bones = model.GetComponentsInChildren<Transform>(); var rotations = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++) rotations[i] = bones[i].rotation;
            for (int i = 0; i < 60; i++) { animator.Tick(0, 0, true); animator.LateTick(); }
            float pausedAngle = 0; for (int i = 0; i < bones.Length; i++) pausedAngle = Mathf.Max(pausedAngle, Quaternion.Angle(rotations[i], bones[i].rotation));
            check(pausedAngle < .1f, $"Kiran namaste remains stable while paused ({pausedAngle:0.000} deg)");
            animator.ResetPose(); animator.Tick(0, 0, false); animator.LateTick();
            check(!animator.Emoting && Sole(model) > -.09f, "reset clears actions and restores Kiran to standing");
            UnityEngine.Object.Destroy(actor);

            loading = library.LoadCharacter("dhane");
            while (!loading.IsCompleted) yield return null;
            check(!loading.IsFaulted && loading.Result, "Dhane animated model loads");
            if (loading.IsFaulted || !loading.Result) yield break;
            actor = new GameObject("Dhane animation checks");
            actor.transform.position = new Vector3(45, 0, 40);
            model = library.InstantiateCharacter("dhane", actor.transform, 1.75f);
            animator = new ClipAnimator(model.transform, model.GetComponentInChildren<Animation>());
            actualMotion = model.GetComponent<ClipMotion>();
            check(animator.ClipNames.Count == 56, "all 56 Dhane source animations are exposed");
            foreach (var entry in DhaneAnimations.All)
            {
                bool plays = animator.PlayClip(entry.key, Act.PinXZ | Act.WhileMoving | Act.NoPlant);
                bool valid = plays;
                float duration = animator.Duration(entry.key);
                var bonesToCheck = model.GetComponentsInChildren<Transform>();
                for (int frame = 0; frame < 30 && plays; frame++)
                {
                    animator.Tick(duration / 31, 0, false); animator.LateTick();
                    foreach (var bone in bonesToCheck)
                    {
                        var position = bone.position;
                        valid &= !float.IsNaN(position.x) && !float.IsNaN(position.y) && !float.IsNaN(position.z) &&
                            !float.IsInfinity(position.sqrMagnitude);
                    }
                    var local = model.transform.InverseTransformPoint(actualMotion.hips.position);
                    valid &= Vector2.Distance(new Vector2(local.x, local.z), actualMotion.hipsRestXZ) < .01f;
                }
                check(valid, $"Dhane {entry.label} resolves, samples and stays over its capsule");
                animator.ResetPose();
            }
            animator.Airborne = true; animator.Jump(.7f);
            animator.Tick(.25f, 0, false); animator.LateTick();
            check(animator.Busy, "Dhane takeoff transitions into airborne animation");
            animator.Airborne = false;
            animator.PlayClip("land_soft", Act.PinXZ | Act.WhileMoving);
            animator.Tick(.1f, 0, false); animator.LateTick();
            check(animator.Emoting, "Dhane soft landing replaces the airborne sequence");
            animator.ResetPose(); animator.Tick(0, 0, false); animator.LateTick();
            sole = Sole(model);
            check(!animator.Emoting && sole > -.09f && sole < .12f, $"Dhane returns to a grounded idle ({sole:0.000} m)");
            UnityEngine.Object.Destroy(actor);
        }
    }
}

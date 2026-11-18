// Inspects every animation clip of a rigged .glb: renders a filmstrip per clip and reports problems.
//   PhewaValley.exe -phewa-clips <file.glb> <output folder>
// Report: root drift, loop seam (pose jump between last and first frame), feet sinking into or
// floating above the ground (baked mesh), bone flips/jitter (sudden rotation spikes), scale glitches.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

namespace PhewaValley
{
    public class ClipInspector : MonoBehaviour
    {
        public string input, output;
        const int Frames = 8, W = 240, H = 400;

        IEnumerator Start()
        {
            Directory.CreateDirectory(output);
            var cam = new GameObject("Camera").AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.78f, .82f, .86f); cam.fieldOfView = 30;
            var sun = new GameObject("Light").AddComponent<Light>(); sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(45, -30, 0); sun.intensity = 1.1f; sun.shadows = LightShadows.Soft;
            RenderSettings.ambientLight = new Color(.6f, .6f, .62f);
            var ground = new MeshBuilder(); ground.Box(0, -.02f, 0, 6, .02f, 6, new Color(.55f, .55f, .5f));
            for (int i = -6; i <= 6; i++) { ground.Box(i * .5f, .001f, 0, .006f, .001f, 6, new Color(.3f, .3f, .3f)); ground.Box(0, .001f, i * .5f, 6, .001f, .006f, new Color(.3f, .3f, .3f)); }
            var g = new GameObject("Ground"); g.AddComponent<MeshFilter>().sharedMesh = ground.Build("Ground"); g.AddComponent<MeshRenderer>().sharedMaterial = Materials.VertexLit;

            var root = new GameObject("model"); var holder = new GameObject("holder"); holder.transform.SetParent(root.transform, false);
            var task = Load(input, holder.transform); while (!task.IsCompleted) yield return null;
            if (!task.Result) { Debug.Log("[clips] FAILED to load"); Application.Quit(); yield break; }
            var anim = holder.GetComponentInChildren<Animation>(); if (anim == null) { Debug.Log("[clips] no animations"); Application.Quit(); yield break; }
            anim.playAutomatically = false; anim.Stop();
            var bones = holder.GetComponentsInChildren<Transform>(true).Where(t => t.name.Contains(":") || t.name.ToLowerInvariant().Contains("hips")).ToArray();
            var hips = bones.First(b => b.name.ToLowerInvariant().EndsWith("hips"));
            var feet = bones.Where(b => { var n = b.name.ToLowerInvariant(); return n.EndsWith("foot") || n.EndsWith("toebase") || n.EndsWith("toe_end"); }).ToArray();
            var smrs = holder.GetComponentsInChildren<SkinnedMeshRenderer>(true); var mesh = new Mesh();
            float MeshMinY() { float m = float.MaxValue; foreach (var s in smrs) { s.BakeMesh(mesh, true); var t = s.transform; foreach (var v in mesh.vertices) m = Mathf.Min(m, (t.position + t.rotation * v).y); } return m; }
            float FeetMinY() => feet.Min(f => f.position.y);

            // Calibrate on the relaxed standing pose: put the soles on y = 0.
            var states = new List<AnimationState>(); foreach (AnimationState s in anim) states.Add(s);
            var idle = states.FirstOrDefault(s => s.name == "standing_relax") ?? states[0];
            idle.clip.SampleAnimation(anim.gameObject, 0); holder.transform.position -= Vector3.up * MeshMinY();
            float footLift = FeetMinY(); float height = 0; foreach (var s in smrs) { s.BakeMesh(mesh, true); var t = s.transform; foreach (var v in mesh.vertices) height = Mathf.Max(height, (t.position + t.rotation * v).y); }

            var report = new StringBuilder(); report.AppendLine($"model height {height:0.000} (units)  foot bone lift {footLift:0.000}  bones {bones.Length}");
            var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt; var frame = new Texture2D(W, H, TextureFormat.RGB24, false);
            foreach (var s in states)
            {
                // Looping legacy clips wrap time, so the true last frame is sampled just before the end.
                var clip = s.clip; float len = clip.length - 1e-4f; var line = new StringBuilder($"{s.name,-16} len {len,6:0.00}s");
                // Rest-of-pose sampler.
                Quaternion[] Pose(float t) { clip.SampleAnimation(anim.gameObject, t); return bones.Select(b => b.localRotation).ToArray(); }
                clip.SampleAnimation(anim.gameObject, 0); var hip0 = hips.position; clip.SampleAnimation(anim.gameObject, len); var hip1 = hips.position;
                line.Append($"  drift ({hip1.x - hip0.x:+0.00;-0.00},{hip1.z - hip0.z:+0.00;-0.00})");
                // Loop seam: biggest bone rotation difference between the last and first frame.
                var a = Pose(0); var bb = Pose(len); float seam = 0; string seamBone = "";
                for (int i = 0; i < bones.Length; i++) { float d = Quaternion.Angle(a[i], bb[i]); if (d > seam) { seam = d; seamBone = bones[i].name; } }
                line.Append($"  seam {seam,5:0.0}° ({Short(seamBone)})");
                // Spikes: sample at 120 Hz; a bone turning > 25° in 1/120 s is a flip or pop.
                float dt = 1 / 120f, worstSpike = 0; string spikeBone = ""; float spikeAt = 0; var prev = Pose(0);
                float minSole = float.MaxValue, maxLowest = float.MinValue; int flightSamples = 0, samples = 0; float maxScaleErr = 0;
                for (float t = dt; t <= len + 1e-4f; t += dt)
                {
                    var cur = Pose(t);
                    for (int i = 0; i < bones.Length; i++)
                    {
                        if (Short(bones[i].name).Contains("Hand") && Short(bones[i].name).Length > 9) continue; // fingers are noisy and tiny
                        float d = Quaternion.Angle(prev[i], cur[i]); if (d > worstSpike) { worstSpike = d; spikeBone = bones[i].name; spikeAt = t; }
                    }
                    prev = cur; samples++;
                    float lowest = FeetMinY() - footLift; minSole = Mathf.Min(minSole, lowest); maxLowest = Mathf.Max(maxLowest, lowest); if (lowest > .05f * height) flightSamples++;
                    foreach (var bn in bones) maxScaleErr = Mathf.Max(maxScaleErr, (bn.localScale - Vector3.one).magnitude);
                }
                line.Append($"  spike {worstSpike,5:0.0}°/8ms ({Short(spikeBone)} @{spikeAt:0.00}s)");
                line.Append($"  feet min {minSole / height * 170,5:0.0}cm max {maxLowest / height * 170,5:0.0}cm air {100f * flightSamples / Math.Max(1, samples),3:0}%  scaleErr {maxScaleErr:0.000}");
                // Filmstrip, camera at 3/4 front, following the hips sideways.
                var strip = new Texture2D(W * Frames, H, TextureFormat.RGB24, false);
                for (int f = 0; f < Frames; f++)
                {
                    float t = len * f / (Frames - 1); clip.SampleAnimation(anim.gameObject, t);
                    yield return null; clip.SampleAnimation(anim.gameObject, t); // skinning updates once per frame, not on a manual Render
                    yield return new WaitForEndOfFrame();
                    var c = hips.position; c.y = height * .5f; var dir = Quaternion.Euler(8, 210, 0) * Vector3.forward;
                    cam.transform.position = c - dir * height * 2.6f; cam.transform.LookAt(c); cam.nearClipPlane = .01f; cam.farClipPlane = 50;
                    cam.Render(); RenderTexture.active = rt; frame.ReadPixels(new Rect(0, 0, W, H), 0, 0); frame.Apply(); RenderTexture.active = null;
                    strip.SetPixels(f * W, 0, W, H, frame.GetPixels());
                }
                strip.Apply(); File.WriteAllBytes(Path.Combine(output, s.name.Replace(' ', '_') + ".png"), strip.EncodeToPNG()); Destroy(strip);
                report.AppendLine(line.ToString()); Debug.Log("[clips] " + line); yield return null;
            }
            File.WriteAllText(Path.Combine(output, "report.txt"), report.ToString());
            Debug.Log("[clips] done"); Application.Quit();
        }

        static string Short(string n) { int i = n.IndexOf(':'); return i >= 0 ? n.Substring(i + 1) : n; }

        static async Task<bool> Load(string path, Transform parent)
        {
            var import = new GltfImport();
            return await import.Load(new Uri(path).AbsoluteUri, new ImportSettings { AnimationMethod = AnimationMethod.Legacy }) && await import.InstantiateMainSceneAsync(parent);
        }
    }
}

// Loads Tripo .glb models at runtime with glTFast. Drop files named after the ids in
// docs/TRIPO_PROMPTS.md into StreamingAssets/PhewaValley/models/ (e.g. pagoda.glb,
// mira.glb). Anything missing falls back to the procedural art.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

namespace PhewaValley
{
    public class ModelLibrary
    {
        readonly Dictionary<string, (GameObject template, float height)> templates = new Dictionary<string, (GameObject, float)>();
        readonly Transform hidden;
        public readonly List<string> errors = new List<string>();
        public IEnumerable<string> Loaded => templates.Keys;

        public ModelLibrary(Transform root)
        {
            hidden = new GameObject("Model Templates").transform; hidden.SetParent(root, false); hidden.gameObject.SetActive(false);
        }

        public static string ModelsFolder => Path.Combine(WorldData.Root, "models");

        /// <summary>Path of a model: a manual download in models/ wins over a script-generated one.</summary>
        public static string FileFor(string id, string generatedFile = "")
        {
            var manual = Path.Combine(ModelsFolder, id + ".glb"); if (File.Exists(manual)) return manual;
            if (!string.IsNullOrEmpty(generatedFile)) { var generated = Path.Combine(WorldData.Root, "generated", generatedFile); if (File.Exists(generated)) return generated; }
            return null;
        }

        public async Task LoadAll(WorldData data, Action<string> progress = null)
        {
            var jobs = new List<(string id, string path)>(); var characters = new HashSet<string>(); foreach (var c in data.characters) if (c.id != "car") characters.Add(c.id);
            foreach (var m in data.models) { var f = FileFor(m.id, m.file); if (f != null) jobs.Add((m.id, f)); }
            foreach (var c in data.characters) { var f = FileFor(c.id); if (f != null) jobs.Add((c.id, f)); }
            int done = 0;
            foreach (var (id, path) in jobs)
            {
                progress?.Invoke($"Loading {id} ({++done}/{jobs.Count})");
                try { await Load(id, path, characters.Contains(id)); }
                catch (Exception e) { errors.Add($"{id}: {e.Message}"); Debug.LogWarning($"[models] {id} skipped: {e.Message}"); }
            }
            if (jobs.Count > 0) Debug.Log($"[models] {templates.Count}/{jobs.Count} models loaded from {ModelsFolder}");
        }

        public async Task<bool> LoadCharacter(string id)
        {
            if (Has(id)) return true;
            var path = FileFor(id); if (path == null) return false;
            await Load(id, path, true); return Has(id);
        }

        async Task Load(string id, string path, bool character)
        {
            var import = new GltfImport();
            var settings = new ImportSettings { AnimationMethod = AnimationMethod.Legacy, GenerateMipMaps = true };
            if (!await import.Load(new Uri(path).AbsoluteUri, settings)) throw new Exception("glTFast could not read the file (is it a .glb export?)");
            var root = new GameObject(id); // measured while active; parked under the hidden root afterwards
            var holder = new GameObject("model"); holder.transform.SetParent(root.transform, false);
            if (!await import.InstantiateMainSceneAsync(holder.transform)) throw new Exception("the file has no scene to instantiate");
            // Stand the model on y = 0 and centre its footprint, measured in template space.
            var renderers = holder.GetComponentsInChildren<Renderer>(true); if (renderers.Length == 0) throw new Exception("the model has no meshes");
            var b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
            var offset = root.transform.InverseTransformPoint(new Vector3(b.center.x, b.min.y, b.center.z));
            holder.transform.localPosition -= offset;
            foreach (var r in renderers) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; }
            foreach (var anim in holder.GetComponentsInChildren<Animation>(true)) anim.playAutomatically = true;
            // Characters: models with their own clips (Tripo "Animation" exports) play those through the
            // ClipAnimator; other Tripo rigs are driven by the procedural animator; unrigged T-pose
            // meshes get an automatic skeleton.
            var clips = character ? holder.GetComponentInChildren<Animation>(true) : null;
            float height = b.size.y;
            if (clips != null && clips.GetClipCount() > 0)
            {
                var motion = ClipMotion.Measure(root.transform, clips, holder.transform, out float standing);
                if (standing > .01f) height = standing;
                if (id == "player" && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) SkinTone.Fix(root);
                foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                { renderer.updateWhenOffscreen = true; renderer.quality = SkinQuality.Bone4; }
                var names = new List<string>(); foreach (AnimationState s in clips) names.Add(s.name);
                Debug.Log($"[models] {id}: {names.Count} animation clips ({string.Join(", ", names)}){(motion ? "" : ", hips not found so root motion is kept")}");
            }
            else if (character)
            {
                if (holder.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
                { if (RigRetarget.Apply(root.transform) == null) Debug.Log($"[models] {id}: rig bones not recognised, shown in its rest pose"); else Debug.Log($"[models] {id}: Tripo rig driven by the procedural animator"); }
                else if (AutoRig.Apply(root.transform) == null) Debug.Log($"[models] {id}: not a T-pose, shown without a rig");
            }
            root.transform.SetParent(hidden, false);
            templates[id] = (root, Mathf.Max(.001f, height));
        }

        public bool Has(string id) => templates.ContainsKey(id);

        public GameObject Instantiate(string id, Transform parent, Vector3 position, float yawRadians, float height)
        {
            if (!templates.TryGetValue(id, out var t)) return null;
            var go = UnityEngine.Object.Instantiate(t.template, parent);
            go.name = id; go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yawRadians * Mathf.Rad2Deg, 0));
            go.transform.localScale = Vector3.one * (height / t.height); go.SetActive(true);
            return go;
        }

        public GameObject InstantiateCharacter(string id, Transform parent, float height)
        {
            if (!templates.ContainsKey(id)) return null;
            var go = Instantiate(id, parent, parent.position, 0, height); go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;
            return go;
        }

        /// <summary>Places every landmark model from world.json; returns the procedural pieces it replaces.</summary>
        public HashSet<string> PlaceLandmarks(WorldData data, Transform parent)
        {
            var replaced = new HashSet<string>();
            foreach (var m in data.models)
            {
                if (!Has(m.id)) continue;
                if (!string.IsNullOrEmpty(m.replaces)) replaced.Add(m.replaces);
                if (!string.IsNullOrEmpty(m.building)) replaced.Add("building:" + m.building);
                int i = 0;
                foreach (var p in m.placements)
                {
                    float y = m.water ? -1.2f : p.y > -900 ? p.y : World.TerrainHeight(p.x, p.z);
                    var go = Instantiate(m.id, parent, new Vector3(p.x, y, p.z), p.yaw, m.height); go.name = $"{m.id} {i++}";
                    if (!m.solid) continue;
                    // Collide with the real shape (platforms, plinths and steps you can climb), not a guessed box.
                    var filters = go.GetComponentsInChildren<MeshFilter>();
                    if (filters.Length > 0 && filters.All(f => f.sharedMesh && f.sharedMesh.isReadable))
                    {
                        foreach (var f in filters) f.gameObject.AddComponent<MeshCollider>().sharedMesh = f.sharedMesh;
                        continue;
                    }
                    var renderers = go.GetComponentsInChildren<Renderer>(); var b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
                    float half = Mathf.Max(b.size.x, b.size.z) * .36f;
                    var box = new GameObject("Collider"); box.transform.SetParent(go.transform, false); box.transform.position = new Vector3(p.x, y + m.height * .25f, p.z);
                    var c = box.AddComponent<BoxCollider>(); c.size = new Vector3(half * 2, m.height * .5f, half * 2) / go.transform.localScale.x;
                }
            }
            Debug.Log($"[models] landmark colliders: {parent.GetComponentsInChildren<MeshCollider>().Length} mesh, {parent.GetComponentsInChildren<BoxCollider>().Length} box");
            return replaced;
        }
    }
}

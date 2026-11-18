// Builds city chunks around the player (port of src/world/streaming.js, simplified):
// near chunks get full detail, far ones are rebuilt cheaper, distant ones unloaded.
using System.Collections.Generic;
using UnityEngine;

namespace PhewaValley
{
    public static class Materials
    {
        static Material vertexLit, water;
        public static Material VertexLit => vertexLit ??= new Material(FindShader("PhewaValley/VertexColorStandard")) { name = "Phewa Vertex Lit", enableInstancing = true };
        public static Material Water
        {
            get
            {
                if (water) return water;
                water = new Material(Shader.Find("PhewaValley/Lake") ?? FindShader("Standard")) { name = "Phewa Lake", color = new Color(.03f, .16f, .18f) };
                water.SetFloat("_Glossiness", .92f); water.SetFloat("_Metallic", .2f); return water;
            }
        }
        static Shader FindShader(string name)
        {
            var s = Shader.Find(name);
            if (!s) { Debug.LogError($"Shader '{name}' missing from the build. Use the Phewa Valley ▸ Build menu, which includes it."); s = Shader.Find("Diffuse"); }
            return s;
        }
    }

    public class ChunkStreamer : MonoBehaviour
    {
        class Chunk { public GameObject root; public bool detailed; public ChunkPlan plan; }
        readonly Dictionary<(int, int), Chunk> chunks = new Dictionary<(int, int), Chunk>();
        public readonly Dictionary<string, LampInfo> lamps = new Dictionary<string, LampInfo>();
        CityGenerator generator; Transform player; ModelLibrary models; WorldData data;
        public int radius = 3, detailRadius = 1, buildsPerFrame = 2;
        public int Resident => chunks.Count;

        public void Init(CityGenerator generator, Transform player, ModelLibrary models = null, WorldData data = null) { this.generator = generator; this.player = player; this.models = models; this.data = data; }

        /// <summary>Synchronously builds the chunks around a point (start-up and fast travel).</summary>
        public void Prime(Vector3 at) { for (int i = 0; i < 64 && Step(at, 64); i++) { } }

        void Update() { if (player) Step(player.position, buildsPerFrame); }

        bool Step(Vector3 at, int budget)
        {
            int pcx = Mathf.Clamp(Mathf.FloorToInt(at.x / 80), World.Min, World.Max), pcz = Mathf.Clamp(Mathf.FloorToInt(at.z / 80), World.Min, World.Max);
            var remove = new List<(int, int)>();
            foreach (var kv in chunks) if (Mathf.Max(Mathf.Abs(kv.Key.Item1 - pcx), Mathf.Abs(kv.Key.Item2 - pcz)) > radius + 1) remove.Add(kv.Key);
            foreach (var key in remove) Unload(key);
            // Nearest first.
            var wanted = new List<(int x, int z, bool detail, float d)>();
            for (int z = pcz - radius; z <= pcz + radius; z++) for (int x = pcx - radius; x <= pcx + radius; x++)
            {
                if (x < World.Min || x > World.Max || z < World.Min || z > World.Max || x * 80 > 440) continue;
                bool detail = Mathf.Max(Mathf.Abs(x - pcx), Mathf.Abs(z - pcz)) <= detailRadius;
                if (chunks.TryGetValue((x, z), out var c) && (c.detailed || !detail)) continue;
                wanted.Add((x, z, detail, (x - pcx) * (x - pcx) + (z - pcz) * (z - pcz)));
            }
            wanted.Sort((a, b) => a.d.CompareTo(b.d));
            for (int i = 0; i < Mathf.Min(budget, wanted.Count); i++) Load(wanted[i].x, wanted[i].z, wanted[i].detail);
            return wanted.Count > budget;
        }

        void Load(int cx, int cz, bool detailed)
        {
            Unload((cx, cz));
            var result = generator.Build(cx, cz, detailed);
            var root = new GameObject($"Chunk {cx}:{cz}"); root.transform.SetParent(transform, false); root.transform.position = new Vector3(cx * 80, 0, cz * 80);
            var city = new GameObject("City"); city.transform.SetParent(root.transform, false);
            city.AddComponent<MeshFilter>().sharedMesh = result.mesh; city.AddComponent<MeshRenderer>().sharedMaterial = Materials.VertexLit;
            var ground = new GameObject("Ground"); ground.transform.SetParent(root.transform, false);
            ground.AddComponent<MeshFilter>().sharedMesh = result.ground; ground.AddComponent<MeshRenderer>().sharedMaterial = Materials.VertexLit;
            ground.AddComponent<MeshCollider>().sharedMesh = result.ground;
            foreach (var c in result.colliders)
            {
                var col = new GameObject(c.id); col.transform.SetParent(root.transform, false); col.transform.localPosition = c.center;
                col.AddComponent<BoxCollider>().size = c.half * 2;
            }
            foreach (var sign in result.signs) SignText(root.transform, sign);
            foreach (var (b, y) in result.modelled) PlaceBuilding(root.transform, b, y);
            foreach (var lamp in result.plan.lamps) lamps[lamp.id] = lamp;
            chunks[(cx, cz)] = new Chunk { root = root, detailed = detailed, plan = result.plan };
        }

        /// <summary>Painted lettering for a shop sign. Yaw is the direction the sign faces.</summary>
        static void SignText(Transform parent, SignInfo sign) => DevaText.Paint(parent, sign.position, Quaternion.Euler(0, sign.yaw * Mathf.Rad2Deg + 180, 0), sign.text, sign.ink, sign.size);

        /// <summary>Fills a lot with a Tripo building, facing the nearest road and fitted to the lot.</summary>
        void PlaceBuilding(Transform parent, BuildingPlan b, float y)
        {
            var kind = b.suburb ? "village" : "town"; var ids = new List<string>();
            foreach (var m in data.models) if (m.building == kind && models.Has(m.id)) ids.Add(m.id);
            if (ids.Count == 0) return;
            var id = ids[(int)(World.Hash(Mathf.FloorToInt(parent.position.x + b.x), Mathf.FloorToInt(parent.position.z + b.z), 17) % ids.Count)];
            float height = b.suburb ? b.height + 2 : b.height + 1.5f, yaw = (b.x < 40 ? -90 : 90) * Mathf.Deg2Rad;
            var go = models.Instantiate(id, parent, parent.TransformPoint(new Vector3(b.x, y, b.z)), yaw, height);
            var rs = go.GetComponentsInChildren<Renderer>(); var bounds = rs[0].bounds; foreach (var r in rs) bounds.Encapsulate(r.bounds);
            float fit = Mathf.Min(1, b.hx * 2.1f / bounds.size.x, b.hz * 2.1f / bounds.size.z);
            go.transform.localScale *= fit;
        }

        void Unload((int, int) key)
        {
            if (!chunks.TryGetValue(key, out var c)) return;
            // Only procedural meshes belong to the chunk; model meshes are shared by every copy.
            foreach (var name in new[] { "City", "Ground" }) { var t = c.root.transform.Find(name); if (t) Destroy(t.GetComponent<MeshFilter>().sharedMesh); }
            foreach (var lamp in c.plan.lamps) lamps.Remove(lamp.id);
            Destroy(c.root); chunks.Remove(key);
        }
    }
}

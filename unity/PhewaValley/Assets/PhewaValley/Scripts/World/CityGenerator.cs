// Port of src/world/city.js + src/world/landmarks.js. Chunk-local coordinates:
// each chunk is 80 m square with roads on its edges, built at (cx*80, 0, cz*80).
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PhewaValley
{
    public struct ColliderBox { public string id; public Vector3 center, half; }
    public class PropInfo { public string id, type; public Vector3 position; public bool breakable; }
    public class LampInfo { public string id; public Vector3 position; }
    /// <summary>A hand-painted shop sign: text is rendered with a TextMesh by the streamer.</summary>
    public class SignInfo { public Vector3 position; public float yaw; public string text; public Color ink; public float size = 1; }

    public class BuildingPlan
    {
        public string id, special; public float x, z, hx, hz, height; public Color color, roof, awning;
        public bool suburb, industrial, flags; public int roofKind; public string sign;
    }

    public class ChunkPlan
    {
        public int cx, cz; public float ox, oz; public World.District district;
        public List<BuildingPlan> buildings = new List<BuildingPlan>();
        public List<PropInfo> props = new List<PropInfo>();
        public List<LampInfo> lamps = new List<LampInfo>();
        public bool publicSpace;
    }

    public class ChunkResult
    {
        public ChunkPlan plan; public Mesh mesh, ground;
        public List<ColliderBox> colliders = new List<ColliderBox>();
        public List<SignInfo> signs = new List<SignInfo>();
        /// <summary>Lots to be filled by a Tripo building model instead of procedural geometry.</summary>
        public List<(BuildingPlan plan, float y)> modelled = new List<(BuildingPlan, float)>();
    }

    public class CityGenerator
    {
        static readonly Color Paving = C(.46f, .4f, .34f), Asphalt = C(.075f, .075f, .078f), Wood = C(.11f, .055f, .028f), Tank = C(.03f, .03f, .035f);
        static readonly Color Glass = C(.055f, .135f, .17f);
        static readonly Color[] Facades = { C(.42f, .15f, .08f), C(.48f, .19f, .1f), C(.36f, .13f, .075f), C(.62f, .58f, .5f), C(.6f, .43f, .2f), C(.45f, .5f, .52f), C(.58f, .36f, .38f), C(.5f, .52f, .36f) };
        static readonly Color[] Tin = { C(.42f, .1f, .07f), C(.13f, .27f, .2f), C(.15f, .2f, .35f), C(.4f, .4f, .38f) };
        static readonly Color[] Awnings = { C(.6f, .06f, .05f), C(.06f, .22f, .55f), C(.75f, .52f, .05f), C(.08f, .4f, .2f) };
        static Color C(float r, float g, float b) => new Color(r, g, b, 1);
        // Hand-painted signboards of a 2000s Nepali bazaar.
        static readonly string[] ShopSigns = { "STD  ISD  FAX", "KIRANA PASAL\nकिराना पसल", "PHOTOCOPY & LAMINATION", "NEW FASHION TAILORS", "MOMO CENTER\nमम सेन्टर", "VCD / DVD / CASSETTE CENTER", "RECHARGE CARD AVAILABLE", "MOBILE REPAIRING CENTER", "HIMALAYAN BOOK DEPOT", "SAJHA KHAJA GHAR\nखाजा घर", "DRY CLEANERS", "SHANTI MEDICAL HALL\nऔषधि पसल", "COLOR LAB  ·  PHOTO STUDIO", "NAULO SWEETS & BAKERY", "COMPUTER INSTITUTE\nBASIC · TYPING · INTERNET" };
        static readonly Color[] SignBoards = { C(.85f, .7f, .1f), C(.75f, .1f, .08f), C(.1f, .3f, .65f), C(.12f, .45f, .2f), C(.9f, .88f, .82f) };

        readonly WorldData data; readonly HashSet<string> replaced;
        public CityGenerator(WorldData data, HashSet<string> replaced) { this.data = data; this.replaced = replaced ?? new HashSet<string>(); }

        public ChunkPlan Describe(int cx, int cz)
        {
            var random = new SeededRandom(World.Hash(cx, cz, 183)); float ox = cx * 80, oz = cz * 80;
            var plan = new ChunkPlan { cx = cx, cz = cz, ox = ox, oz = oz, district = World.DistrictAt(ox + 40, oz + 40) };
            plan.publicSpace = cx == 0 && cz == 0 || cx == -2 && cz == 2 || cx == 0 && cz == 4;
            foreach (var p in data.pois)
            {
                if (Mathf.FloorToInt(p.x / 80) != cx || Mathf.FloorToInt(p.z / 80) != cz) continue;
                if (p.kind == "community" || p.kind == "park" || p.kind == "vista") continue;
                plan.buildings.Add(new BuildingPlan
                {
                    id = $"{cx}:{cz}:poi:{p.id}", x = p.x - ox + 16, z = p.z - oz, hx = 12, hz = 11, height = p.id == "warehouse" ? 13 : p.id == "clinic" ? 12 : 7,
                    color = p.id == "clinic" ? C(.62f, .64f, .58f) : p.id == "cafe" ? C(.49f, .31f, .2f) : C(.36f, .4f, .39f), special = p.id,
                    industrial = p.id == "warehouse" || p.id == "harbor" || p.id == "depot", roof = Tin[0], awning = Awnings[0]
                });
            }
            if (!plan.publicSpace && ox < 400)
                foreach (var (x, z) in new[] { (25f, 25f), (55f, 25f), (25f, 55f), (55f, 55f) })
                {
                    if (plan.buildings.Any(b => Mathf.Abs(b.x - x) < b.hx + 11 && Mathf.Abs(b.z - z) < b.hz + 11)) continue;
                    if (plan.district.id == "rural" && (x != 25 || z != 25)) continue;
                    bool suburb = plan.district.id == "suburbs" || plan.district.id == "rural", industrial = plan.district.id == "industrial" || plan.district.id == "highway";
                    float height = suburb ? random.Range(4.5f, 7) : industrial ? random.Range(6, 10) : random.Range(10, 21) * (cx == 0 || cz == 0 ? 1.15f : 1);
                    plan.buildings.Add(new BuildingPlan
                    {
                        id = $"{cx}:{cz}:lot:{x}:{z}", x = x, z = z, hx = suburb ? 8.2f : random.Range(8.5f, 11), hz = suburb ? 8 : random.Range(8, 10.5f), height = height,
                        color = random.Pick(Facades), suburb = suburb, industrial = industrial, roof = random.Pick(Tin), awning = random.Pick(Awnings), flags = random.Next() < .65f,
                        roofKind = Mathf.FloorToInt(random.Next() * 4), sign = random.Pick(ShopSigns)
                    });
                }
            int li = 0;
            foreach (var (x, z) in new[] { (14f, 11f), (66f, 69f) })
            {
                if (ox + x <= 430) { var id = $"{cx}:{cz}:lamp:{li}"; var y = World.TerrainHeight(ox + x, oz + z); plan.lamps.Add(new LampInfo { id = id, position = new Vector3(ox + x, y, oz + z) }); }
                li++;
            }
            for (int i = 0; i < 2; i++)
            {
                float x = i == 1 ? 68 : 12, z = i == 1 ? 51 : 27;
                if (ox + x < 430) plan.props.Add(new PropInfo { id = $"{cx}:{cz}:bin:{i}", type = "bin", position = new Vector3(ox + x, World.TerrainHeight(ox + x, oz + z), oz + z), breakable = true });
            }
            return plan;
        }

        public ChunkResult Build(int cx, int cz, bool detailed = true)
        {
            var plan = Describe(cx, cz); var g = new MeshBuilder(); var ground = new MeshBuilder(); var result = new ChunkResult { plan = plan };
            var random = new SeededRandom(World.Hash(cx, cz, 771));
            var green = plan.district.id == "rural" ? C(.23f, .29f, .16f) : C(.25f, .29f, .23f);
            int steps = plan.district.id == "rural" ? 8 : 1;
            for (int zi = 0; zi < steps; zi++) for (int xi = 0; xi < steps; xi++)
            {
                float a = xi * 80f / steps, b = zi * 80f / steps, c = (xi + 1) * 80f / steps, d = (zi + 1) * 80f / steps;
                Vector3 P(float px, float pz) => new Vector3(px, World.TerrainHeight(plan.ox + px, plan.oz + pz) - .03f, pz);
                if (plan.ox + a < World.LakeEdge) ground.Facing(new Vector3((a + c) / 2, -50, (b + d) / 2), P(a, b), P(a, d), P(c, d), P(c, b), green, Surface.Matte);
            }
            Roads(g, plan, detailed);
            if (detailed) Poles(g, plan);
            if (detailed && !plan.publicSpace && plan.district.id != "rural" && World.Hash(cx, cz, 91) % 3 == 0 && plan.ox < 400)
            {
                // Yellow STD/ISD telephone booth on the corner.
                g.Box(12.2f, 1.15f, 14, .55f, 1.15f, .55f, C(.9f, .72f, .08f), new Surface(.5f)); g.Box(12.2f, 1.4f, 14 + .56f, .4f, .5f, .01f, C(.1f, .15f, .2f), new Surface(.2f, .4f));
                result.signs.Add(new SignInfo { position = new Vector3(12.2f, 2.45f, 14 + .58f), yaw = 0, text = "STD · ISD", ink = C(.6f, .05f, .05f), size = .55f });
            }
            if (cx == 1 && cz == 0)
            {
                // Hari Salon: a narrow concrete shop with a mirror, shelf and painted sign.
                var wall = C(.78f, .74f, .62f);
                g.Box(44, 1.8f, 59.2f, 2.2f, 1.8f, .1f, wall); g.Box(41.9f, 1.8f, 61.5f, .1f, 1.8f, 2.4f, wall); g.Box(46.1f, 1.8f, 61.5f, .1f, 1.8f, 2.4f, wall);
                g.Box(44, 3.65f, 61.5f, 2.4f, .08f, 2.6f, C(.4f, .4f, .38f), new Surface(.5f, .5f));
                g.Box(44, 1.7f, 59.33f, 1.4f, .7f, .02f, C(.75f, .82f, .85f), new Surface(.05f, .9f)); g.Box(44, 1.15f, 59.45f, 1.6f, .04f, .18f, C(.35f, .22f, .12f));
                g.Box(44, 4.15f, 64.0f, 2.3f, .42f, .05f, C(.1f, .3f, .65f));
                result.signs.Add(new SignInfo { position = new Vector3(44, 4.15f, 64.06f), yaw = 0, text = "HARI SALON  हरि सैलुन", ink = C(.98f, .95f, .85f), size = .7f });
                result.colliders.Add(Col("salon-back", 44, 1.8f, 59.2f, 2.2f, 1.8f, .15f));
                if (!replaced.Contains("salon_chair")) { g.Box(44, .55f, 61.4f, .35f, .08f, .35f, C(.6f, .08f, .08f)); g.Box(44, .9f, 61.75f, .35f, .35f, .06f, C(.6f, .08f, .08f)); g.Cylinder(44, .25f, 61.4f, .12f, .25f, C(.7f, .7f, .7f), Surface.Metal); }
                if (!replaced.Contains("cassette")) { g.Box(45.6f, 1.32f, 60.4f, .22f, .11f, .07f, C(.6f, .6f, .62f), Surface.Metal); g.Box(13.4f, .82f, 55, .22f, .11f, .07f, C(.6f, .6f, .62f), Surface.Metal); }
                g.Box(13.4f, .4f, 55, .3f, .4f, .3f, C(.35f, .22f, .12f));
                if (!replaced.Contains("cyber"))
                {
                    g.Box(60, 1.9f, 60.5f, 2.6f, 1.9f, .1f, C(.8f, .72f, .55f)); g.Box(57.4f, 1.9f, 62, .1f, 1.9f, 1.6f, C(.8f, .72f, .55f)); g.Box(62.6f, 1.9f, 62, .1f, 1.9f, 1.6f, C(.8f, .72f, .55f));
                    for (int i = 0; i < 3; i++) { g.Box(58.5f + i * 1.5f, .8f, 61.2f, .55f, .04f, .4f, C(.6f, .45f, .25f)); g.Box(58.5f + i * 1.5f, 1.1f, 61.1f, .22f, .2f, .2f, C(.85f, .82f, .72f)); }
                    g.Box(60, 4.2f, 63.7f, 2.6f, .45f, .05f, C(.85f, .7f, .1f));
                    result.signs.Add(new SignInfo { position = new Vector3(60, 4.2f, 63.76f), yaw = 0, text = "PHEWA CYBER  ·  INTERNET Rs 20", ink = C(.6f, .05f, .05f), size = .6f });
                }
            }
            pendingSigns = result.signs;
            foreach (var b in plan.buildings)
            {
                float y = World.TerrainHeight(plan.ox + b.x, plan.oz + b.z);
                if (b.special == null && !b.industrial && replaced.Contains(b.suburb ? "building:village" : "building:town")) result.modelled.Add((b, y));
                else Building(g, b, detailed, y);
                result.colliders.Add(new ColliderBox { id = b.id, center = new Vector3(b.x, y + b.height / 2, b.z), half = new Vector3(b.hx, b.height / 2, b.hz) });
            }
            if (plan.publicSpace)
            {
                g.Box(40, .04f, 40, 26, .04f, 26, C(.49f, .47f, .4f));
                foreach (var t in new[] { 20f, 40f, 60f }) { g.Box(t, .085f, 40, .04f, .01f, 26, C(.31f, .32f, .29f)); g.Box(40, .085f, t, 26, .01f, .04f, C(.31f, .32f, .29f)); }
                if (cx == 0 && cz == 0)
                {
                    if (!replaced.Contains("pagoda")) { Landmarks.Pagoda(g, 47, 0, 47); result.colliders.Add(Col("pagoda", 47, 2.5f, 47, 5.2f, 2.5f, 5.2f)); }
                    if (!replaced.Contains("chautari")) { Landmarks.Chautari(g, 55, 0, 64, 7); result.colliders.Add(Col("chautari-square", 55, .5f, 64, 3.4f, .5f, 3.4f)); }
                }
                if (cx == -2 && cz == 2 && !replaced.Contains("stupa")) { Landmarks.Stupa(g, 40, 0, 54); result.colliders.Add(Col("stupa", 40, 3, 54, 8.6f, 3, 8.6f)); }
                if (cx == 0 && cz == 4 && !replaced.Contains("chautari")) { float y = World.TerrainHeight(plan.ox + 54, plan.oz + 32); Landmarks.Chautari(g, 54, y, 32, 9); result.colliders.Add(Col("chautari-lookout", 54, y + .5f, 32, 3.4f, .5f, 3.4f)); }
                foreach (var (x, z) in new[] { (19f, 22f), (61f, 22f), (19f, 60f), (61f, 60f) })
                {
                    float y = World.TerrainHeight(plan.ox + x, plan.oz + z);
                    g.Box(x, y + .5f, z, 1.8f, .12f, .48f, C(.25f, .16f, .09f)); g.Box(x, y + .95f, z + .43f, 1.8f, .45f, .08f, C(.25f, .16f, .09f));
                }
            }
            int count = plan.publicSpace ? 8 : plan.district.id == "rural" ? 7 : 3;
            for (int i = 0; i < count; i++)
            {
                float x = plan.publicSpace ? (i % 2 == 1 ? 62 : 18) : random.Pick(new[] { 12.5f, 67.5f }), z = 15 + (i * 13 % 52);
                if (plan.ox + x < 430) Tree(g, x, z, World.Hash(cx, cz, i + 5), World.TerrainHeight(plan.ox + x, plan.oz + z), detailed);
            }
            if (plan.ox >= 400 && plan.ox < 440)
            {
                g.Box(20, .09f, 40, 14, .09f, 33, C(.5f, .48f, .4f));
                for (float z = 10; z < 80; z += 10) { g.Box(34, .6f, z, .08f, .6f, .08f, C(.13f, .2f, .22f), Surface.Metal); g.Box(34, 1.13f, z + 5, .05f, .05f, 5, C(.22f, .28f, .27f), Surface.Metal); }
                result.colliders.Add(Col($"seawall:{cz}", 34, 1, 40, .3f, 1, 40));
            }
            // Street lamps are part of the chunk mesh; DayNight adds the actual lights.
            foreach (var lamp in plan.lamps)
            {
                var p = lamp.position - new Vector3(plan.ox, 0, plan.oz);
                g.Box(p.x, p.y + 2.9f, p.z, .065f, 2.9f, .065f, C(.15f, .19f, .19f), Surface.Metal); g.Box(p.x + .55f, p.y + 5.7f, p.z, .6f, .06f, .06f, C(.17f, .2f, .19f), Surface.Metal);
                g.Box(p.x + 1, p.y + 5.62f, p.z, .3f, .055f, .16f, C(.8f, .58f, .28f), new Surface(.4f, .1f, -3f));
            }
            result.mesh = g.Build($"Chunk {cx}:{cz}"); result.ground = ground.Build($"Ground {cx}:{cz}");
            return result;
        }

        static ColliderBox Col(string id, float x, float y, float z, float hx, float hy, float hz) => new ColliderBox { id = id, center = new Vector3(x, y, z), half = new Vector3(hx, hy, hz) };

        static void Tree(MeshBuilder g, float x, float z, uint seed, float y, bool detailed)
        {
            var r = new SeededRandom(seed); float h = r.Range(2.8f, 4.4f);
            g.Box(x, y + h * .5f, z, .16f, h * .5f, .16f, C(.18f, .12f, .075f));
            g.Sphere(x, y + h + .5f, z, 1.55f, 2f, 1.5f, r.Pick(new[] { C(.12f, .24f, .13f), C(.2f, .3f, .13f), C(.14f, .26f, .1f), C(.55f, .1f, .12f) }), new Surface(.95f), detailed ? 8 : 6);
        }

        /// <summary>Fishbone TV antennas, DTH dishes, unfinished floors with rebar and laundry lines.</summary>
        static void Rooftop2000s(MeshBuilder g, BuildingPlan b, float x, float top, float z)
        {
            var steel = C(.35f, .35f, .33f); var rod = new Surface(.5f, .8f);
            float ax = x + b.hx * .2f, az = z - b.hz * .55f;
            g.Box(ax, top + 3.2f, az, .03f, 2.6f, .03f, steel, rod);
            for (int i = 0; i < 6; i++) g.Box(ax, top + 4.2f + i * .28f, az, .55f - i * .06f, .015f, .015f, steel, rod, .4f);
            if (b.roofKind == 1) { g.Sphere(x + b.hx * .7f, top + 1.6f, z - b.hz * .7f, .45f, .45f, .12f, C(.75f, .75f, .72f), new Surface(.4f, .3f), 10); g.Box(x + b.hx * .7f, top + 1.1f, z - b.hz * .7f, .03f, .5f, .03f, steel, rod); }
            if (b.roofKind == 2)
            {
                // Next floor "coming soon": concrete pillar stubs with rusty rebar.
                foreach (var sx in new[] { -1f, 0f, 1f }) foreach (var sz in new[] { -1f, 1f })
                {
                    float px = x + sx * (b.hx - .3f), pz = z + sz * (b.hz - .3f);
                    g.Box(px, top + .9f, pz, .22f, .5f, .22f, C(.6f, .6f, .57f));
                    foreach (var o in new[] { (-.12f, -.12f), (.12f, -.12f), (-.12f, .12f), (.12f, .12f) }) g.Box(px + o.Item1, top + 1.9f, pz + o.Item2, .012f, .55f, .012f, C(.42f, .2f, .1f), new Surface(.7f, .6f));
                }
            }
            if (b.roofKind == 3)
            {
                float y = top + 1.9f; g.Box(x - b.hx * .8f, top + 1f, z + b.hz * .1f, .03f, 1f, .03f, steel); g.Box(x + b.hx * .1f, top + 1f, z + b.hz * .1f, .03f, 1f, .03f, steel);
                var clothes = new[] { C(.8f, .1f, .2f), C(.1f, .3f, .7f), C(.95f, .9f, .8f), C(.9f, .6f, .1f), C(.2f, .5f, .3f) };
                for (int i = 0; i < 6; i++) g.Box(x - b.hx * .8f + (i + .5f) * b.hx * .9f / 6, y - .35f, z + b.hz * .1f, .22f, .33f, .01f, clothes[i % 5], new Surface(.95f));
            }
        }

        /// <summary>Concrete electricity poles every 20 m with sagging, tangled wires.</summary>
        static void Poles(MeshBuilder g, ChunkPlan plan)
        {
            if (plan.district.id == "rural" || plan.ox >= 400) return;
            var pole = C(.42f, .4f, .37f); var wire = C(.03f, .03f, .03f); float x = 10.6f;
            for (float z = 12; z <= 72; z += 20)
            {
                float py = World.TerrainHeight(plan.ox + x, plan.oz + z);
                g.Box(x, py + 4.5f, z, .11f, 4.5f, .11f, pole); g.Box(x, py + 8.4f, z, .9f, .05f, .06f, pole);
                if (World.Hash(plan.cx, plan.cz, (int)z) % 4 == 0) { g.Box(x + .35f, py + 6.6f, z, .28f, .45f, .22f, C(.3f, .32f, .3f), Surface.Metal); g.Box(x + .35f, py + 5.6f, z, .05f, .55f, .05f, wire); }
                for (int w = 0; w < 4; w++)
                {
                    var a = new Vector3(x - .8f + w * .5f, py + 8.35f, z); var b = new Vector3(a.x, py + 8.35f, z + 20); float sag = .9f + w * .25f;
                    for (int i = 0; i < 20; i++) { float t0 = i / 20f, t1 = (i + 1) / 20f; var p0 = Vector3.Lerp(a, b, t0) + Vector3.down * sag * 4 * t0 * (1 - t0); var p1 = Vector3.Lerp(a, b, t1) + Vector3.down * sag * 4 * t1 * (1 - t1); var m = (p0 + p1) / 2; g.Box(m.x, m.y, m.z, .012f, .012f, (p1 - p0).magnitude / 2 + .01f, wire, new Surface(.6f)); }
                }
                for (int i = 0; i < 10; i++) { float a = i / 10f * Mathf.PI * 2; g.Box(x + Mathf.Cos(a) * .35f, py + 7.2f + Mathf.Sin(a) * .35f, z, .02f, .02f, .02f + Mathf.Abs(Mathf.Sin(a)) * .03f, wire); }
            }
        }

        static void Roads(MeshBuilder g, ChunkPlan plan, bool detailed)
        {
            var road = new Surface(.94f);
            foreach (var x in new[] { 3.5f, 76.5f }) g.Box(x, .012f, 40, 3.5f, .012f, 40, Asphalt, road);
            foreach (var z in new[] { 3.5f, 76.5f }) g.Box(40, .016f, z, 40, .016f, 3.5f, Asphalt, road);
            foreach (var x in new[] { 8.6f, 71.4f }) g.Box(x, .09f, 40, 1.6f, .09f, 33, Paving);
            foreach (var z in new[] { 8.6f, 71.4f }) g.Box(40, .09f, z, 33, .09f, 1.6f, Paving);
            for (float t = 15; t < 70; t += 12) { g.Box(.07f, .035f, t, .07f, .012f, 2.8f, C(.65f, .57f, .35f)); g.Box(t, .035f, .07f, 2.8f, .012f, .07f, C(.65f, .57f, .35f)); }
            if (detailed) for (int t = 1; t < 6; t++)
            {
                float s = 1 + t * 1.2f; var stripe = C(.75f, .75f, .65f);
                foreach (var z in new[] { 10.7f, 40f, 69.3f }) { g.Box(s, .045f, z, .35f, .014f, 1.1f, stripe); g.Box(80 - s, .045f, z, .35f, .014f, 1.1f, stripe); }
                foreach (var x in new[] { 10.7f, 40f, 69.3f }) { g.Box(x, .045f, s, 1.1f, .014f, .35f, stripe); g.Box(x, .045f, 80 - s, 1.1f, .014f, .35f, stripe); }
            }
        }

        [System.ThreadStatic] static List<SignInfo> pendingSigns;
        static void Building(MeshBuilder g, BuildingPlan b, bool detailed, float y)
        {
            float x = b.x, z = b.z, hx = b.hx, hz = b.hz, h = b.height; var color = b.color;
            g.Box(x, y + h / 2, z, hx, h / 2, hz, color, new Surface(.85f));
            g.Box(x, y + h + .18f, z, hx + .22f, .18f, hz + .22f, C(.19f, .21f, .21f), new Surface(.78f));
            var tin = new Surface(.5f, .45f);
            if (b.suburb)
            {
                g.Pyramid(x, y + h + .3f, z, hx + .6f, 2.6f, hz + .6f, b.roof, tin, 1.2f, hz * .25f);
                g.Box(x + hx * .45f, y + h + 1.5f, z - hz * .5f, .6f, 1.6f, .65f, C(.38f, .2f, .12f));
            }
            else if (!b.industrial)
            {
                // Flat roof terrace: parapet, stair room with a tin canopy and the black water tank.
                foreach (var s in new[] { -1f, 1f }) { g.Box(x + s * hx, y + h + .7f, z, .14f, .5f, hz, color); g.Box(x, y + h + .7f, z + s * hz, hx, .5f, .14f, color); }
                g.Box(x - hx * .45f, y + h + 1.4f, z - hz * .4f, hx * .35f, 1.2f, hz * .35f, color);
                g.Pyramid(x - hx * .45f, y + h + 2.6f, z - hz * .4f, hx * .42f, .9f, hz * .42f, b.roof, tin, hx * .1f, hz * .4f);
                g.Cylinder(x + hx * .5f, y + h + 1.2f, z + hz * .45f, .75f, .62f, Tank, new Surface(.4f), 10);
                if (b.flags) g.PrayerFlags(new Vector3(x - hx * .45f, y + h + 4.2f, z - hz * .4f), new Vector3(x + hx * .95f, y + h + 1.2f, z + hz * .95f), .9f);
                Rooftop2000s(g, b, x, y + h, z);
            }
            else g.Pyramid(x, y + h + .3f, z, hx + .3f, 1.6f, hz + .3f, b.roof, new Surface(.5f, .5f), hx * .1f, hz + .3f);
            if (!detailed) return;
            int floors = Mathf.Clamp(Mathf.FloorToInt(h / 3.5f), 1, 9), cols = b.suburb ? 2 : 4;
            for (int floor = 0; floor < floors; floor++)
            {
                float wy = y + 2f + floor * (h - 2.8f) / floors;
                if (!b.suburb && floor > 0) g.Box(x, wy - .75f, z, hx + .1f, .07f, hz + .1f, b.industrial ? color * .82f : Wood);
                for (int col = 0; col < cols; col++)
                {
                    float wx = x - hx + (col + .5f) * hx * 2 / cols, wz = z - hz + (col + .5f) * hz * 2 / cols;
                    bool lit = World.Hash(Mathf.FloorToInt(x + col), floor, Mathf.FloorToInt(z)) % 5 != 0;
                    var surf = lit ? new Surface(.16f, .45f, -.65f) : new Surface(.17f, .5f);
                    float w = b.suburb ? 1f : .75f;
                    g.Box(wx, wy, z + hz + .035f, w, .8f, .045f, Glass, surf); g.Box(wx, wy, z - hz - .035f, w, .8f, .045f, Glass, surf);
                    g.Box(x + hx + .035f, wy, wz, .045f, .8f, w, Glass, surf); g.Box(x - hx - .035f, wy, wz, .045f, .8f, w, Glass, surf);
                    if (!b.industrial)
                    {
                        g.Box(wx, wy, z + hz + .02f, w + .22f, 1f, .03f, Wood); g.Box(wx, wy, z - hz - .02f, w + .22f, 1f, .03f, Wood);
                        g.Box(x + hx + .02f, wy, wz, .03f, 1f, w + .22f, Wood); g.Box(x - hx - .02f, wy, wz, .03f, 1f, w + .22f, Wood);
                        g.Box(wx, wy + 1.06f, z + hz + .07f, w + .4f, .08f, .09f, Wood); g.Box(wx, wy + 1.06f, z - hz - .07f, w + .4f, .08f, .09f, Wood);
                    }
                }
            }
            g.Box(x - hx - .05f, y + 1.2f, z, .08f, 1.2f, .8f, C(.055f, .07f, .075f), new Surface(.3f, .4f));
            if (b.special != null || (!b.suburb && !b.industrial))
            {
                var awning = b.special == "cafe" ? C(.55f, .08f, .05f) : b.special == "clinic" ? C(.62f, .69f, .63f) : b.awning;
                g.Box(x - hx - 1.8f, y + 3.4f, z, 1.9f, .18f, Mathf.Min(hz, 7), awning);
                g.Box(x - hx - .12f, y + 4.1f, z, .12f, .4f, Mathf.Min(hz, 6), C(.075f, .13f, .15f), new Surface(.55f, .15f, -.4f));
                g.Box(x - hx - 3, y + .055f, z, 3.5f, .055f, 2, C(.49f, .48f, .43f));
                if (b.special == null && !string.IsNullOrEmpty(b.sign))
                {
                    var board = SignBoards[World.Hash((int)x, (int)z, 5) % SignBoards.Length];
                    g.Box(x - hx - .09f, y + 4.65f, z, .05f, .45f, Mathf.Min(hz, 6) - .3f, board, new Surface(.7f));
                    // Rolling metal shutter half down on one side.
                    for (int i = 0; i < 8; i++) g.Box(x - hx - .07f, y + 2.6f - i * .16f, z + hz * .45f, .03f, .07f, 1.4f, C(.5f, .52f, .5f), Surface.Metal);
                    pendingSigns?.Add(new SignInfo { position = new Vector3(x - hx - .13f, y + 4.65f, z), yaw = -Mathf.PI / 2, text = b.sign, ink = board.grayscale > .6f ? C(.6f, .05f, .05f) : C(.98f, .95f, .88f), size = .6f });
                }
                if (b.special == "cafe")
                {
                    g.Box(x - hx - 1.4f, y + .55f, z + 3, .5f, .55f, 1.4f, C(.24f, .16f, .09f));
                    foreach (var dz in new[] { -4f, 5f }) { g.Cylinder(x - hx - 3.5f, y + .9f, z + dz, .8f, .06f, C(.37f, .28f, .17f)); g.Box(x - hx - 3.5f, y + .45f, z + dz, .08f, .45f, .08f, C(.1f, .12f, .13f)); }
                }
                if (b.special == "clinic") { g.Box(x - hx - .15f, y + 4.1f, z, .15f, .6f, .2f, C(.7f, .2f, .15f), new Surface(.5f, 0, .12f)); g.Box(x - hx - .18f, y + 4.1f, z, .15f, .2f, .6f, C(.7f, .2f, .15f), new Surface(.5f, 0, .12f)); }
            }
            if (b.industrial)
            {
                g.Box(x - hx - .08f, y + 2, z + hz * .4f, .1f, 2, 3, C(.14f, .2f, .22f), Surface.Metal);
                for (int i = 0; i < 6; i++) g.Box(x - hx - .2f, y + .5f + i * .55f, z + hz * .4f, .05f, .035f, 3, C(.32f, .37f, .37f));
            }
        }
    }

    /// <summary>Procedural Nepali landmarks, replaced by Tripo models when present.</summary>
    public static class Landmarks
    {
        static readonly Color Brick = new Color(.42f, .14f, .075f), BrickDark = new Color(.3f, .1f, .055f), Wood = new Color(.11f, .055f, .028f), Stone = new Color(.48f, .45f, .4f);
        static readonly Color Gold = new Color(.72f, .5f, .14f), Copper = new Color(.36f, .2f, .09f), White = new Color(.86f, .85f, .8f);

        public static void Pagoda(MeshBuilder g, float x, float y, float z)
        {
            float top = y;
            for (int i = 0; i < 5; i++) { float h = .42f, half = 6 - i * .55f; g.Box(x, top + h / 2, z, half, h / 2, half, i % 2 == 1 ? BrickDark : Stone, new Surface(.9f)); top += h; }
            g.Box(x, top + 1.7f, z, 2.7f, 1.7f, 2.7f, Brick, new Surface(.85f));
            foreach (var side in new[] { -1f, 1f }) for (int k = -2; k <= 2; k++) { g.Box(x + side * 3.25f, top + 1.5f, z + k * 1.3f, .14f, 1.5f, .14f, Wood); g.Box(x + k * 1.3f, top + 1.5f, z + side * 3.25f, .14f, 1.5f, .14f, Wood); }
            top += 3.4f;
            var tiers = new[] { (4.6f, 1.9f, 2.1f, 2.2f), (3.5f, 1.6f, 1.6f, 1.8f), (2.5f, 1.5f, 0f, 0f) };
            foreach (var (roof, rise, body, bodyHeight) in tiers)
            {
                g.Box(x, top + .12f, z, roof * .78f, .12f, roof * .78f, Wood);
                g.Pyramid(x, top + .2f, z, roof, rise, roof, Copper, new Surface(.45f, .55f), roof * .32f, roof * .32f);
                foreach (var (sx, sz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) g.Sphere(x + sx * roof * .95f, top - .05f, z + sz * roof * .95f, .12f, .16f, .12f, Gold, Surface.Gilt, 6);
                top += .2f + rise;
                if (body > 0) { g.Box(x, top + bodyHeight / 2, z, body, bodyHeight / 2, body, Brick, new Surface(.85f)); top += bodyHeight; }
            }
            g.Pyramid(x, top, z, .8f, .5f, .8f, Gold, Surface.Gilt, .3f, .3f);
            g.Sphere(x, top + 1f, z, .38f, .55f, .38f, Gold, Surface.Gilt, 8); g.Cylinder(x, top + 1.9f, z, .07f, .5f, Gold, Surface.Gilt, 6);
            var crown = new Vector3(x, top + 1.6f, z);
            foreach (var (dx, dz) in new[] { (-15f, -15f), (15f, -15f), (-15f, 15f), (15f, 15f) }) g.PrayerFlags(crown, new Vector3(x + dx, y + 4.5f, z + dz), 1.6f, .55f);
        }

        public static void Stupa(MeshBuilder g, float x, float y, float z)
        {
            float top = y;
            for (int i = 0; i < 3; i++) { float half = 9 - i * 1.4f, h = .75f; g.Box(x, top + h / 2, z, half, h / 2, half, White, new Surface(.85f)); g.Box(x, top + h - .04f, z, half + .05f, .05f, half + .05f, new Color(.62f, .2f, .1f)); top += h; }
            g.Sphere(x, top, z, 5.6f, 4.4f, 5.6f, White, new Surface(.85f), 20);
            top += 4.2f;
            g.Box(x, top + 1.3f, z, 1.6f, 1.3f, 1.6f, Gold, Surface.Gilt);
            var dark = new Color(.03f, .03f, .05f);
            foreach (var (dx, dz) in new[] { (0f, 1f), (0f, -1f), (1f, 0f), (-1f, 0f) })
            {
                float yaw = dx != 0 ? Mathf.PI / 2 : 0, fx = x + dx * 1.62f, fz = z + dz * 1.62f;
                foreach (var u in new[] { -.55f, .55f })
                {
                    float ex = fx + (dz != 0 ? u : 0), ez = fz + (dx != 0 ? u : 0);
                    g.Box(ex, top + 1.55f, ez, .34f, .13f, .03f, new Color(.95f, .95f, .92f), new Surface(.6f), yaw);
                    g.Box(ex, top + 1.55f, ez, .12f, .12f, .04f, dark, new Surface(.6f), yaw);
                    g.Box(ex, top + 1.85f, ez, .38f, .05f, .04f, dark, new Surface(.6f), yaw);
                }
                g.Box(fx, top + 1.05f, fz, .1f, .22f, .04f, dark, new Surface(.6f), yaw);
            }
            top += 2.6f;
            for (int i = 0; i < 13; i++) { float r = 1.25f - i * .07f; g.Cylinder(x, top + .17f, z, r, .17f, Gold, Surface.Gilt, 12); top += .36f; }
            g.Cylinder(x, top + .1f, z, 1f, .1f, Gold, Surface.Gilt, 12); g.Sphere(x, top + .55f, z, .32f, .5f, .32f, Gold, Surface.Gilt, 8);
            var crown = new Vector3(x, top - 1, z);
            for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4 + Mathf.PI / 8; g.PrayerFlags(crown, new Vector3(x + Mathf.Cos(a) * 15, y + 2.2f, z + Mathf.Sin(a) * 15), 2.2f, .55f); }
        }

        public static void Chautari(MeshBuilder g, float x, float y, float z, uint seed)
        {
            var r = new SeededRandom(seed);
            g.Box(x, y + .35f, z, 3.6f, .35f, 3.6f, Stone, new Surface(.95f)); g.Box(x, y + .9f, z, 2.6f, .2f, 2.6f, new Color(.44f, .41f, .36f), new Surface(.95f));
            g.Box(x, y + 2.6f, z, .45f, 2, .45f, new Color(.28f, .2f, .13f));
            for (int i = 0; i < 5; i++) g.Sphere(x + r.Range(-2, 2), y + 5.6f + r.Range(-.5f, 1.2f), z + r.Range(-2, 2), r.Range(2.2f, 3), r.Range(1.6f, 2.2f), r.Range(2.2f, 3), r.Pick(new[] { new Color(.13f, .27f, .1f), new Color(.17f, .31f, .11f), new Color(.11f, .23f, .09f) }), new Surface(.95f), 8);
            g.PrayerFlags(new Vector3(x, y + 4.5f, z), new Vector3(x + 6, y + 2.6f, z + 4), .6f);
        }

        public static void Doonga(MeshBuilder g, float x, float y, float z, float yaw, uint seed)
        {
            var colors = new[] { new Color(.6f, .06f, .04f), new Color(.04f, .2f, .55f), new Color(.8f, .55f, .04f), new Color(.05f, .38f, .2f) };
            var r = new SeededRandom(seed); var hull = r.Pick(colors); var trim = r.Pick(colors);
            g.Box(x, y, z, .75f, .3f, 3.4f, hull, new Surface(.55f), yaw); g.Box(x, y + .32f, z, .8f, .05f, 3.45f, trim, new Surface(.55f), yaw);
            g.Box(x, y + .05f, z, .6f, .26f, 3.2f, new Color(.3f, .19f, .1f), new Surface(.8f), yaw);
        }

        /// <summary>Himalaya backdrop, valley hills, outer ground and the lake.</summary>
        public static Mesh Landscape(bool withBoats)
        {
            var g = new MeshBuilder();
            ValleyBackdrop.Build(g);
            var flat = new Color(.2f, .25f, .14f);
            foreach (var (x0, z0, x1, z1) in new[] { (-1600f, -1600f, 436f, -480f), (-1600f, -480f, -480f, 1300f), (-1600f, 560f, 436f, 1300f), (436f, -1600f, 1046f, -660f), (436f, 740f, 1046f, 1300f), (1046f, -1600f, 1800f, 1300f) })
                g.Facing(new Vector3((x0 + x1) / 2, -10, (z0 + z1) / 2), new Vector3(x0, -.25f, z0), new Vector3(x1, -.25f, z0), new Vector3(x1, -.25f, z1), new Vector3(x0, -.25f, z1), flat, new Surface(.95f));
            // Lake bed below the water, so the shore never shows the void.
            g.Facing(new Vector3(740, 10, 40), new Vector3(436, -3.6f, -660), new Vector3(1046, -3.6f, -660), new Vector3(1046, -3.6f, 740), new Vector3(436, -3.6f, 740), new Color(.12f, .14f, .1f), new Surface(.95f));
            if (withBoats) { uint i = 3; foreach (var (x, z, yaw) in new[] { (468f, -40f, .3f), (476f, 22f, -.5f), (462f, 92f, 1.2f), (492f, 160f, .1f), (455f, 48f, 2.6f), (520f, -110f, .8f) }) Doonga(g, x, -.95f, z, yaw, i++); }
            foreach (var z in new[] { -60f, 140f }) g.Box(445, -.8f, z, 2, .25f, 9, new Color(.3f, .2f, .1f));
            return g.Build("Landscape");
        }
    }
}

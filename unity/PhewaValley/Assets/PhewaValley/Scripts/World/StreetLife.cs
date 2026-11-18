// 2000s street life around the player: Safa tempos, micro buses, decorated trucks,
// motorbikes and Maruti taxis driving on the left, plus cows and dozing street dogs.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PhewaValley
{
    public class StreetLife : MonoBehaviour
    {
        class Vehicle { public Transform t; public bool alongX; public float line, dir, speed, cruise, honk; }
        class Animal { public Transform t; public Vector3 target; public float wait; public bool walker, cow; }
        readonly List<Vehicle> vehicles = new List<Vehicle>(); readonly List<Animal> animals = new List<Animal>();
        readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        /// <summary>Points on the road that traffic must stop for (a cow, an event).</summary>
        public static readonly List<Vector3> Obstacles = new List<Vector3>();
        int VehicleCount => Game.Instance.activities.festivals.Today == "Bandh" ? 2 : new[] { 10, 26, 40 }[Mathf.Clamp(Game.Instance.progress.density, 0, 2)];
        const int CowCount = 9, DogCount = 12; const float Range = 210;
        class Critter { public Transform t; public string kind; public Vector3 home; public float phase, flee; }
        readonly List<Critter> critters = new List<Critter>();

        static Color C(float r, float g, float b) => new Color(r, g, b);
        static readonly Surface Head = new Surface(.3f, 0, -2.5f, 1), Tail = new Surface(.3f, 0, -1.2f, 1);

        Mesh Tempo()
        {
            var g = new MeshBuilder(); var green = C(.12f, .45f, .22f); var white = C(.9f, .9f, .86f);
            g.Box(0, .75f, .2f, .7f, .45f, 1.35f, green, new Surface(.4f, .2f)); g.Box(0, 1.55f, .2f, .72f, .35f, 1.3f, white, new Surface(.4f, .2f));
            g.Pyramid(0, .4f, 1.6f, .55f, 1.3f, .2f, green, new Surface(.4f, .2f), .5f, .15f); g.Box(0, 1.5f, 1.66f, .45f, .25f, .02f, C(.08f, .12f, .14f), new Surface(.1f, .5f));
            g.Box(0, .6f, 1.78f, .1f, .06f, .02f, C(1, .95f, .8f), Head); g.Box(-.5f, .55f, -1.16f, .1f, .06f, .02f, C(.7f, .05f, .03f), Tail); g.Box(.5f, .55f, -1.16f, .1f, .06f, .02f, C(.7f, .05f, .03f), Tail);
            foreach (var (x, z) in new[] { (0f, 1.35f), (-.65f, -.85f), (.65f, -.85f) }) g.Cylinder(x, .26f, z, .26f, .08f, C(.03f, .03f, .03f), new Surface(.9f), 10);
            return g.Build("Tempo");
        }
        Mesh MicroBus()
        {
            var g = new MeshBuilder(); var white = C(.92f, .92f, .9f);
            g.Box(0, 1.15f, 0, .85f, .85f, 2.3f, white, new Surface(.35f, .2f)); g.Box(0, 1.0f, 0, .86f, .12f, 2.31f, C(.1f, .25f, .65f));
            for (int i = 0; i < 4; i++) foreach (var s in new[] { -.86f, .86f }) g.Box(s, 1.55f, -1.3f + i * .85f, .01f, .25f, .33f, C(.08f, .12f, .14f), new Surface(.1f, .5f));
            g.Box(0, 1.5f, 2.31f, .7f, .3f, .01f, C(.08f, .12f, .14f), new Surface(.1f, .5f)); g.Box(0, 2.1f, -.2f, .7f, .06f, 1.6f, C(.25f, .25f, .25f), Surface.Metal);
            foreach (var s in new[] { -.6f, .6f }) { g.Box(s, .75f, 2.32f, .12f, .07f, .01f, C(1, .95f, .8f), Head); g.Box(s, .75f, -2.32f, .1f, .08f, .01f, C(.7f, .05f, .03f), Tail); }
            foreach (var (x, z) in new[] { (-.8f, 1.5f), (.8f, 1.5f), (-.8f, -1.5f), (.8f, -1.5f) }) g.Cylinder(x, .33f, z, .33f, .1f, C(.03f, .03f, .03f), new Surface(.9f), 10);
            return g.Build("Micro bus");
        }
        Mesh Truck()
        {
            // Hand-painted Tata-style truck: colourful wooden body and a decorated cab.
            var g = new MeshBuilder(); var paints = new[] { C(.8f, .1f, .1f), C(.95f, .7f, .1f), C(.1f, .4f, .7f), C(.1f, .55f, .3f) };
            g.Box(0, 1.6f, 1.9f, .95f, .9f, .7f, C(.85f, .55f, .1f), new Surface(.4f, .2f)); g.Box(0, 1.9f, 2.61f, .8f, .35f, .01f, C(.08f, .12f, .14f), new Surface(.1f, .5f));
            g.Box(0, 2.65f, 2.0f, 1f, .18f, .55f, paints[2]);
            for (int i = 0; i < 6; i++) g.Box(0, 1.1f + i * .3f, -1.1f, 1.05f, .15f, 2.2f, paints[i % 4], new Surface(.6f));
            g.Box(0, .55f, -.4f, .95f, .12f, 3.2f, C(.15f, .15f, .15f), Surface.Metal);
            foreach (var s in new[] { -.7f, .7f }) { g.Box(s, .9f, 2.61f, .12f, .08f, .01f, C(1, .95f, .8f), Head); g.Box(s, .9f, -3.31f, .1f, .08f, .01f, C(.7f, .05f, .03f), Tail); }
            foreach (var (x, z) in new[] { (-.9f, 1.9f), (.9f, 1.9f), (-.9f, -2.2f), (.9f, -2.2f), (-.9f, -1.3f), (.9f, -1.3f) }) g.Cylinder(x, .45f, z, .45f, .14f, C(.03f, .03f, .03f), new Surface(.9f), 10);
            return g.Build("Truck");
        }
        Mesh Bike()
        {
            var g = new MeshBuilder(); var red = C(.65f, .05f, .05f);
            g.Cylinder(0, .32f, .62f, .32f, .05f, C(.03f, .03f, .03f), new Surface(.9f), 10); g.Cylinder(0, .32f, -.62f, .32f, .05f, C(.03f, .03f, .03f), new Surface(.9f), 10);
            g.Box(0, .62f, 0, .14f, .18f, .5f, red, new Surface(.3f, .4f)); g.Box(0, .82f, -.15f, .14f, .05f, .35f, C(.08f, .08f, .08f)); g.Box(0, .95f, .55f, .35f, .03f, .03f, C(.2f, .2f, .2f), Surface.Metal);
            g.Box(0, .9f, .7f, .07f, .06f, .02f, C(1, .95f, .8f), Head);
            // Rider in a jacket and helmet.
            g.Box(0, 1.25f, -.1f, .2f, .3f, .14f, C(.15f, .2f, .3f)); g.Sphere(0, 1.7f, -.08f, .14f, .15f, .14f, C(.85f, .1f, .1f), new Surface(.3f), 8);
            g.Box(-.12f, .85f, .05f, .07f, .07f, .3f, C(.1f, .1f, .12f)); g.Box(.12f, .85f, .05f, .07f, .07f, .3f, C(.1f, .1f, .12f));
            return g.Build("Bike");
        }
        Mesh Taxi()
        {
            var g = new MeshBuilder(); var black = C(.05f, .05f, .05f);
            g.Box(0, .62f, 0, .78f, .3f, 1.75f, black, new Surface(.3f, .3f)); g.Box(0, 1.12f, -.15f, .72f, .26f, 1.05f, black, new Surface(.3f, .3f)); g.Box(0, 1.45f, -.15f, .25f, .08f, .15f, C(.95f, .8f, .1f), new Surface(.4f, 0, -1.5f, 1));
            foreach (var s in new[] { -.5f, .5f }) { g.Box(s, .72f, 1.76f, .14f, .07f, .01f, C(1, .95f, .8f), Head); g.Box(s, .72f, -1.76f, .14f, .07f, .01f, C(.7f, .05f, .03f), Tail); }
            foreach (var (x, z) in new[] { (-.72f, 1.1f), (.72f, 1.1f), (-.72f, -1.1f), (.72f, -1.1f) }) g.Cylinder(x, .3f, z, .3f, .1f, C(.02f, .02f, .02f), new Surface(.9f), 10);
            return g.Build("Taxi");
        }
        Mesh Cow(bool lying)
        {
            var g = new MeshBuilder(); var hide = C(.88f, .86f, .8f); float y = lying ? .45f : .95f;
            g.Box(0, y, 0, .32f, .3f, .75f, hide); g.Box(0, y + .2f, .85f, .16f, .17f, .22f, hide); g.Box(0, y + .32f, .95f, .2f, .03f, .03f, C(.4f, .35f, .3f)); g.Box(0, y + .3f, .2f, .12f, .12f, .25f, hide); // hump
            if (!lying) foreach (var (x, z) in new[] { (-.2f, .55f), (.2f, .55f), (-.2f, -.55f), (.2f, -.55f) }) g.Box(x, .35f, z, .06f, .35f, .06f, hide);
            g.Box(0, y, -.85f, .03f, .3f, .03f, C(.5f, .45f, .4f)); return g.Build(lying ? "Cow lying" : "Cow");
        }
        Mesh Dog()
        {
            var g = new MeshBuilder(); var fur = C(.45f, .3f, .15f); g.Sphere(0, .18f, 0, .32f, .16f, .2f, fur, new Surface(.95f), 8); g.Sphere(.28f, .18f, .12f, .11f, .1f, .1f, fur, new Surface(.95f), 6); g.Box(.32f, .25f, .14f, .03f, .05f, .03f, C(.2f, .12f, .06f));
            return g.Build("Dog");
        }

        Mesh M(string id) { if (meshes.TryGetValue(id, out var m)) return m; m = id switch { "tempo" => Tempo(), "bus" => MicroBus(), "truck" => Truck(), "bike" => Bike(), "taxi" => Taxi(), "cow" => Cow(false), "cowlying" => Cow(true), _ => Dog() }; meshes[id] = m; return m; }

        GameObject Body(string id, Vector3 half, float cy)
        {
            var go = new GameObject(id) { layer = 2 }; go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = M(id); go.AddComponent<MeshRenderer>().sharedMaterial = Materials.VertexLit;
            var box = go.AddComponent<BoxCollider>(); box.size = half * 2; box.center = new Vector3(0, cy, 0);
            var rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true;
            if (id == "truck") Plate(go.transform, new Vector3(0, 1.8f, -3.33f), "HORN OK PLEASE\nहर्न बजाउनुहोस्");
            if (id == "bus") Plate(go.transform, new Vector3(0, 1.95f, 2.33f), "PRITHVI CHOWK - LAKESIDE", true);
            return go;
        }

        void Plate(Transform parent, Vector3 at, string text, bool front = false) =>
            DevaText.Paint(parent, at, Quaternion.Euler(0, front ? 180 : 0, 0), text, front ? C(.9f, .1f, .1f) : C(1, .9f, .2f), .45f);

        void Update()
        {
            var G = Game.Instance; if (G == null || !G.Ready) return;
            var p = G.player.transform.position; float dt = Time.deltaTime;
            while (vehicles.Count < VehicleCount) vehicles.Add(SpawnVehicle(p, false));
            while (vehicles.Count > VehicleCount) { Destroy(vehicles[vehicles.Count - 1].t.gameObject); vehicles.RemoveAt(vehicles.Count - 1); }
            Wildlife(G, p, dt);
            while (animals.Count < CowCount + DogCount) animals.Add(SpawnAnimal(p, animals.Count < CowCount));
            var car = G.car.transform.position;
            foreach (var v in vehicles)
            {
                if ((v.t.position - p).sqrMagnitude > Range * Range * 1.3f || OutOfWorld(v)) { Respawn(v, p); continue; }
                var fwd = v.t.forward; float blocked = 99;
                foreach (var obstacle in Obstacles.Concat(new[] { p, car }))
                {
                    var d = obstacle - v.t.position; float ahead = Vector3.Dot(d, fwd), side = Vector3.Cross(fwd, d).magnitude;
                    if (ahead > 0 && ahead < blocked && side < 2.2f) blocked = ahead;
                }
                foreach (var o in vehicles) if (o != v && o.alongX == v.alongX && Mathf.Abs(o.line - v.line) < .5f && o.dir == v.dir) { float ahead = Vector3.Dot(o.t.position - v.t.position, fwd); if (ahead > 0 && ahead < blocked) blocked = ahead; }
                float target = blocked < 7 ? 0 : blocked < 16 ? v.cruise * .4f : v.cruise;
                v.speed = Mathf.MoveTowards(v.speed, target, (target < v.speed ? 9 : 3) * dt);
                v.t.position += fwd * v.speed * dt;
                v.honk -= dt; if (blocked < 9 && v.honk <= 0 && (v.t.position - p).magnitude < 25) { G.audioDirector.Effect("bus_horn"); v.honk = 8; }
            }
            foreach (var a in animals)
            {
                if ((a.t.position - p).sqrMagnitude > Range * Range * 1.2f) { Destroy(a.t.gameObject); var fresh = SpawnAnimal(p, a.cow); a.t = fresh.t; a.walker = fresh.walker; a.target = fresh.target; continue; }
                if (!a.walker) continue;
                a.wait -= dt; if (a.wait > 0) continue;
                var d = a.target - a.t.position; d.y = 0;
                if (d.magnitude < .3f) { a.wait = Random.Range(6, 20); a.target = a.t.position + new Vector3(Random.Range(-6, 6), 0, Random.Range(-6, 6)); }
                else { a.t.position += d.normalized * .45f * dt; a.t.rotation = Quaternion.Slerp(a.t.rotation, Quaternion.LookRotation(d), dt); }
            }
        }

        /// <summary>Wildlife so the valley never feels hollow: stupa monkeys, hill goats, pigeons that scatter.</summary>
        void Wildlife(Game G, Vector3 p, float dt)
        {
            var district = World.DistrictAt(p.x, p.z).id; bool nearStupa = World.DistanceXZ(p, -120, 214) < 120, rural = district == "rural";
            int want = (nearStupa ? 8 : 0) + (rural ? 8 : 0) + (district == "downtown" || district == "suburbs" ? 14 : 0);
            while (critters.Count < want)
            {
                string kind = nearStupa && critters.Count(c => c.kind == "monkey") < 8 ? "monkey" : rural && critters.Count(c => c.kind == "goat") < 8 ? "goat" : "pigeon";
                Vector3 home = kind == "monkey" ? new Vector3(-120 + Random.Range(-20f, 20f), 0, 214 + Random.Range(-20f, 20f)) : p + new Vector3(Random.Range(-35f, 35f), 0, Random.Range(-35f, 35f));
                home.y = World.TerrainHeight(home.x, home.z);
                var go = new GameObject(kind); go.transform.SetParent(transform, false); go.AddComponent<MeshFilter>().sharedMesh = CritterMesh(kind); go.AddComponent<MeshRenderer>().sharedMaterial = Materials.VertexLit;
                go.transform.position = home; critters.Add(new Critter { t = go.transform, kind = kind, home = home, phase = Random.Range(0, 10) });
            }
            for (int i = critters.Count - 1; i >= 0; i--)
            {
                var c = critters[i]; c.phase += dt;
                if (World.DistanceXZ(c.t.position, p) > 140 || critters.Count > want + 4) { Destroy(c.t.gameObject); critters.RemoveAt(i); continue; }
                float near = World.DistanceXZ(c.t.position, p);
                if (c.kind == "pigeon")
                {
                    if (c.flee <= 0 && near < (G.player.Speed > 4 ? 6 : 3)) c.flee = .01f;
                    if (c.flee > 0) { c.flee += dt; c.t.position += (c.t.position - p).normalized * 7 * dt + Vector3.up * 5 * dt; if (c.flee > 4) { Destroy(c.t.gameObject); critters.RemoveAt(i); } }
                    else c.t.position = c.home + new Vector3(Mathf.Sin(c.phase * .7f) * .6f, 0, Mathf.Cos(c.phase * .5f) * .6f);
                }
                else
                {
                    // Monkeys hop about; goats graze slowly. Both keep a respectful distance.
                    float hop = c.kind == "monkey" ? Mathf.Abs(Mathf.Sin(c.phase * 4)) * .25f : 0;
                    var wander = c.home + new Vector3(Mathf.Sin(c.phase * .21f) * 4, 0, Mathf.Cos(c.phase * .17f) * 4);
                    if (near < 2.5f) wander = c.t.position + (c.t.position - p).normalized * 2;
                    var pos = Vector3.MoveTowards(c.t.position, wander, (c.kind == "monkey" ? 2.5f : .5f) * dt); pos.y = World.TerrainHeight(pos.x, pos.z) + hop;
                    var dir = pos - c.t.position; dir.y = 0; if (dir.sqrMagnitude > 1e-5f) c.t.rotation = Quaternion.LookRotation(dir); c.t.position = pos;
                }
            }
        }

        Mesh CritterMesh(string kind)
        {
            if (meshes.TryGetValue(kind, out var m)) return m; var g = new MeshBuilder();
            if (kind == "monkey") { var fur = C(.42f, .32f, .22f); g.Sphere(0, .32f, 0, .16f, .2f, .22f, fur, new Surface(.95f), 8); g.Sphere(0, .52f, .18f, .1f, .1f, .1f, C(.62f, .45f, .38f), new Surface(.9f), 8); g.Box(0, .4f, -.3f, .02f, .02f, .2f, fur); foreach (var x in new[] { -.08f, .08f }) foreach (var z in new[] { -.1f, .12f }) g.Box(x, .1f, z, .03f, .1f, .03f, fur); }
            else if (kind == "goat") { var hide = C(.18f, .14f, .12f); g.Box(0, .55f, 0, .17f, .17f, .38f, hide); g.Box(0, .72f, .42f, .08f, .1f, .14f, hide); g.Box(0, .86f, .38f, .07f, .06f, .02f, C(.5f, .45f, .35f)); foreach (var x in new[] { -.1f, .1f }) foreach (var z in new[] { -.28f, .28f }) g.Box(x, .2f, z, .03f, .2f, .03f, hide); }
            else { var grey = C(.42f, .44f, .5f); g.Sphere(0, .1f, 0, .08f, .07f, .12f, grey, new Surface(.8f), 6); g.Sphere(0, .17f, .09f, .045f, .045f, .045f, C(.3f, .35f, .38f), new Surface(.8f), 6); }
            m = g.Build(kind); meshes[kind] = m; return m;
        }

        /// <summary>A moving vehicle whose front is touching this point (a pedestrian stepping out too late).</summary>
        public bool Hits(Vector3 p, out Vector3 velocity)
        {
            foreach (var v in vehicles)
            {
                if (v.speed < 3.5f) continue; var d = p - v.t.position; d.y = 0;
                float ahead = Vector3.Dot(d, v.t.forward), side = Mathf.Abs(Vector3.Dot(d, v.t.right));
                if (ahead > 0 && ahead < 2.7f && side < 1.05f) { velocity = v.t.forward * v.speed; return true; }
            }
            velocity = default; return false;
        }

        /// <summary>A vehicle passing fast and close (for a flinch).</summary>
        public bool NearMiss(Vector3 p)
        {
            foreach (var v in vehicles)
            {
                if (v.speed < 7) continue; var d = p - v.t.position; d.y = 0;
                float ahead = Vector3.Dot(d, v.t.forward), side = Mathf.Abs(Vector3.Dot(d, v.t.right));
                if (ahead > -2 && ahead < 3 && side < 2.6f) return true;
            }
            return false;
        }

        bool OutOfWorld(Vehicle v) { var q = v.t.position; return q.x < -470 || q.x > 428 || q.z < -470 || q.z > 550; }

        Vehicle SpawnVehicle(Vector3 around, bool _)
        {
            float r = Random.value; string kind = r < .3f ? "tempo" : r < .5f ? "bus" : r < .7f ? "bike" : r < .85f ? "taxi" : "truck";
            Vector3 half = kind switch { "bus" => new Vector3(.9f, 1.1f, 2.35f), "truck" => new Vector3(1.05f, 1.5f, 3.35f), "bike" => new Vector3(.3f, .9f, 1f), "tempo" => new Vector3(.75f, 1f, 1.8f), _ => new Vector3(.8f, .75f, 1.8f) };
            GameObject go = null;
            if (kind == "taxi" && Game.Instance.models.Has("car")) { go = Game.Instance.models.Instantiate("car", transform, Vector3.zero, 0, 1.5f); go.layer = 2; var box = go.AddComponent<BoxCollider>(); box.size = half * 2 / go.transform.localScale.x; box.center = new Vector3(0, half.y, 0) / go.transform.localScale.x; go.AddComponent<Rigidbody>().isKinematic = true; }
            else go = Body(kind, half, half.y);
            var v = new Vehicle { t = go.transform, cruise = kind switch { "bike" => 12, "bus" => 9, "truck" => 8, "tempo" => 7, _ => 11 } };
            Respawn(v, around); return v;
        }

        void Respawn(Vehicle v, Vector3 around)
        {
            // Nepal drives on the left: with Unity's +x to the right, the left lane is at -x of the direction of travel.
            v.alongX = Random.value < .5f; v.dir = Random.value < .5f ? 1 : -1;
            float across = v.alongX ? around.z : around.x; int k = Mathf.RoundToInt((across + Random.Range(-Range, Range) * .7f) / 80);
            k = Mathf.Clamp(k, -5, v.alongX ? 6 : 5); float road = k * 80;
            float along = (v.alongX ? around.x : around.z) + Random.Range(-Range, Range) * .9f;
            if (v.alongX) { v.line = road + 2.2f * v.dir; along = Mathf.Clamp(along, -460, 420); v.t.SetPositionAndRotation(new Vector3(along, World.TerrainHeight(along, v.line), v.line), Quaternion.LookRotation(Vector3.right * v.dir)); }
            else { v.line = road - 2.2f * v.dir; along = Mathf.Clamp(along, -460, 540); v.t.SetPositionAndRotation(new Vector3(v.line, World.TerrainHeight(v.line, along), along), Quaternion.LookRotation(Vector3.forward * v.dir)); }
            v.speed = v.cruise * .7f; v.honk = Random.Range(0, 8);
            if ((v.t.position - Game.Instance.player.transform.position).sqrMagnitude < 30 * 30) v.t.position += v.t.forward * -60; // never pop in next to the player
        }

        Animal SpawnAnimal(Vector3 around, bool cow)
        {
            bool walker = cow && Random.value < .5f; var kind = cow ? (walker ? "cow" : "cowlying") : "dog";
            var go = new GameObject(kind) { layer = 2 }; go.transform.SetParent(transform, false); go.AddComponent<MeshFilter>().sharedMesh = M(kind); go.AddComponent<MeshRenderer>().sharedMaterial = Materials.VertexLit;
            if (cow) { var b = go.AddComponent<BoxCollider>(); b.size = new Vector3(.7f, 1.2f, 1.9f); b.center = new Vector3(0, .6f, 0); }
            // On footpaths and road shoulders, where they really sit.
            float x = Mathf.Round((around.x + Random.Range(-Range, Range) * .8f) / 80) * 80 + (Random.value < .5f ? 9.2f : -9.2f), z = around.z + Random.Range(-Range, Range) * .8f;
            if (Random.value < .5f) { x = around.x + Random.Range(-Range, Range) * .8f; z = Mathf.Round((around.z + Random.Range(-Range, Range) * .8f) / 80) * 80 + (Random.value < .5f ? 9.2f : -9.2f); }
            x = Mathf.Clamp(x, -460, 420); z = Mathf.Clamp(z, -460, 540);
            go.transform.SetPositionAndRotation(new Vector3(x, World.TerrainHeight(x, z), z), Quaternion.Euler(0, Random.Range(0, 360), 0));
            return new Animal { t = go.transform, walker = walker, cow = cow, target = go.transform.position, wait = Random.Range(2, 10) };
        }
    }
}

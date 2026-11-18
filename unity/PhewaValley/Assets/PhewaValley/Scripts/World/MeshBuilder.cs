// Procedural mesh builder (port of src/world/geometry.js). Every face is emitted
// through Facing(), which fixes the winding from an "inside" point, so geometry
// is always front-facing in Unity's left-handed convention.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PhewaValley
{
    /// <summary>Per-vertex surface: x = roughness, y = metallic, z = emission (negative = only at night).</summary>
    public struct Surface
    {
        public float roughness, metallic, emission, battery;
        /// <param name="battery">1 = keeps glowing during load shedding (vehicle lamps, candles).</param>
        public Surface(float roughness = .8f, float metallic = 0, float emission = 0, float battery = 0) { this.roughness = roughness; this.metallic = metallic; this.emission = emission; this.battery = battery; }
        public static readonly Surface Matte = new Surface(.9f);
        public static readonly Surface Gilt = new Surface(.28f, .9f);
        public static readonly Surface Metal = new Surface(.35f, .65f);
    }

    public class MeshBuilder
    {
        readonly List<Vector3> positions = new List<Vector3>(4096), normals = new List<Vector3>(4096);
        readonly List<Color> colors = new List<Color>(4096);
        readonly List<Vector4> surfaces = new List<Vector4>(4096);
        readonly List<int> indices = new List<int>(8192);
        public int VertexCount => positions.Count;

        public static readonly Color[] PrayerColors = { new Color(.04f, .18f, .62f), new Color(.85f, .85f, .82f), new Color(.72f, .06f, .04f), new Color(.05f, .42f, .12f), new Color(.88f, .7f, .05f) };

        public MeshBuilder Facing(Vector3 inside, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Surface s)
        {
            var n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-12f) n = Vector3.Cross(c - a, d - a);
            var centre = (a + b + c + d) * .25f;
            if (Vector3.Dot(n, centre - inside) < 0) { (a, d) = (d, a); (b, c) = (c, b); n = -n; }
            n.Normalize();
            int start = positions.Count;
            positions.Add(a); positions.Add(b); positions.Add(c); positions.Add(d);
            var sv = new Vector4(s.roughness, s.metallic, s.emission, s.battery);
            for (int i = 0; i < 4; i++) { normals.Add(n); colors.Add(color); surfaces.Add(sv); }
            indices.Add(start); indices.Add(start + 1); indices.Add(start + 2); indices.Add(start); indices.Add(start + 2); indices.Add(start + 3);
            return this;
        }

        public MeshBuilder Box(float x, float y, float z, float hx, float hy, float hz, Color color, Surface s = default, float yaw = 0)
        {
            if (s.roughness == 0 && s.metallic == 0 && s.emission == 0) s = new Surface();
            var c = new Vector3(x, y, z); var r = Quaternion.Euler(0, yaw * Mathf.Rad2Deg, 0);
            Vector3 P(float sx, float sy, float sz) => c + r * new Vector3(sx * hx, sy * hy, sz * hz);
            Facing(c, P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1), color, s);
            Facing(c, P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), color, s);
            Facing(c, P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), color, s);
            Facing(c, P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1), color, s);
            Facing(c, P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), color, s);
            Facing(c, P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1), color, s);
            return this;
        }

        public MeshBuilder Sphere(float x, float y, float z, float sx, float sy, float sz, Color color, Surface s = default, int segments = 8)
        {
            if (s.roughness == 0 && s.metallic == 0) s = new Surface();
            int rings = Mathf.Max(4, segments / 2); var c = new Vector3(x, y, z);
            Vector3 P(int i, int j) { float phi = j * Mathf.PI / rings, th = i * 2 * Mathf.PI / segments; return c + new Vector3(Mathf.Sin(phi) * Mathf.Cos(th) * sx, Mathf.Cos(phi) * sy, Mathf.Sin(phi) * Mathf.Sin(th) * sz); }
            for (int j = 0; j < rings; j++) for (int i = 0; i < segments; i++) Facing(c, P(i, j), P(i + 1, j), P(i + 1, j + 1), P(i, j + 1), color, s);
            return this;
        }

        public MeshBuilder Cylinder(float x, float y, float z, float radius, float halfHeight, Color color, Surface s = default, int segments = 8)
        {
            if (s.roughness == 0 && s.metallic == 0) s = new Surface();
            var c = new Vector3(x, y, z);
            for (int i = 0; i < segments; i++)
            {
                float a = i * 2 * Mathf.PI / segments, b = (i + 1) * 2 * Mathf.PI / segments;
                var p = new Vector3(x + Mathf.Cos(a) * radius, y - halfHeight, z + Mathf.Sin(a) * radius);
                var q = new Vector3(x + Mathf.Cos(b) * radius, y - halfHeight, z + Mathf.Sin(b) * radius);
                var top = new Vector3(0, halfHeight * 2, 0);
                Facing(c, p, q, q + top, p + top, color, s);
                Facing(c - Vector3.up * halfHeight * 4, new Vector3(x, y + halfHeight, z), p + top, q + top, q + top, color, s);
            }
            return this;
        }

        /// <summary>Four-sided roof or frustum: base half extents shrink to (tx, tz) at y + height.</summary>
        public MeshBuilder Pyramid(float x, float y, float z, float hx, float height, float hz, Color color, Surface s, float tx = 0, float tz = 0, float yaw = 0)
        {
            var r = Quaternion.Euler(0, yaw * Mathf.Rad2Deg, 0); var o = new Vector3(x, y, z);
            Vector3 P(float dx, float dy, float dz) => o + r * new Vector3(dx, dy, dz);
            var b = new[] { P(-hx, 0, -hz), P(hx, 0, -hz), P(hx, 0, hz), P(-hx, 0, hz) };
            var t = new[] { P(-tx, height, -tz), P(tx, height, -tz), P(tx, height, tz), P(-tx, height, tz) };
            var inside = new Vector3(x, y + height * .3f, z);
            for (int i = 0; i < 4; i++) { int j = (i + 1) % 4; Facing(inside, b[i], b[j], t[j], t[i], color, s); }
            if (tx > 0 || tz > 0) Facing(o, t[0], t[1], t[2], t[3], color, s);
            Facing(o + Vector3.up, b[0], b[1], b[2], b[3], color, s);
            return this;
        }

        /// <summary>A sagging string of five-colour prayer flags from a to b.</summary>
        public MeshBuilder PrayerFlags(Vector3 a, Vector3 b, float sag = .8f, float spacing = .5f)
        {
            float length = Vector3.Distance(a, b); int count = Mathf.Max(2, Mathf.FloorToInt(length / spacing));
            var dir = b - a; float yaw = Mathf.Atan2(dir.x, dir.z) + Mathf.PI / 2;
            for (int i = 1; i < count; i++)
            {
                float t = (float)i / count; var p = Vector3.Lerp(a, b, t); p.y -= sag * 4 * t * (1 - t);
                Box(p.x, p.y - .2f, p.z, .17f, .19f, .012f, PrayerColors[i % 5], new Surface(.9f), yaw);
            }
            return this;
        }

        public Mesh Build(string name = "Procedural")
        {
            var mesh = new Mesh { name = name, indexFormat = positions.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(positions); mesh.SetNormals(normals); mesh.SetColors(colors); mesh.SetUVs(1, surfaces); mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

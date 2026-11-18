// Correctly shaped Devanagari for a renderer that cannot shape Indic scripts.
// tools/render-devanagari.ps1 pre-renders every Devanagari phrase found in these
// scripts; this class draws those images inline with ordinary text, in the UI
// and on painted signs in the world. Unknown phrases fall back to the font.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PhewaValley
{
    public static class DevaText
    {
        [Serializable] class Entry { public string text, file; public int width, height; }
        [Serializable] class Index { public Entry[] phrases; }
        static Dictionary<string, Texture2D> images; static Material signMaterial; static Font signFont;
        // Must match the pattern in tools/render-devanagari.ps1.
        static readonly Regex Runs = new Regex(@"[ऀ-ॿ][ऀ-ॿ !…]*[ऀ-ॿ!…]|[ऀ-ॿ]", RegexOptions.Compiled);
        static readonly Regex Digits = new Regex(@"^[०-९]+$");

        public static Texture2D Image(string phrase)
        {
            if (images == null) Load();
            return images.TryGetValue(phrase.Trim(), out var t) ? t : null;
        }

        static void Load()
        {
            images = new Dictionary<string, Texture2D>();
            var dir = Path.Combine(WorldData.Root, "deva"); var index = Path.Combine(dir, "index.json");
            if (!File.Exists(index)) { Debug.LogWarning("[deva] index.json missing: run npm run unity:devanagari"); return; }
            foreach (var e in JsonUtility.FromJson<Index>(File.ReadAllText(index)).phrases)
            {
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, true) { name = e.text, wrapMode = TextureWrapMode.Clamp };
                if (t.LoadImage(File.ReadAllBytes(Path.Combine(dir, e.file)))) images[e.text] = t;
            }
        }

        static IEnumerable<(string text, bool deva)> Split(string text)
        {
            int at = 0;
            foreach (Match m in Runs.Matches(text))
            {
                if (m.Index > at) yield return (text.Substring(at, m.Index - at), false);
                bool image = !Digits.IsMatch(m.Value) && Image(m.Value) != null; yield return (m.Value, image); at = m.Index + m.Length;
            }
            if (at < text.Length) yield return (text.Substring(at), false);
        }

        /// <summary>Draws one line mixing Latin text and shaped Devanagari images. Honors the style's alignment.</summary>
        public static void Label(Rect r, string text, GUIStyle style)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (!Runs.IsMatch(text)) { GUI.Label(r, text, style); return; }
            var line = new GUIStyle(style) { wordWrap = false, alignment = TextAnchor.UpperLeft, padding = new RectOffset(0, 0, 0, 0) };
            float h = (style.fontSize > 0 ? style.fontSize : 16) * 1.45f, total = 0;
            var parts = new List<(string text, bool deva, float w)>();
            foreach (var (t, deva) in Split(text))
            {
                float w = deva ? Image(t).width * h / Image(t).height : line.CalcSize(new GUIContent(t)).x;
                parts.Add((t, deva, w)); total += w;
            }
            var a = style.alignment; float x = r.x + (a == TextAnchor.UpperRight || a == TextAnchor.MiddleRight || a == TextAnchor.LowerRight ? r.width - total : a == TextAnchor.UpperCenter || a == TextAnchor.MiddleCenter || a == TextAnchor.LowerCenter ? (r.width - total) / 2 : 0);
            float y = a == TextAnchor.MiddleLeft || a == TextAnchor.MiddleCenter || a == TextAnchor.MiddleRight ? r.y + (r.height - h) / 2 : r.y;
            var old = GUI.color;
            foreach (var (t, deva, w) in parts)
            {
                if (deva) { GUI.color = style.normal.textColor * old; GUI.DrawTexture(new Rect(x, y - h * .12f, w, h), Image(t)); GUI.color = old; }
                else GUI.Label(new Rect(x, y, w + 2, h), t, line);
                x += w;
            }
        }

        /// <summary>Paints sign text in the world: Latin lines as TextMesh, Devanagari lines as shaped image quads.</summary>
        public static void Paint(Transform parent, Vector3 localPosition, Quaternion localRotation, string text, Color ink, float size)
        {
            if (signFont == null)
            {
                signFont = Font.CreateDynamicFontFromOSFont(new[] { "Arial Black", "Impact", "Arial" }, 64);
                var shader = Shader.Find("PhewaValley/SignText") ?? Shader.Find("GUI/Text Shader");
                signMaterial = new Material(shader) { mainTexture = signFont.material.mainTexture };
                Font.textureRebuilt += f => { if (f == signFont) signMaterial.mainTexture = signFont.material.mainTexture; };
            }
            var lines = text.Split('\n'); float lineHeight = .3f * size / .6f;
            for (int i = 0; i < lines.Length; i++)
            {
                var go = new GameObject("Sign text"); go.transform.SetParent(parent, false);
                go.transform.localPosition = localPosition + localRotation * Vector3.up * ((lines.Length - 1) * .5f - i) * lineHeight * 1.1f; go.transform.localRotation = localRotation;
                var image = Runs.IsMatch(lines[i]) ? Image(lines[i]) : null;
                if (image != null)
                {
                    float h = lineHeight * 1.3f, w = h * image.width / image.height;
                    var mesh = new Mesh { vertices = new[] { new Vector3(-w / 2, -h / 2), new Vector3(w / 2, -h / 2), new Vector3(w / 2, h / 2), new Vector3(-w / 2, h / 2) }, uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) }, colors = new[] { ink, ink, ink, ink }, triangles = new[] { 0, 2, 1, 0, 3, 2 } };
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterial = new Material(signMaterial.shader) { mainTexture = image };
                }
                else
                {
                    var tm = go.AddComponent<TextMesh>(); tm.font = signFont; tm.fontSize = 64; tm.characterSize = .075f * size;
                    tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = ink; tm.text = lines[i];
                    go.GetComponent<MeshRenderer>().sharedMaterial = signMaterial;
                }
            }
        }
    }
}

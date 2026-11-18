// Vintage Nepali UI kit, drawn entirely in code: aged paper, dhaka-weave borders,
// school-notebook pages, crimson ticket buttons, Casio-style LCD and Devanagari type.
using UnityEngine;

namespace PhewaValley
{
    public static class Retro
    {
        public static readonly Color Crimson = new Color(.6f, .07f, .09f), CrimsonDark = new Color(.38f, .03f, .05f), Paper = new Color(.95f, .9f, .78f);
        public static readonly Color Ink = new Color(.18f, .11f, .07f), Gold = new Color(.9f, .68f, .24f), FlagBlue = new Color(.0f, .2f, .55f), Lcd = new Color(.66f, .74f, .52f);

        public static Font Serif, Mono, Deva;
        public static Texture2D PaperTex, DhakaTex, NotebookTex, LcdTex, Solid, CrimsonTex, ShadowTex, MountainTex, KeyTex;
        public static GUIStyle Title, Heading, Body, Small, Label, Eyebrow, Ticket, TicketGold, Key, LcdText, Stamp, Field, Deva20;
        static bool ready;

        public static void Init()
        {
            if (ready) return; ready = true;
            Serif = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Palatino Linotype", "Times New Roman", "Nirmala UI", "Segoe UI Symbol", "Segoe UI Emoji" }, 18);
            Mono = Font.CreateDynamicFontFromOSFont(new[] { "Courier New", "Consolas", "Nirmala UI", "Segoe UI Symbol", "Segoe UI Emoji" }, 18);
            Deva = Font.CreateDynamicFontFromOSFont(new[] { "Nirmala UI", "Mangal", "Kokila", "Arial Unicode MS", "Segoe UI Symbol" }, 18);
            Solid = Tex(1, 1, (x, y) => Color.white);
            PaperTex = Tex(256, 256, (x, y) =>
            {
                float n = Noise(x, y) * .06f + Noise(x / 8, y / 8) * .05f, edge = Mathf.Min(Mathf.Min(x, 255 - x), Mathf.Min(y, 255 - y)) / 40f;
                var c = Paper * (1 - n) * Mathf.Lerp(.86f, 1, Mathf.Clamp01(edge)); c.a = 1; return c;
            });
            // Dhaka weave: stacked triangles and diamonds in crimson, black, saffron, white and green.
            var weave = new[] { new Color(.62f, .06f, .08f), new Color(.06f, .05f, .05f), new Color(.93f, .55f, .1f), new Color(.95f, .92f, .85f), new Color(.1f, .38f, .2f) };
            DhakaTex = Tex(64, 16, (x, y) =>
            {
                int cell = x / 8, lx = x % 8; float d = Mathf.Abs(lx - 3.5f) + Mathf.Abs(y - 7.5f) * .55f;
                if (y < 2 || y > 13) return weave[1];
                if (d < 2.2f) return weave[(cell + 2) % 5];
                if (d < 3.4f) return weave[3];
                return (cell % 2 == 0) ? weave[0] : weave[4];
            }, TextureWrapMode.Repeat, FilterMode.Point);
            NotebookTex = Tex(256, 256, (x, y) =>
            {
                var c = new Color(.97f, .96f, .9f) * (1 - Noise(x, y) * .04f);
                if ((255 - y) % 28 == 0 && y < 230) c = new Color(.55f, .7f, .85f);
                if (x == 34 || x == 37) c = new Color(.85f, .3f, .3f);
                c.a = 1; return c;
            });
            LcdTex = Tex(64, 32, (x, y) => { var c = Lcd * (1 - Noise(x, y) * .05f); if (x < 2 || y < 2 || x > 61 || y > 29) c = new Color(.18f, .2f, .14f); c.a = 1; return c; });
            CrimsonTex = Tex(32, 32, (x, y) => { bool border = x < 2 || y < 2 || x > 29 || y > 29; var c = border ? Gold : Color.Lerp(Crimson, CrimsonDark, y / 32f); c.a = 1; return c; });
            var crimsonHover = Tex(32, 32, (x, y) => { bool border = x < 2 || y < 2 || x > 29 || y > 29; var c = border ? Paper : Color.Lerp(new Color(.75f, .12f, .12f), Crimson, y / 32f); c.a = 1; return c; });
            var goldTex = Tex(32, 32, (x, y) => { bool border = x < 2 || y < 2 || x > 29 || y > 29; var c = border ? CrimsonDark : Color.Lerp(new Color(.98f, .8f, .35f), Gold, y / 32f); c.a = 1; return c; });
            KeyTex = Tex(32, 32, (x, y) => { bool border = x < 2 || y < 2 || x > 29 || y > 29; var c = border ? new Color(.35f, .3f, .24f) : Color.Lerp(new Color(.96f, .93f, .84f), new Color(.8f, .76f, .66f), y / 32f); if (y < 5 && !border) c *= .85f; c.a = 1; return c; });
            ShadowTex = Tex(1, 1, (x, y) => new Color(0, 0, 0, .45f));
            MountainTex = Tex(512, 128, (x, y) =>
            {
                float h = 30 + 50 * Mathf.Abs(Mathf.Sin(x * .021f)) + 28 * Mathf.Abs(Mathf.Sin(x * .057f + 1)) + (x > 300 && x < 340 ? (20 - Mathf.Abs(x - 320)) * 2.6f : 0);
                if (y > h) return new Color(0, 0, 0, 0);
                return y > h - 14 ? new Color(.95f, .95f, .93f, .95f) : new Color(.25f, .12f, .14f, .95f);
            });

            Title = Make(Serif, 44, Ink, FontStyle.Bold); Heading = Make(Serif, 26, Ink, FontStyle.Bold); Body = Make(Serif, 19, Ink); Body.richText = true;
            Small = Make(Serif, 15, new Color(.32f, .24f, .17f)); Label = Make(Serif, 16, Color.white); Label.wordWrap = false;
            Eyebrow = Make(Mono, 14, Crimson, FontStyle.Bold); Eyebrow.wordWrap = false;
            Deva20 = Make(Deva, 20, Crimson, FontStyle.Bold);
            Ticket = new GUIStyle(GUI.skin.button) { font = Serif, fontSize = 18, alignment = TextAnchor.MiddleLeft, wordWrap = true, richText = true, padding = new RectOffset(14, 14, 8, 8), border = new RectOffset(4, 4, 4, 4),
                normal = { background = CrimsonTex, textColor = Paper }, hover = { background = crimsonHover, textColor = Color.white }, active = { background = goldTex, textColor = Ink }, focused = { background = CrimsonTex, textColor = Paper } };
            TicketGold = new GUIStyle(Ticket) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { background = goldTex, textColor = Ink }, hover = { background = crimsonHover, textColor = Color.white } };
            Key = new GUIStyle(GUI.skin.label) { font = Mono, fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, border = new RectOffset(4, 4, 4, 4), normal = { background = KeyTex, textColor = Ink } };
            LcdText = new GUIStyle(GUI.skin.label) { font = Mono, fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(.12f, .14f, .1f) } };
            Stamp = Make(Serif, 30, Crimson, FontStyle.Bold); Stamp.alignment = TextAnchor.MiddleRight;
            Field = new GUIStyle(GUI.skin.textField) { font = Serif, fontSize = 18, normal = { background = Solid, textColor = Ink }, focused = { background = Solid, textColor = Ink }, padding = new RectOffset(8, 8, 6, 6) };
        }

        static GUIStyle Make(Font f, int size, Color c, FontStyle style = FontStyle.Normal) => new GUIStyle(GUI.skin.label) { font = f, fontSize = size, fontStyle = style, wordWrap = true, normal = { textColor = c } };

        static float Noise(int x, int y) { unchecked { uint h = (uint)(x * 374761393 + y * 668265263); h = (h ^ (h >> 13)) * 1274126177; return (h & 0xffff) / 65535f; } }

        static Texture2D Tex(int w, int h, System.Func<int, int, Color> f, TextureWrapMode wrap = TextureWrapMode.Clamp, FilterMode filter = FilterMode.Bilinear)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = wrap, filterMode = filter };
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) t.SetPixel(x, y, f(x, y));
            t.Apply(); return t;
        }

        public static void Fill(Rect r, Color c) { var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Solid); GUI.color = old; }

        /// <summary>A horizontal dhaka-weave band.</summary>
        public static void Dhaka(Rect r) => GUI.DrawTextureWithTexCoords(r, DhakaTex, new Rect(0, 0, r.width / (r.height * 4), 1));

        /// <summary>Aged paper card with a crimson frame and dhaka bands top and bottom.</summary>
        public static void PaperPanel(Rect r, bool bands = true)
        {
            GUI.DrawTexture(new Rect(r.x + 6, r.y + 8, r.width, r.height), ShadowTex);
            Fill(r, CrimsonDark); GUI.DrawTexture(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), PaperTex);
            if (bands) { Dhaka(new Rect(r.x + 3, r.y + 3, r.width - 6, 12)); Dhaka(new Rect(r.x + 3, r.yMax - 15, r.width - 6, 12)); }
        }

        /// <summary>Lined school-notebook page (copy) with a red margin.</summary>
        public static void Notebook(Rect r)
        {
            GUI.DrawTexture(new Rect(r.x + 5, r.y + 7, r.width, r.height), ShadowTex);
            GUI.DrawTextureWithTexCoords(r, NotebookTex, new Rect(0, 0, r.width / 256f, r.height / 256f));
            Fill(new Rect(r.x, r.y, r.width, 6), Crimson);
        }

        public static void LcdPanel(Rect r, string text) { GUI.DrawTexture(r, LcdTex); GUI.Label(r, text, LcdText); }

        public static void KeyCap(Rect r, string key) => GUI.Label(r, key, Key);

        static readonly char[] Digits = { '०', '१', '२', '३', '४', '५', '६', '७', '८', '९' };
        public static string Devanagari(int n) { var s = n.ToString(); var c = s.ToCharArray(); for (int i = 0; i < c.Length; i++) if (char.IsDigit(c[i])) c[i] = Digits[c[i] - '0']; return new string(c); }

        /// <summary>Horizontal meter with an optional highlighted zone.</summary>
        public static void Meter(Rect r, float value, float zoneMin = -1, float zoneMax = -1, Color? fill = null)
        {
            Fill(r, Ink); var inner = new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4); Fill(inner, new Color(.85f, .8f, .68f));
            if (zoneMin >= 0) Fill(new Rect(inner.x + inner.width * zoneMin, inner.y, inner.width * (zoneMax - zoneMin), inner.height), new Color(.3f, .62f, .3f));
            Fill(new Rect(inner.x, inner.y + inner.height * .3f, inner.width * Mathf.Clamp01(value), inner.height * .4f), fill ?? Crimson);
            Fill(new Rect(inner.x + inner.width * Mathf.Clamp01(value) - 2, inner.y - 3, 4, inner.height + 6), Ink);
        }
    }
}

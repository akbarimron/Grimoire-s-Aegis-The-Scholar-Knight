using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrimoireAegis.UI
{
    /// <summary>
    /// BMFont (text .fnt) bitmap font: the 5x7 pixel font shared with Lost in Multiverse RPG.
    /// Supports inline colour tags: "{#ffd86a}gold text{/}".
    /// </summary>
    public class PixelFont
    {
        private struct Glyph { public Rect uv; public int w, h, xoff, yoff, adv; }

        private readonly Dictionary<int, Glyph> _glyphs = new Dictionary<int, Glyph>();
        public Texture2D Atlas { get; private set; }
        public int LineHeight { get; private set; } = 12;

        public static PixelFont Load(string fntResource, string pngResource)
        {
            var desc = Resources.Load<TextAsset>(fntResource);
            var tex = Resources.Load<Texture2D>(pngResource);
            if (desc == null || tex == null) return null;
            tex.filterMode = FilterMode.Point;
            var f = new PixelFont { Atlas = tex };
            foreach (string raw in desc.text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.StartsWith("common "))
                {
                    f.LineHeight = Val(line, "lineHeight", 12);
                }
                else if (line.StartsWith("char "))
                {
                    int id = Val(line, "id", 0), x = Val(line, "x", 0), y = Val(line, "y", 0);
                    int w = Val(line, "width", 0), h = Val(line, "height", 0);
                    // Unity textures are bottom-up; BMFont coordinates are top-down.
                    var uv = new Rect((float)x / tex.width, 1f - (float)(y + h) / tex.height, (float)w / tex.width, (float)h / tex.height);
                    f._glyphs[id] = new Glyph { uv = uv, w = w, h = h, xoff = Val(line, "xoffset", 0), yoff = Val(line, "yoffset", 0), adv = Val(line, "xadvance", w + 1) };
                }
            }
            return f;
        }

        private static int Val(string line, string key, int def)
        {
            int i = line.IndexOf(" " + key + "=", StringComparison.Ordinal);
            if (i < 0) return def;
            i += key.Length + 2;
            int j = i;
            while (j < line.Length && (char.IsDigit(line[j]) || line[j] == '-')) j++;
            return int.TryParse(line.Substring(i, j - i), out int v) ? v : def;
        }

        private static char Fold(char c)
        {
            switch (c)
            {
                case '—': case '–': return '-';     // em / en dash
                case '‘': case '’': return '\'';
                case '“': case '”': return '"';
                case '≈': return '~';
                case '≤': return '<';
                case '≥': return '>';
                case ' ': return ' ';
                default: return c;
            }
        }

        private bool TryGlyph(char c, out Glyph g)
        {
            c = Fold(c);
            if (_glyphs.TryGetValue(c, out g)) return true;
            // fold accents / unknown characters to a safe fallback
            if (_glyphs.TryGetValue(char.ToUpperInvariant(c), out g)) return true;
            return _glyphs.TryGetValue('?', out g);
        }

        public int Advance(char c) => c == ' ' ? 3 : (TryGlyph(c, out var g) ? g.adv : 4);

        /// <summary>Width in virtual pixels, colour tags excluded.</summary>
        public int Measure(string s, int scale = 1)
        {
            int w = 0, best = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '{' && TrySkipTag(s, ref i)) continue;
                if (s[i] == '\n') { best = Mathf.Max(best, w); w = 0; continue; }
                w += Advance(s[i]);
            }
            return Mathf.Max(best, w) * scale;
        }

        private static bool TrySkipTag(string s, ref int i)
        {
            if (i + 2 < s.Length && s[i + 1] == '/' && s[i + 2] == '}') { i += 2; return true; }
            if (i + 8 < s.Length && s[i + 1] == '#' && s[i + 8] == '}') { i += 8; return true; }
            return false;
        }

        public void Draw(float x, float y, string s, Color col, int scale = 1)
        {
            if (Atlas == null || string.IsNullOrEmpty(s)) return;
            var stack = new Stack<Color>();
            Color cur = col;
            float cx = x, cy = y;
            Color prev = GUI.color;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '{')
                {
                    if (i + 2 < s.Length && s[i + 1] == '/' && s[i + 2] == '}') { cur = stack.Count > 0 ? stack.Pop() : col; i += 2; continue; }
                    if (i + 8 < s.Length && s[i + 1] == '#' && s[i + 8] == '}' && ColorUtility.TryParseHtmlString(s.Substring(i + 1, 7), out Color tc))
                    {
                        stack.Push(cur);
                        cur = new Color(tc.r, tc.g, tc.b, col.a);
                        i += 8;
                        continue;
                    }
                }
                if (c == '\n') { cx = x; cy += LineHeight * scale; continue; }
                if (c == ' ') { cx += 3 * scale; continue; }
                if (!TryGlyph(c, out Glyph g)) { cx += 4 * scale; continue; }
                GUI.color = cur;
                GUI.DrawTextureWithTexCoords(new Rect(cx + g.xoff * scale, cy + g.yoff * scale, g.w * scale, g.h * scale), Atlas, g.uv, true);
                cx += g.adv * scale;
            }
            GUI.color = prev;
        }

        /// <summary>Greedy word wrap that keeps colour tags attached to their words.</summary>
        public List<string> Wrap(string s, int maxW, int scale = 1)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(s)) return lines;
            foreach (string para in s.Split('\n'))
            {
                string line = "";
                foreach (string word in para.Split(' '))
                {
                    string test = line.Length == 0 ? word : line + " " + word;
                    if (Measure(test, scale) > maxW && line.Length > 0)
                    {
                        lines.Add(line);
                        line = word;
                    }
                    else line = test;
                }
                lines.Add(line);
            }
            return lines;
        }
    }

    /// <summary>
    /// Immediate-mode pixel UI on a fixed 640x360 canvas, scaled by an integer factor and
    /// letter-boxed (same approach as Lost in Multiverse RPG's 640x360 viewport).
    /// Coordinates passed to every call are virtual pixels.
    /// </summary>
    public static class PixelUI
    {
        public const int W = 640, H = 360;

        public static float Scale { get; private set; } = 2f;
        public static Vector2 Offset { get; private set; }
        public static PixelFont Font { get; private set; }
        public static Func<string, Texture2D> Tex;   // resource loader injected by the controller
        public static Action<string> Sfx;            // hover / click sounds

        private static Texture2D _white;
        private static string _tooltip;
        private static Vector2 _tooltipPos;
        private static int _hoverId, _lastHoverId;

        public static readonly Color Ink = new Color32(0xec, 0xe0, 0xc8, 0xff);
        public static readonly Color Dim = new Color32(0x8a, 0x8e, 0xa8, 0xff);
        public static readonly Color Gold = new Color32(0xff, 0xd8, 0x6a, 0xff);
        public static readonly Color Red = new Color32(0xf0, 0x5a, 0x50, 0xff);
        public static readonly Color Green = new Color32(0x8a, 0xe0, 0x5a, 0xff);
        public static readonly Color Cyan = new Color32(0x7f, 0xd8, 0xff, 0xff);
        public static readonly Color Purple = new Color32(0xd0, 0xa0, 0xff, 0xff);
        public static readonly Color ParchInk = new Color32(0x2a, 0x1a, 0x10, 0xff);
        public static readonly Color ParchDim = new Color32(0x6a, 0x52, 0x3a, 0xff);

        public static bool IsRepaint => Event.current != null && Event.current.type == EventType.Repaint;
        public static Vector2 Mouse => Event.current != null ? Event.current.mousePosition : Vector2.zero;

        public static void Begin()
        {
            if (_white == null)
            {
                _white = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                _white.SetPixel(0, 0, Color.white);
                _white.Apply();
            }
            if (Font == null) Font = PixelFont.Load("Gen/fonts/pixel_fnt", "Gen/fonts/pixel");

            float sx = Screen.width / (float)W, sy = Screen.height / (float)H;
            float s = Mathf.Min(sx, sy);
            Scale = s >= 1f ? Mathf.Floor(s) : s;   // integer scale keeps pixels crisp
            Offset = new Vector2(Mathf.Floor((Screen.width - W * Scale) * 0.5f), Mathf.Floor((Screen.height - H * Scale) * 0.5f));

            GUI.matrix = Matrix4x4.identity;
            if (IsRepaint)
            {
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _white);
                GUI.color = Color.white;
            }
            GUI.matrix = Matrix4x4.TRS(Offset, Quaternion.identity, new Vector3(Scale, Scale, 1f));
            GUI.BeginClip(new Rect(0, 0, W, H));
            _tooltip = null;
            if (Event.current.type == EventType.Layout) { _lastHoverId = _hoverId; _hoverId = 0; }
        }

        public static void End()
        {
            if (_tooltip != null) DrawTooltip();
            GUI.EndClip();
            GUI.matrix = Matrix4x4.identity;
        }

        // ------------------------------------------------------------------ primitives
        public static void Fill(Rect r, Color c)
        {
            if (!IsRepaint) return;
            Color p = GUI.color; GUI.color = c;
            GUI.DrawTexture(r, _white);
            GUI.color = p;
        }

        public static void Frame(Rect r, Color c, int t = 1)
        {
            Fill(new Rect(r.x, r.y, r.width, t), c);
            Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
            Fill(new Rect(r.x, r.y, t, r.height), c);
            Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        public static Texture2D T(string key) => Tex != null ? Tex(key) : null;

        public static void NineSlice(Rect r, Texture2D t, int b, Color? tint = null)
        {
            if (!IsRepaint || t == null) return;
            Color p = GUI.color; GUI.color = tint ?? Color.white;
            float tw = t.width, th = t.height;
            float[] xs = { r.x, r.x + b, r.xMax - b, r.xMax };
            float[] ys = { r.y, r.y + b, r.yMax - b, r.yMax };
            float[] us = { 0, b / tw, 1 - b / tw, 1 };
            float[] vs = { 1, 1 - b / th, b / th, 0 }; // top-down -> bottom-up
            for (int iy = 0; iy < 3; iy++)
            for (int ix = 0; ix < 3; ix++)
            {
                var dst = new Rect(xs[ix], ys[iy], xs[ix + 1] - xs[ix], ys[iy + 1] - ys[iy]);
                if (dst.width <= 0 || dst.height <= 0) continue;
                var uv = new Rect(us[ix], vs[iy + 1], us[ix + 1] - us[ix], vs[iy] - vs[iy + 1]);
                GUI.DrawTextureWithTexCoords(dst, t, uv, true);
            }
            GUI.color = p;
        }

        /// <summary>kind: panel | dark | gold | red | teal | parchment</summary>
        public static void Panel(Rect r, string kind = "panel", float alpha = 1f)
        {
            string key = kind == "panel" ? "ui2/panel" : (kind == "parchment" ? "ui2/parchment" : "ui2/panel_" + kind);
            NineSlice(r, T(key), kind == "parchment" ? 8 : 6, new Color(1, 1, 1, alpha));
        }

        public static void Shade(Rect r, float a) => Fill(r, new Color(0.02f, 0.02f, 0.05f, a));

        // ------------------------------------------------------------------ text
        public static void Text(float x, float y, string s, Color col, int scale = 1, bool shadow = true)
        {
            if (!IsRepaint || Font == null) return;
            if (shadow) Font.Draw(x + scale, y + scale, StripTags(s), new Color(0, 0, 0, 0.78f * col.a), scale);
            Font.Draw(x, y, s, col, scale);
        }

        public static void TextCenter(Rect r, string s, Color col, int scale = 1, bool shadow = true)
        {
            if (Font == null) return;
            int w = Font.Measure(s, scale);
            int lines = s.Split('\n').Length;
            float h = lines * Font.LineHeight * scale - 2 * scale;
            Text(Mathf.Round(r.x + (r.width - w) * 0.5f), Mathf.Round(r.y + (r.height - h) * 0.5f), s, col, scale, shadow);
        }

        public static void TextRight(float xRight, float y, string s, Color col, int scale = 1, bool shadow = true)
        {
            if (Font == null) return;
            Text(xRight - Font.Measure(s, scale), y, s, col, scale, shadow);
        }

        /// <summary>Word-wrapped text; returns the height used.</summary>
        public static float TextWrap(Rect r, string s, Color col, int scale = 1, bool shadow = true, int maxLines = 999)
        {
            if (Font == null || string.IsNullOrEmpty(s)) return 0;
            var lines = Font.Wrap(s, Mathf.FloorToInt(r.width), scale);
            float y = r.y;
            int n = 0;
            foreach (var l in lines)
            {
                if (n++ >= maxLines) break;
                Text(r.x, y, l, col, scale, shadow);
                y += Font.LineHeight * scale;
            }
            return y - r.y;
        }

        public static float WrapHeight(string s, float width, int scale = 1)
        {
            if (Font == null || string.IsNullOrEmpty(s)) return 0;
            return Font.Wrap(s, Mathf.FloorToInt(width), scale).Count * Font.LineHeight * scale;
        }

        public static int Measure(string s, int scale = 1) => Font != null ? Font.Measure(s, scale) : s.Length * 5;

        public static string StripTags(string s)
        {
            if (s.IndexOf('{') < 0) return s;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '{')
                {
                    if (i + 2 < s.Length && s[i + 1] == '/' && s[i + 2] == '}') { i += 2; continue; }
                    if (i + 8 < s.Length && s[i + 1] == '#' && s[i + 8] == '}') { i += 8; continue; }
                }
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------ sprites & icons
        public static void Image(Rect r, Texture2D t, Color? tint = null)
        {
            if (!IsRepaint || t == null) return;
            Color p = GUI.color; GUI.color = tint ?? Color.white;
            GUI.DrawTexture(r, t, ScaleMode.StretchToFill, true);
            GUI.color = p;
        }

        public static void Icon(string key, float x, float y, int scale = 1, Color? tint = null)
        {
            var t = T(key);
            if (t == null) return;
            Image(new Rect(x, y, t.width * scale, t.height * scale), t, tint);
        }

        /// <summary>Frame from a horizontal strip of square frames.</summary>
        public static void Frame(Rect r, Texture2D strip, int frame, Color? tint = null)
        {
            if (!IsRepaint || strip == null) return;
            int n = Mathf.Max(1, strip.width / Mathf.Max(1, strip.height));
            frame = Mathf.Clamp(frame, 0, n - 1);
            Color p = GUI.color; GUI.color = tint ?? Color.white;
            GUI.DrawTextureWithTexCoords(r, strip, new Rect((float)frame / n, 0, 1f / n, 1), true);
            GUI.color = p;
        }

        public static void Strip(float x, float y, string key, string animKey, float time, int scale = 1, Color? tint = null, bool loop = true, int forceFrame = -1)
        {
            var t = T(key);
            if (t == null) return;
            int f = forceFrame >= 0 ? forceFrame : AnimLibrary.FrameAt(animKey, time, loop);
            Frame(new Rect(x, y, t.height * scale, t.height * scale), t, f, tint);
        }

        // ------------------------------------------------------------------ widgets
        public static bool Hover(Rect r) => r.Contains(Mouse);

        /// <summary>
        /// Pixel button. style: normal | gold | danger. selected = keyboard focus.
        /// Returns true on click. Hotkey hint is drawn on the right when given.
        /// </summary>
        public static bool Button(Rect r, string label, string icon = null, bool enabled = true, bool selected = false,
                                  string style = "normal", string hotkey = null, string tooltip = null, int align = 0)
        {
            bool hover = enabled && Hover(r);
            int id = Mathf.RoundToInt(r.x * 7 + r.y * 131 + r.width * 17);
            if (hover)
            {
                _hoverId = id;
                if (_lastHoverId != id && Event.current.type == EventType.Repaint) { Sfx?.Invoke("move"); _lastHoverId = id; }
                if (tooltip != null) { _tooltip = tooltip; _tooltipPos = new Vector2(r.x, r.yMax + 2); }
            }
            var mouseDev = UnityEngine.InputSystem.Mouse.current;
            bool pressed = hover && mouseDev != null && mouseDev.leftButton.isPressed;
            string tex = !enabled ? "ui2/btn_disabled" : pressed ? "ui2/btn_pressed" : (hover || selected) ? "ui2/btn_hover" : (style == "gold" ? "ui2/btn_gold" : style == "danger" ? "ui2/btn_danger" : "ui2/btn_normal");
            NineSlice(r, T(tex), 4);

            Color col = !enabled ? new Color(0.36f, 0.34f, 0.42f) : (hover || selected) ? Gold : Ink;
            float tx = r.x + 6;
            if (icon != null)
            {
                var it = T(icon);
                if (it != null)
                {
                    Image(new Rect(r.x + 4, Mathf.Round(r.y + (r.height - it.height) * 0.5f), it.width, it.height), it, enabled ? Color.white : new Color(1, 1, 1, 0.35f));
                    tx = r.x + 6 + it.width + 2;
                }
            }
            float ty = Mathf.Round(r.y + (r.height - 8) * 0.5f) - 1;
            if (align == 1) TextCenter(new Rect(tx - 2, r.y, r.xMax - tx - 2, r.height), label, col);
            else Text(tx, ty, label, col);
            // hotkey hint only when it does not collide with the label
            if (hotkey != null && tx + Measure(StripTags(label)) + Measure(hotkey) + 10 <= r.xMax)
                TextRight(r.xMax - 5, ty, hotkey, enabled ? Dim : new Color(0.3f, 0.3f, 0.36f));

            if (enabled && Event.current.type == EventType.MouseUp && Event.current.button == 0 && r.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                Sfx?.Invoke("select");
                return true;
            }
            return false;
        }

        /// <summary>Selectable list row (used for menus, lists): highlight + cursor arrow.</summary>
        public static bool Row(Rect r, string label, bool selected, string icon = null, bool enabled = true, string right = null, Color? rightCol = null)
        {
            bool hover = enabled && Hover(r);
            if (selected || hover)
            {
                Fill(r, new Color(1f, 0.85f, 0.4f, selected ? 0.13f : 0.07f));
                Frame(r, selected ? Gold : new Color(1f, 0.85f, 0.4f, 0.4f));
            }
            float x = r.x + 4;
            if (selected) { Icon("ui2/cursor", r.x - 7, r.y + (r.height - 7) * 0.5f); }
            if (icon != null)
            {
                var it = T(icon);
                if (it != null) { Image(new Rect(x, Mathf.Round(r.y + (r.height - it.height) * 0.5f), it.width, it.height), it, enabled ? Color.white : new Color(1, 1, 1, 0.35f)); x += it.width + 3; }
            }
            Color col = !enabled ? new Color(0.4f, 0.38f, 0.46f) : (selected ? Gold : Ink);
            Text(x, Mathf.Round(r.y + (r.height - 8) * 0.5f) - 1, label, col);
            if (right != null) TextRight(r.xMax - 4, Mathf.Round(r.y + (r.height - 8) * 0.5f) - 1, right, enabled ? (rightCol ?? Dim) : new Color(0.4f, 0.38f, 0.46f));
            if (enabled && Event.current.type == EventType.MouseUp && Event.current.button == 0 && r.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                Sfx?.Invoke("select");
                return true;
            }
            return false;
        }

        /// <summary>Bar with a dark frame, a lagging "ghost" segment and a 1 px highlight.</summary>
        public static void Bar(Rect r, float v, Color col, float ghost = -1f)
        {
            NineSlice(r, T("ui2/bar_frame"), 2);
            Rect inner = new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2);
            v = Mathf.Clamp01(v);
            if (ghost > v) Fill(new Rect(inner.x, inner.y, Mathf.Round(inner.width * Mathf.Clamp01(ghost)), inner.height), new Color(1f, 0.95f, 0.8f, 0.8f));
            float w = Mathf.Round(inner.width * v);
            Fill(new Rect(inner.x, inner.y, w, inner.height), col);
            if (inner.height >= 3) Fill(new Rect(inner.x, inner.y, w, 1), Color.Lerp(col, Color.white, 0.45f));
        }

        public static void Tooltip(string text) { _tooltip = text; _tooltipPos = Mouse + new Vector2(8, 10); }

        private static void DrawTooltip()
        {
            float w = Mathf.Min(220, Measure(StripTags(_tooltip)) + 10);
            float h = WrapHeight(_tooltip, w - 10) + 8;
            float x = Mathf.Clamp(_tooltipPos.x, 2, W - w - 2), y = Mathf.Clamp(_tooltipPos.y, 2, H - h - 2);
            Panel(new Rect(x, y, w, h), "dark");
            TextWrap(new Rect(x + 5, y + 4, w - 10, h), _tooltip, Ink);
        }

        public static bool ScrollWheel(Rect r, out float delta)
        {
            delta = 0;
            var e = Event.current;
            if (e.type == EventType.ScrollWheel && r.Contains(e.mousePosition))
            {
                delta = e.delta.y;
                e.Use();
                return true;
            }
            return false;
        }

        /// <summary>Clips drawing to r (virtual coords). Pair with EndClip.</summary>
        public static void BeginClip(Rect r) => GUI.BeginClip(r);
        public static void EndClip() => GUI.EndClip();
    }
}

// Cine Quest — Layout JSON serialize/deserialize without UnityEngine.JsonUtility.
// Uses minimal JSON for ImageParameterState + panel/scope poses so tests can round-trip shipped types.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CineQuest.Core
{
    [Serializable]
    public sealed class LayoutDocument
    {
        public int version = 1;
        public string name = "Default";
        public ImageParameterState image;
        public string environment = "Passthrough";
        public string qualityMode = "Balanced";
        public bool falseColor;
        public bool audioMuted = true;
        public PanelPose mainPanel;
        public List<ScopePose> scopes = new List<ScopePose>();
    }

    [Serializable]
    public sealed class PanelPose
    {
        public string id = "main";
        public float px, py, pz;
        public float qx, qy, qz, qw = 1f;
        public float sx = 1f, sy = 1f, sz = 1f;
    }

    [Serializable]
    public sealed class ScopePose
    {
        public string type;
        public bool enabled;
        public float opacity = 0.95f;
        public float px, py, pz;
        public float qx, qy, qz, qw = 1f;
        public float sx = 1f, sy = 1f, sz = 1f;
    }

    /// <summary>
    /// Deterministic JSON for layout round-trips. Not a full JSON library — covers Cine Quest fields only.
    /// </summary>
    public static class LayoutSerializer
    {
        public static string Serialize(LayoutDocument doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            var sb = new StringBuilder(512);
            sb.Append('{');
            WriteInt(sb, "version", doc.version); sb.Append(',');
            WriteString(sb, "name", doc.name ?? ""); sb.Append(',');
            sb.Append("\"image\":");
            WriteImage(sb, doc.image ?? ImageParameterState.CreateNeutral());
            sb.Append(',');
            WriteString(sb, "environment", doc.environment ?? "Passthrough"); sb.Append(',');
            WriteString(sb, "qualityMode", doc.qualityMode ?? "Balanced"); sb.Append(',');
            WriteBool(sb, "falseColor", doc.falseColor); sb.Append(',');
            WriteBool(sb, "audioMuted", doc.audioMuted); sb.Append(',');
            sb.Append("\"mainPanel\":");
            WritePanel(sb, doc.mainPanel ?? new PanelPose());
            sb.Append(',');
            sb.Append("\"scopes\":[");
            if (doc.scopes != null)
            {
                for (int i = 0; i < doc.scopes.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    WriteScope(sb, doc.scopes[i]);
                }
            }
            sb.Append("]}");
            return sb.ToString();
        }

        public static LayoutDocument Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new ArgumentException("json empty");
            var doc = new LayoutDocument();
            doc.version = ReadInt(json, "version", 1);
            doc.name = ReadString(json, "name", "Default");
            doc.environment = ReadString(json, "environment", "Passthrough");
            doc.qualityMode = ReadString(json, "qualityMode", "Balanced");
            doc.falseColor = ReadBool(json, "falseColor", false);
            doc.audioMuted = ReadBool(json, "audioMuted", true);
            doc.image = ReadImage(json);
            doc.mainPanel = ReadPanel(json);
            doc.scopes = ReadScopes(json);
            return doc;
        }

        static void WriteImage(StringBuilder sb, ImageParameterState p)
        {
            sb.Append('{');
            WriteBool(sb, "locked", p.locked); sb.Append(',');
            WriteBool(sb, "bypass", p.bypass); sb.Append(',');
            WriteInt(sb, "colorSpace", (int)p.colorSpace); sb.Append(',');
            WriteFloat(sb, "brightness", p.brightness); sb.Append(',');
            WriteFloat(sb, "contrast", p.contrast); sb.Append(',');
            WriteFloat(sb, "gamma", p.gamma); sb.Append(',');
            WriteFloat(sb, "saturation", p.saturation); sb.Append(',');
            WriteFloat(sb, "temperature", p.temperature); sb.Append(',');
            WriteFloat(sb, "tint", p.tint); sb.Append(',');
            WriteFloat(sb, "lift", p.lift);
            sb.Append('}');
        }

        static ImageParameterState ReadImage(string json)
        {
            // Prefer nested "image":{...} block
            var block = ExtractObject(json, "image");
            var src = block ?? json;
            var p = ImageParameterState.CreateNeutral();
            p.locked = ReadBool(src, "locked", false);
            p.bypass = ReadBool(src, "bypass", false);
            p.colorSpace = (ColorSpaceMode)ReadInt(src, "colorSpace", 0);
            p.brightness = ReadFloat(src, "brightness", 0f);
            p.contrast = ReadFloat(src, "contrast", 1f);
            p.gamma = ReadFloat(src, "gamma", 1f);
            p.saturation = ReadFloat(src, "saturation", 1f);
            p.temperature = ReadFloat(src, "temperature", 0f);
            p.tint = ReadFloat(src, "tint", 0f);
            p.lift = ReadFloat(src, "lift", 0f);
            return p;
        }

        static void WritePanel(StringBuilder sb, PanelPose p)
        {
            sb.Append('{');
            WriteString(sb, "id", p.id ?? "main"); sb.Append(',');
            WriteFloat(sb, "px", p.px); sb.Append(',');
            WriteFloat(sb, "py", p.py); sb.Append(',');
            WriteFloat(sb, "pz", p.pz); sb.Append(',');
            WriteFloat(sb, "qx", p.qx); sb.Append(',');
            WriteFloat(sb, "qy", p.qy); sb.Append(',');
            WriteFloat(sb, "qz", p.qz); sb.Append(',');
            WriteFloat(sb, "qw", p.qw); sb.Append(',');
            WriteFloat(sb, "sx", p.sx); sb.Append(',');
            WriteFloat(sb, "sy", p.sy); sb.Append(',');
            WriteFloat(sb, "sz", p.sz);
            sb.Append('}');
        }

        static PanelPose ReadPanel(string json)
        {
            var block = ExtractObject(json, "mainPanel") ?? json;
            return new PanelPose
            {
                id = ReadString(block, "id", "main"),
                px = ReadFloat(block, "px", 0),
                py = ReadFloat(block, "py", 0),
                pz = ReadFloat(block, "pz", 0),
                qx = ReadFloat(block, "qx", 0),
                qy = ReadFloat(block, "qy", 0),
                qz = ReadFloat(block, "qz", 0),
                qw = ReadFloat(block, "qw", 1),
                sx = ReadFloat(block, "sx", 1),
                sy = ReadFloat(block, "sy", 1),
                sz = ReadFloat(block, "sz", 1)
            };
        }

        static void WriteScope(StringBuilder sb, ScopePose s)
        {
            sb.Append('{');
            WriteString(sb, "type", s.type ?? ""); sb.Append(',');
            WriteBool(sb, "enabled", s.enabled); sb.Append(',');
            WriteFloat(sb, "opacity", s.opacity); sb.Append(',');
            WriteFloat(sb, "px", s.px); sb.Append(',');
            WriteFloat(sb, "py", s.py); sb.Append(',');
            WriteFloat(sb, "pz", s.pz); sb.Append(',');
            WriteFloat(sb, "qx", s.qx); sb.Append(',');
            WriteFloat(sb, "qy", s.qy); sb.Append(',');
            WriteFloat(sb, "qz", s.qz); sb.Append(',');
            WriteFloat(sb, "qw", s.qw); sb.Append(',');
            WriteFloat(sb, "sx", s.sx); sb.Append(',');
            WriteFloat(sb, "sy", s.sy); sb.Append(',');
            WriteFloat(sb, "sz", s.sz);
            sb.Append('}');
        }

        static List<ScopePose> ReadScopes(string json)
        {
            var list = new List<ScopePose>();
            int key = json.IndexOf("\"scopes\"", StringComparison.Ordinal);
            if (key < 0) return list;
            int arr = json.IndexOf('[', key);
            int end = json.IndexOf(']', arr);
            if (arr < 0 || end < 0) return list;
            string inner = json.Substring(arr + 1, end - arr - 1).Trim();
            if (string.IsNullOrEmpty(inner)) return list;

            // Split top-level objects
            int depth = 0;
            int start = 0;
            for (int i = 0; i < inner.Length; i++)
            {
                char c = inner[i];
                if (c == '{') { if (depth == 0) start = i; depth++; }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        string obj = inner.Substring(start, i - start + 1);
                        list.Add(new ScopePose
                        {
                            type = ReadString(obj, "type", ""),
                            enabled = ReadBool(obj, "enabled", false),
                            opacity = ReadFloat(obj, "opacity", 0.95f),
                            px = ReadFloat(obj, "px", 0),
                            py = ReadFloat(obj, "py", 0),
                            pz = ReadFloat(obj, "pz", 0),
                            qx = ReadFloat(obj, "qx", 0),
                            qy = ReadFloat(obj, "qy", 0),
                            qz = ReadFloat(obj, "qz", 0),
                            qw = ReadFloat(obj, "qw", 1),
                            sx = ReadFloat(obj, "sx", 1),
                            sy = ReadFloat(obj, "sy", 1),
                            sz = ReadFloat(obj, "sz", 1)
                        });
                    }
                }
            }
            return list;
        }

        static string ExtractObject(string json, string key)
        {
            string needle = "\"" + key + "\"";
            int k = json.IndexOf(needle, StringComparison.Ordinal);
            if (k < 0) return null;
            int brace = json.IndexOf('{', k);
            if (brace < 0) return null;
            int depth = 0;
            for (int i = brace; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}')
                {
                    depth--;
                    if (depth == 0) return json.Substring(brace, i - brace + 1);
                }
            }
            return null;
        }

        static void WriteString(StringBuilder sb, string key, string value)
        {
            sb.Append('"').Append(key).Append("\":\"").Append(Escape(value)).Append('"');
        }

        static void WriteBool(StringBuilder sb, string key, bool value)
        {
            sb.Append('"').Append(key).Append("\":").Append(value ? "true" : "false");
        }

        static void WriteInt(StringBuilder sb, string key, int value)
        {
            sb.Append('"').Append(key).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture));
        }

        static void WriteFloat(StringBuilder sb, string key, float value)
        {
            sb.Append('"').Append(key).Append("\":").Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        static string Escape(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

        static int ReadInt(string json, string key, int def)
        {
            var s = ReadRaw(json, key);
            if (s == null) return def;
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : def;
        }

        static float ReadFloat(string json, string key, float def)
        {
            var s = ReadRaw(json, key);
            if (s == null) return def;
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;
        }

        static bool ReadBool(string json, string key, bool def)
        {
            var s = ReadRaw(json, key);
            if (s == null) return def;
            if (s == "true") return true;
            if (s == "false") return false;
            return def;
        }

        static string ReadString(string json, string key, string def)
        {
            string needle = "\"" + key + "\"";
            int k = json.IndexOf(needle, StringComparison.Ordinal);
            if (k < 0) return def;
            int colon = json.IndexOf(':', k + needle.Length);
            if (colon < 0) return def;
            int q1 = json.IndexOf('"', colon + 1);
            if (q1 < 0) return def;
            int q2 = q1 + 1;
            while (q2 < json.Length)
            {
                if (json[q2] == '"' && json[q2 - 1] != '\\') break;
                q2++;
            }
            if (q2 >= json.Length) return def;
            return json.Substring(q1 + 1, q2 - q1 - 1).Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        static string ReadRaw(string json, string key)
        {
            string needle = "\"" + key + "\"";
            int k = json.IndexOf(needle, StringComparison.Ordinal);
            if (k < 0) return null;
            int colon = json.IndexOf(':', k + needle.Length);
            if (colon < 0) return null;
            int i = colon + 1;
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
            if (i >= json.Length) return null;
            if (json[i] == '"') return null; // use ReadString
            int start = i;
            while (i < json.Length && json[i] != ',' && json[i] != '}' && json[i] != ']') i++;
            return json.Substring(start, i - start).Trim();
        }
    }
}

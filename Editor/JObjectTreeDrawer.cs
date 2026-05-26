#if UNITY_EDITOR
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace BB.Framework
{
    /// <summary>
    /// Reusable IMGUI renderer for a Newtonsoft JToken tree. Draws objects/arrays as foldouts and
    /// leaf values as editable fields (toggle/int/float/text). When <see cref="ReadOnly"/> is false,
    /// edits mutate the JToken in place and set <see cref="Dirty"/>. Used by the Inspector window.
    /// </summary>
    public class JObjectTreeDrawer
    {
        private readonly HashSet<string> m_OpenPaths = new HashSet<string>();
        public bool ReadOnly { get; set; } = true;
        public bool Dirty { get; private set; }

        public void ClearDirty() => Dirty = false;

        public void Draw(JToken root)
        {
            if (root == null)
            {
                EditorGUILayout.LabelField("(null)");
                return;
            }
            DrawToken(root, "$", "$");
        }

        private void DrawToken(JToken token, string label, string path)
        {
            switch (token.Type)
            {
                case JTokenType.Object: DrawObject((JObject)token, label, path); break;
                case JTokenType.Array: DrawArray((JArray)token, label, path); break;
                default: DrawLeaf(token, label, path); break;
            }
        }

        private void DrawObject(JObject obj, string label, string path)
        {
            var open = IsOpen(path, true);
            EditorGUILayout.BeginHorizontal();
            var newOpen = EditorGUILayout.Foldout(open, $"{label}  {{{obj.Count}}}", true);
            SetOpen(path, newOpen);

            using (new EditorGUI.DisabledScope(ReadOnly))
            {
                if (GUILayout.Button("+key", GUILayout.Width(50)))
                {
                    var key = "newKey" + obj.Count;
                    while (obj.ContainsKey(key)) key += "_";
                    obj[key] = "";
                    Dirty = true;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!newOpen) return;
            EditorGUI.indentLevel++;

            string keyToRemove = null;
            foreach (var prop in obj.Properties())
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                DrawToken(prop.Value, prop.Name, path + "." + prop.Name);
                EditorGUILayout.EndVertical();

                using (new EditorGUI.DisabledScope(ReadOnly))
                {
                    if (GUILayout.Button("X", GUILayout.Width(22)))
                    {
                        keyToRemove = prop.Name;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            if (keyToRemove != null)
            {
                obj.Remove(keyToRemove);
                Dirty = true;
            }
            EditorGUI.indentLevel--;
        }

        private void DrawArray(JArray array, string label, string path)
        {
            var open = IsOpen(path, true);
            EditorGUILayout.BeginHorizontal();
            var newOpen = EditorGUILayout.Foldout(open, $"{label}  [{array.Count}]", true);
            SetOpen(path, newOpen);

            using (new EditorGUI.DisabledScope(ReadOnly))
            {
                if (GUILayout.Button("+", GUILayout.Width(22)))
                {
                    array.Add("");
                    Dirty = true;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!newOpen) return;
            EditorGUI.indentLevel++;

            int removeIndex = -1;
            for (int i = 0; i < array.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                DrawToken(array[i], "[" + i + "]", path + "[" + i + "]");
                EditorGUILayout.EndVertical();
                using (new EditorGUI.DisabledScope(ReadOnly))
                {
                    if (GUILayout.Button("X", GUILayout.Width(22)))
                        removeIndex = i;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (removeIndex >= 0)
            {
                array.RemoveAt(removeIndex);
                Dirty = true;
            }
            EditorGUI.indentLevel--;
        }

        private void DrawLeaf(JToken token, string label, string path)
        {
            EditorGUILayout.BeginHorizontal();
            var typeTag = "[" + token.Type.ToString().ToLowerInvariant() + "]";
            EditorGUILayout.LabelField(label, typeTag, GUILayout.MaxWidth(280));

            using (new EditorGUI.DisabledScope(ReadOnly))
            {
                switch (token.Type)
                {
                    case JTokenType.Boolean:
                    {
                        var v = token.Value<bool>();
                        var nv = EditorGUILayout.Toggle(v);
                        if (nv != v) { Replace(token, new JValue(nv)); Dirty = true; }
                        break;
                    }
                    case JTokenType.Integer:
                    {
                        var v = token.Value<long>();
                        var nv = EditorGUILayout.LongField(v);
                        if (nv != v) { Replace(token, new JValue(nv)); Dirty = true; }
                        break;
                    }
                    case JTokenType.Float:
                    {
                        var v = token.Value<double>();
                        var nv = EditorGUILayout.DoubleField(v);
                        if (nv != v) { Replace(token, new JValue(nv)); Dirty = true; }
                        break;
                    }
                    case JTokenType.Null:
                    {
                        EditorGUILayout.LabelField("null");
                        break;
                    }
                    default:
                    {
                        var s = token.Value<string>() ?? "";
                        var ns = EditorGUILayout.TextField(s);
                        if (ns != s) { Replace(token, new JValue(ns)); Dirty = true; }
                        break;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void Replace(JToken oldToken, JToken newToken)
        {
            oldToken.Replace(newToken);
        }

        private bool IsOpen(string path, bool defaultOpen)
        {
            if (m_OpenPaths.Contains(path)) return true;
            if (defaultOpen && !m_OpenPaths.Contains("closed:" + path)) return true;
            return false;
        }

        private void SetOpen(string path, bool open)
        {
            if (open)
            {
                m_OpenPaths.Add(path);
                m_OpenPaths.Remove("closed:" + path);
            }
            else
            {
                m_OpenPaths.Remove(path);
                m_OpenPaths.Add("closed:" + path);
            }
        }
    }
}
#endif

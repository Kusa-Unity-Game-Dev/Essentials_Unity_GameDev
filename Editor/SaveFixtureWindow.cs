#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using BB.Framework.SaveV2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace BB.Framework
{
    /// <summary>
    /// Tools/Save System/Fixtures. Capture a slot's state as a git-diffable bundle under
    /// Assets/SaveFixtures/&lt;name&gt;/ (one plain-text .save.json per module), and inject a fixture
    /// back into a slot — Full (wipe + write all) or Partial (overwrite only the fixture's modules).
    /// Checksums are recomputed on inject, so hand-editing fixture JSON is safe.
    /// </summary>
    public class SaveFixtureWindow : EditorWindow
    {
        private Vector2 m_FixtureScroll;
        private Vector2 m_DetailScroll;
        private string m_SelectedFixture;
        private SaveFixtureMetadata m_Meta;
        private string m_NewFixtureName = "";
        private string m_NewFixtureDescription = "";
        private string m_CaptureSlot = "";
        private string m_InjectTargetSlot = "";
        private FixtureInjectMode m_InjectMode = FixtureInjectMode.Full;
        private string m_Status;
        private MessageType m_StatusType = MessageType.None;

        [MenuItem("Tools/Save System/Fixtures")]
        public static void ShowWindow()
        {
            var w = GetWindow<SaveFixtureWindow>("Save Fixtures");
            w.minSize = new Vector2(700, 500);
            w.Show();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.BeginHorizontal();
            DrawFixturePane();
            DrawDetailPane();
            EditorGUILayout.EndHorizontal();
            DrawStatus();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70))) m_SelectedFixture = m_SelectedFixture;
            if (GUILayout.Button("Open fixtures folder", EditorStyles.toolbarButton, GUILayout.Width(160)))
            {
                EnsureFixturesRoot();
                SaveEditorUtils.OpenInExplorer(SaveEditorUtils.FixturesAssetRoot);
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label(SaveEditorUtils.FixturesAssetRoot, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFixturePane()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(260));
            EditorGUILayout.LabelField("Fixtures", EditorStyles.boldLabel);
            m_FixtureScroll = EditorGUILayout.BeginScrollView(m_FixtureScroll);
            var fixtures = EnumerateFixtures();
            if (fixtures.Count == 0) EditorGUILayout.HelpBox("No fixtures yet. Capture one below.", MessageType.Info);
            foreach (var name in fixtures)
            {
                var selected = name == m_SelectedFixture;
                var style = selected ? EditorStyles.toolbarButton : EditorStyles.miniButton;
                if (GUILayout.Button(name, style)) SelectFixture(name);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Capture", EditorStyles.boldLabel);
            IReadOnlyList<string> slots = SaveEditorUtils.EnumerateSlots();
            
            int slotIdx = -1;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == m_CaptureSlot)
                {
                    slotIdx = i;
                    break;
                }
            }
            
            int newSlotIdx = EditorGUILayout.Popup("source slot", slotIdx, AsArray(slots));
            if (newSlotIdx >= 0 && newSlotIdx < slots.Count) m_CaptureSlot = slots[newSlotIdx];

            m_NewFixtureName = EditorGUILayout.TextField("name", m_NewFixtureName);
            EditorGUILayout.LabelField("description");
            m_NewFixtureDescription = EditorGUILayout.TextArea(m_NewFixtureDescription, GUILayout.MinHeight(40));

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(m_CaptureSlot) || string.IsNullOrEmpty(m_NewFixtureName)))
            {
                if (GUILayout.Button("Capture current slot")) CaptureSlot();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawDetailPane()
        {
            EditorGUILayout.BeginVertical();
            if (string.IsNullOrEmpty(m_SelectedFixture))
            {
                EditorGUILayout.HelpBox("Pick or capture a fixture.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.LabelField("Fixture: " + m_SelectedFixture, EditorStyles.boldLabel);
            if (m_Meta == null)
            {
                EditorGUILayout.HelpBox("metadata missing or unreadable", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            m_DetailScroll = EditorGUILayout.BeginScrollView(m_DetailScroll);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("capturedAt", m_Meta.CapturedAt ?? "");
                EditorGUILayout.TextField("slotSource", m_Meta.SlotSource ?? "");
                EditorGUILayout.TextField("packageVersion", m_Meta.PackageVersion ?? "");
                EditorGUILayout.TextField("modules", string.Join(", ", m_Meta.ModulesIncluded));
            }
            m_Meta.Description = EditorGUILayout.TextArea(m_Meta.Description ?? "", GUILayout.MinHeight(50));
            if (GUILayout.Button("Save metadata edits")) WriteFixtureMetadata(m_SelectedFixture, m_Meta);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Inject", EditorStyles.boldLabel);
            m_InjectTargetSlot = EditorGUILayout.TextField("target slot", m_InjectTargetSlot);
            m_InjectMode = (FixtureInjectMode)EditorGUILayout.EnumPopup("mode", m_InjectMode);
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(m_InjectTargetSlot)))
            {
                if (GUILayout.Button("Inject fixture into target slot")) InjectFixture();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Files", EditorStyles.boldLabel);
            var folder = FixtureFolder(m_SelectedFixture);
            foreach (var file in Directory.GetFiles(folder, "*.save.json"))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Path.GetFileName(file));
                if (GUILayout.Button("Open", GUILayout.Width(60)))
                    EditorUtility.OpenWithDefaultApp(file);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Delete fixture (irreversible)")) DeleteFixture();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawStatus()
        {
            if (string.IsNullOrEmpty(m_Status)) return;
            EditorGUILayout.HelpBox(m_Status, m_StatusType);
        }

        private void SetStatus(string s, MessageType t) { m_Status = s; m_StatusType = t; Repaint(); }

        private static string[] AsArray(IReadOnlyList<string> list)
        {
            var arr = new string[list.Count];
            for (int i = 0; i < list.Count; i++) arr[i] = list[i];
            return arr;
        }

        private static string FixtureFolder(string name) => Path.Combine(SaveEditorUtils.FixturesAssetRoot, name);

        private static void EnsureFixturesRoot()
        {
            if (!Directory.Exists(SaveEditorUtils.FixturesAssetRoot))
                Directory.CreateDirectory(SaveEditorUtils.FixturesAssetRoot);
        }

        private List<string> EnumerateFixtures()
        {
            EnsureFixturesRoot();
            var result = new List<string>();
            foreach (var dir in Directory.GetDirectories(SaveEditorUtils.FixturesAssetRoot))
                result.Add(Path.GetFileName(dir));
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        private void SelectFixture(string name)
        {
            m_SelectedFixture = name;
            m_Meta = LoadFixtureMetadata(name);
            m_InjectTargetSlot = m_Meta?.SlotSource ?? "";
            m_InjectMode = m_Meta?.GetMode() ?? FixtureInjectMode.Full;
        }

        private static SaveFixtureMetadata LoadFixtureMetadata(string name)
        {
            var path = Path.Combine(FixtureFolder(name), SaveFixtureMetadata.FileName);
            if (!File.Exists(path)) return null;
            try { return JsonConvert.DeserializeObject<SaveFixtureMetadata>(File.ReadAllText(path)); }
            catch { return null; }
        }

        private static void WriteFixtureMetadata(string name, SaveFixtureMetadata meta)
        {
            var folder = FixtureFolder(name);
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, SaveFixtureMetadata.FileName);
            File.WriteAllText(path, JsonConvert.SerializeObject(meta, Formatting.Indented));
            AssetDatabase.Refresh();
        }

        private void CaptureSlot()
        {
            try
            {
                var folder = FixtureFolder(m_NewFixtureName);
                if (Directory.Exists(folder))
                {
                    if (!EditorUtility.DisplayDialog("Overwrite?", $"Fixture '{m_NewFixtureName}' already exists. Overwrite?", "Overwrite", "Cancel"))
                        return;
                    Directory.Delete(folder, recursive: true);
                }
                Directory.CreateDirectory(folder);

                var meta = new SaveFixtureMetadata
                {
                    Name = m_NewFixtureName,
                    Description = m_NewFixtureDescription,
                    CapturedAt = DateTime.UtcNow.ToString("o"),
                    SlotSource = m_CaptureSlot,
                    PackageVersion = SaveEnvelope.PackageVersion,
                };
                meta.SetMode(FixtureInjectMode.Full);

                var files = SaveEditorUtils.EnumerateModuleFiles(m_CaptureSlot);
                foreach (var srcPath in files)
                {
                    var moduleId = SaveEditorUtils.ModuleIdFromPath(srcPath);
                    var enc = SaveEditorUtils.TryGetEncryptorFor(moduleId);
                    var r = SaveEditorUtils.ReadEnvelope(srcPath, enc);
                    if (!r.Ok)
                    {
                        SetStatus("Skip " + moduleId + ": " + r.Error, MessageType.Warning);
                        continue;
                    }
                    var doc = new JObject
                    {
                        [SaveEnvelope.EnvelopeKey] = r.Header,
                        [SaveEnvelope.DataKey] = r.Data,
                    };
                    var dstPath = Path.Combine(folder, moduleId + ".save.json");
                    File.WriteAllText(dstPath, doc.ToString(Formatting.Indented));
                    meta.ModulesIncluded.Add(moduleId);
                }

                WriteFixtureMetadata(m_NewFixtureName, meta);
                AssetDatabase.Refresh();
                SetStatus($"Captured {meta.ModulesIncluded.Count} module(s) into '{m_NewFixtureName}'.", MessageType.Info);
                m_NewFixtureName = "";
                m_NewFixtureDescription = "";
                SelectFixture(meta.Name);
            }
            catch (Exception e)
            {
                SetStatus("Capture failed: " + e.Message, MessageType.Error);
            }
        }

        private void InjectFixture()
        {
            try
            {
                if (m_Meta == null) { SetStatus("no metadata loaded", MessageType.Error); return; }
                var folder = FixtureFolder(m_SelectedFixture);
                var targetDir = SaveEditorUtils.GetSlotPath(m_InjectTargetSlot);

                if (m_InjectMode == FixtureInjectMode.Full)
                {
                    if (Directory.Exists(targetDir))
                    {
                        if (!EditorUtility.DisplayDialog("Wipe slot?",
                            $"Full mode will delete slot '{m_InjectTargetSlot}' contents before injection.",
                            "Wipe + inject", "Cancel"))
                            return;
                        Directory.Delete(targetDir, recursive: true);
                    }
                }
                Directory.CreateDirectory(targetDir);

                int written = 0;
                foreach (var file in Directory.GetFiles(folder, "*.save.json"))
                {
                    var moduleId = Path.GetFileName(file).Replace(".save.json", "");
                    var doc = JObject.Parse(File.ReadAllText(file));
                    var headerObj = (JObject)doc[SaveEnvelope.EnvelopeKey];
                    var dataObj = (JObject)doc[SaveEnvelope.DataKey];
                    if (headerObj == null || dataObj == null)
                    {
                        SetStatus("malformed fixture file: " + Path.GetFileName(file), MessageType.Warning);
                        continue;
                    }
                    var header = headerObj.ToObject<SaveEnvelopeHeader>();
                    var descriptor = SaveEditorUtils.TryGetDescriptor(moduleId);
                    var compress = descriptor == null ? true : descriptor.Compressed;
                    var encryptor = (descriptor != null && descriptor.Encrypted)
                        ? SaveEditorUtils.TryGetEncryptorFor(moduleId)
                        : null;
                    var dstPath = SaveEditorUtils.GetModulePath(m_InjectTargetSlot, moduleId);
                    if (!SaveEditorUtils.WriteEnvelope(dstPath, dataObj, header, compress, encryptor, out var err))
                    {
                        SetStatus("write failed for " + moduleId + ": " + err, MessageType.Error);
                        continue;
                    }
                    written++;
                }

                if (SaveV2.SaveSystem.Instance != null && Application.isPlaying)
                {
                    SetStatus($"Injected {written} module(s). Reload in play mode: SaveSystem.Instance.LoadAllAsync(\"{m_InjectTargetSlot}\").", MessageType.Info);
                }
                else
                {
                    SetStatus($"Injected {written} module(s) into '{m_InjectTargetSlot}'.", MessageType.Info);
                }
            }
            catch (Exception e)
            {
                SetStatus("Inject failed: " + e.Message, MessageType.Error);
            }
        }

        private void DeleteFixture()
        {
            if (string.IsNullOrEmpty(m_SelectedFixture)) return;
            if (!EditorUtility.DisplayDialog("Delete fixture", $"Delete '{m_SelectedFixture}'?", "Delete", "Cancel")) return;
            var folder = FixtureFolder(m_SelectedFixture);
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
            var metaFile = folder + ".meta";
            if (File.Exists(metaFile)) File.Delete(metaFile);
            AssetDatabase.Refresh();
            m_SelectedFixture = null;
            m_Meta = null;
            SetStatus("Deleted.", MessageType.Info);
        }
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.IO;
using BB.Framework.SaveV2;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace BB.Framework
{
    /// <summary>
    /// Tools/Save System/Inspector. Opens one .save file, shows its envelope header (read-only) and
    /// data payload as an editable tree. Locked by default; unlock to edit, then "Recompute + Save"
    /// repacks with a fresh checksum, honoring the file's original compression/encryption.
    /// </summary>
    public class SaveDataInspectorWindow : EditorWindow
    {
        private string m_FilePath;
        private SaveEditorUtils.EnvelopeReadResult m_Result;
        private SaveEnvelopeHeader m_Header;
        private JObject m_Data;
        private readonly JObjectTreeDrawer m_Drawer = new JObjectTreeDrawer();
        private Vector2 m_Scroll;
        private bool m_Locked = true;
        private string m_Status;
        private MessageType m_StatusType = MessageType.None;

        [MenuItem("Tools/Save System/Inspector")]
        public static void ShowWindow()
        {
            var w = GetWindow<SaveDataInspectorWindow>("Save Inspector");
            w.minSize = new Vector2(500, 500);
            w.Show();
        }

        public static void OpenFile(string filePath)
        {
            var w = GetWindow<SaveDataInspectorWindow>("Save Inspector");
            w.minSize = new Vector2(500, 500);
            w.LoadFile(filePath);
            w.Show();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space();

            if (m_Result == null || !m_Result.Ok)
            {
                if (m_Result != null && !string.IsNullOrEmpty(m_Result.Error))
                    EditorGUILayout.HelpBox(m_Result.Error, MessageType.Error);
                else
                    EditorGUILayout.HelpBox("Pick a .save file to inspect.", MessageType.Info);
                return;
            }

            DrawHeaderBlock();
            EditorGUILayout.Space();
            DrawLockToggle();
            EditorGUILayout.Space();
            DrawDataTree();
            EditorGUILayout.Space();
            DrawSaveBar();
            DrawStatus();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Browse", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                var start = string.IsNullOrEmpty(m_FilePath) ? SaveEditorUtils.GetSavesRoot() : Path.GetDirectoryName(m_FilePath);
                var picked = EditorUtility.OpenFilePanel("Pick .save file", start, "save");
                if (!string.IsNullOrEmpty(picked)) LoadFile(picked);
            }
            if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(80)) && !string.IsNullOrEmpty(m_FilePath))
                LoadFile(m_FilePath);
            if (GUILayout.Button("Open Folder", EditorStyles.toolbarButton, GUILayout.Width(100)) && !string.IsNullOrEmpty(m_FilePath))
                SaveEditorUtils.OpenInExplorer(Path.GetDirectoryName(m_FilePath));
            GUILayout.FlexibleSpace();
            GUILayout.Label(string.IsNullOrEmpty(m_FilePath) ? "<no file>" : m_FilePath, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeaderBlock()
        {
            EditorGUILayout.LabelField("Envelope header", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("format", m_Header.Format);
                EditorGUILayout.TextField("moduleId", m_Header.ModuleId);
                EditorGUILayout.IntField("moduleVersion", m_Header.ModuleVersion);
                EditorGUILayout.TextField("savedAtUtc", m_Header.SavedAtUtc);
                EditorGUILayout.TextField("engineVersion", m_Header.EngineVersion);
                EditorGUILayout.TextField("packageVersion", m_Header.PackageVersion);
                EditorGUILayout.TextField("compression", m_Header.Compression);
                EditorGUILayout.TextField("encryption", m_Header.Encryption);
                EditorGUILayout.TextField("checksum", m_Header.Checksum);
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate checksum"))
            {
                ValidateChecksum();
            }
            if (GUILayout.Button("Copy header JSON"))
            {
                EditorGUIUtility.systemCopyBuffer = SaveEditorUtils.PrettyPrintJson(JObject.FromObject(m_Header));
                SetStatus("Header copied to clipboard.", MessageType.Info);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLockToggle()
        {
            var newLocked = !EditorGUILayout.ToggleLeft(" Unlocked (edit mode)", !m_Locked);
            if (newLocked != m_Locked)
            {
                m_Locked = newLocked;
                m_Drawer.ReadOnly = m_Locked;
            }
            if (!m_Locked)
                EditorGUILayout.HelpBox("Edit mode enabled. Save button will rewrite the file with a new checksum.", MessageType.Warning);
        }

        private void DrawDataTree()
        {
            EditorGUILayout.LabelField("Data payload", EditorStyles.boldLabel);
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll, GUILayout.MinHeight(200));
            m_Drawer.ReadOnly = m_Locked;
            m_Drawer.Draw(m_Data);
            EditorGUILayout.EndScrollView();
        }

        private void DrawSaveBar()
        {
            using (new EditorGUI.DisabledScope(m_Locked))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Recompute + Save"))
                {
                    SaveBack();
                }
                if (GUILayout.Button("Discard changes"))
                {
                    if (!string.IsNullOrEmpty(m_FilePath)) LoadFile(m_FilePath);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawStatus()
        {
            if (string.IsNullOrEmpty(m_Status)) return;
            EditorGUILayout.HelpBox(m_Status, m_StatusType);
        }

        private void LoadFile(string filePath)
        {
            m_FilePath = filePath;
            var moduleId = SaveEditorUtils.ModuleIdFromPath(filePath);
            var encryptor = SaveEditorUtils.TryGetEncryptorFor(moduleId);
            m_Result = SaveEditorUtils.ReadEnvelope(filePath, encryptor);
            if (!m_Result.Ok)
            {
                m_Header = null;
                m_Data = null;
                SetStatus(m_Result.Error, MessageType.Error);
                return;
            }
            m_Header = m_Result.Header.ToObject<SaveEnvelopeHeader>();
            m_Data = m_Result.Data;
            m_Drawer.ClearDirty();
            SetStatus("Loaded.", MessageType.None);
        }

        private void ValidateChecksum()
        {
            if (m_Data == null) return;
            var bytes = System.Text.Encoding.UTF8.GetBytes(m_Data.ToString(Newtonsoft.Json.Formatting.None));
            var current = "sha256:" + SaveEnvelope.Sha256Hex(bytes);
            if (string.Equals(current, m_Header.Checksum, StringComparison.OrdinalIgnoreCase))
                SetStatus("Checksum matches.", MessageType.Info);
            else
                SetStatus($"Checksum DIFFERS.\n  stored:  {m_Header.Checksum}\n  current: {current}", MessageType.Warning);
        }

        private void SaveBack()
        {
            if (string.IsNullOrEmpty(m_FilePath) || m_Data == null)
            {
                SetStatus("Nothing to save.", MessageType.Error);
                return;
            }
            var moduleId = SaveEditorUtils.ModuleIdFromPath(m_FilePath);
            var encryptor = SaveEditorUtils.TryGetEncryptorFor(moduleId);
            var compress = string.Equals(m_Header.Compression, "gzip", StringComparison.OrdinalIgnoreCase);
            if (!SaveEditorUtils.WriteEnvelope(m_FilePath, m_Data, m_Header, compress, encryptor, out var error))
            {
                SetStatus("Save failed: " + error, MessageType.Error);
                return;
            }
            SetStatus("Saved. Reloading to confirm…", MessageType.Info);
            LoadFile(m_FilePath);
        }

        private void SetStatus(string text, MessageType type)
        {
            m_Status = text;
            m_StatusType = type;
            Repaint();
        }
    }
}
#endif

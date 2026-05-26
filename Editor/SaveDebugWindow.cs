#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using BB.Framework.SaveV2;
using UnityEditor;
using UnityEngine;

namespace BB.Framework
{
    /// <summary>
    /// Tools/Save System/Debug Window. Central hub: lists slots and their module files (size, version,
    /// mtime), opens files in the Inspector, and in Play mode triggers save/load/delete. Subscribes to
    /// SaveSystem.OnRecovery to show a live, color-coded recovery-event log.
    /// </summary>
    public class SaveDebugWindow : EditorWindow
    {
        private Vector2 m_SlotScroll;
        private Vector2 m_ModuleScroll;
        private Vector2 m_LogScroll;
        private string m_SelectedSlot;
        private bool m_AutoRefresh = true;
        private double m_LastRefresh;
        private readonly List<SaveRecoveryEvent> m_RecoveryLog = new List<SaveRecoveryEvent>();
        private bool m_Subscribed;

        [MenuItem("Tools/Save System/Debug Window")]
        public static void ShowWindow()
        {
            var w = GetWindow<SaveDebugWindow>("Save Debug");
            w.minSize = new Vector2(700, 500);
            w.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            TrySubscribe();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            TryUnsubscribe();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) TrySubscribe();
            if (state == PlayModeStateChange.ExitingPlayMode) TryUnsubscribe();
            Repaint();
        }

        private void TrySubscribe()
        {
            if (m_Subscribed) return;
            if (SaveV2.SaveSystem.Instance == null) return;
            SaveV2.SaveSystem.Instance.OnRecovery += OnRecovery;
            m_Subscribed = true;
        }

        private void TryUnsubscribe()
        {
            if (!m_Subscribed) return;
            if (SaveV2.SaveSystem.Instance != null)
                SaveV2.SaveSystem.Instance.OnRecovery -= OnRecovery;
            m_Subscribed = false;
        }

        private void OnRecovery(SaveRecoveryEvent ev)
        {
            m_RecoveryLog.Add(ev);
            if (m_RecoveryLog.Count > 200) m_RecoveryLog.RemoveAt(0);
            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (!m_AutoRefresh) return;
            var now = EditorApplication.timeSinceStartup;
            if (now - m_LastRefresh < 1.0) return;
            m_LastRefresh = now;
            if (Application.isPlaying) TrySubscribe();
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.BeginHorizontal();
            DrawSlotPane();
            DrawModulePane();
            EditorGUILayout.EndHorizontal();
            DrawRecoveryLog();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(80))) Repaint();
            if (GUILayout.Button("Open persistent data path", EditorStyles.toolbarButton, GUILayout.Width(200)))
                SaveEditorUtils.OpenInExplorer(SaveEditorUtils.GetSavesRoot());
            m_AutoRefresh = GUILayout.Toggle(m_AutoRefresh, " auto-refresh", EditorStyles.toolbarButton, GUILayout.Width(110));
            GUILayout.FlexibleSpace();
            GUILayout.Label(Application.isPlaying ? "PLAY MODE" : "EDIT MODE", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSlotPane()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(280));
            EditorGUILayout.LabelField("Slots", EditorStyles.boldLabel);
            m_SlotScroll = EditorGUILayout.BeginScrollView(m_SlotScroll);
            var slots = SaveEditorUtils.EnumerateSlots();
            if (slots.Count == 0) EditorGUILayout.HelpBox("No slots yet.", MessageType.Info);
            foreach (var slot in slots)
            {
                var selected = slot == m_SelectedSlot;
                var style = selected ? EditorStyles.toolbarButton : EditorStyles.miniButton;
                if (GUILayout.Button(slot, style)) m_SelectedSlot = slot;
            }
            EditorGUILayout.EndScrollView();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Play-mode actions", EditorStyles.miniBoldLabel);
                if (!string.IsNullOrEmpty(m_SelectedSlot) && GUILayout.Button("SaveAll selected"))
                    _ = SaveV2.SaveSystem.Instance?.SaveAllAsync(m_SelectedSlot);
                if (!string.IsNullOrEmpty(m_SelectedSlot) && GUILayout.Button("LoadAll selected"))
                    _ = SaveV2.SaveSystem.Instance?.LoadAllAsync(m_SelectedSlot);
                if (!string.IsNullOrEmpty(m_SelectedSlot) && GUILayout.Button("Delete slot (irreversible)"))
                {
                    if (EditorUtility.DisplayDialog("Delete slot",
                        $"Delete slot '{m_SelectedSlot}' and all module files?", "Delete", "Cancel"))
                        _ = SaveV2.SaveSystem.Instance?.DeleteSlotAsync(m_SelectedSlot);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawModulePane()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(string.IsNullOrEmpty(m_SelectedSlot) ? "Modules" : "Modules in '" + m_SelectedSlot + "'", EditorStyles.boldLabel);
            m_ModuleScroll = EditorGUILayout.BeginScrollView(m_ModuleScroll);

            if (string.IsNullOrEmpty(m_SelectedSlot))
            {
                EditorGUILayout.HelpBox("Pick a slot on the left.", MessageType.Info);
            }
            else
            {
                var files = SaveEditorUtils.EnumerateModuleFiles(m_SelectedSlot);
                if (files.Count == 0)
                    EditorGUILayout.HelpBox("Slot has no .save files yet.", MessageType.Info);

                foreach (var file in files)
                {
                    DrawModuleRow(file);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawModuleRow(string file)
        {
            var id = SaveEditorUtils.ModuleIdFromPath(file);
            var size = SaveEditorUtils.GetFileSize(file);
            var mtime = SaveEditorUtils.GetLastModified(file);
            var descriptor = SaveEditorUtils.TryGetDescriptor(id);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(id, EditorStyles.boldLabel, GUILayout.Width(160));
            EditorGUILayout.LabelField($"v{descriptor?.Version ?? -1}", GUILayout.Width(40));
            if (descriptor != null && descriptor.Encrypted) EditorGUILayout.LabelField("[enc]", GUILayout.Width(40));
            EditorGUILayout.LabelField(FormatBytes(size), GUILayout.Width(80));
            EditorGUILayout.LabelField(mtime.ToString("yyyy-MM-dd HH:mm"), GUILayout.Width(140));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Inspect", GUILayout.Width(70)))
                SaveDataInspectorWindow.OpenFile(file);
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Save", GUILayout.Width(50)))
                    _ = SaveV2.SaveSystem.Instance?.SaveAsync(m_SelectedSlot, id);
                if (GUILayout.Button("Load", GUILayout.Width(50)))
                    _ = SaveV2.SaveSystem.Instance?.LoadAsync(m_SelectedSlot, id);
            }
            if (GUILayout.Button("Del", GUILayout.Width(40)))
            {
                if (EditorUtility.DisplayDialog("Delete file", $"Delete '{Path.GetFileName(file)}'?", "Delete", "Cancel"))
                    File.Delete(file);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawRecoveryLog()
        {
            EditorGUILayout.LabelField("Recovery events", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{m_RecoveryLog.Count} events  ·  subscribed={m_Subscribed}");
            if (GUILayout.Button("Clear", GUILayout.Width(60))) m_RecoveryLog.Clear();
            EditorGUILayout.EndHorizontal();

            m_LogScroll = EditorGUILayout.BeginScrollView(m_LogScroll, GUILayout.MinHeight(120), GUILayout.MaxHeight(220));
            for (int i = m_RecoveryLog.Count - 1; i >= 0; i--)
            {
                var ev = m_RecoveryLog[i];
                var color = ColorFor(ev.Kind);
                var prev = GUI.color;
                GUI.color = color;
                EditorGUILayout.LabelField($"[{ev.Kind}] {ev.SlotName}/{ev.ModuleId}  —  {ev.Detail}");
                GUI.color = prev;
            }
            EditorGUILayout.EndScrollView();
        }

        private static Color ColorFor(SaveRecoveryKind kind)
        {
            switch (kind)
            {
                case SaveRecoveryKind.ChecksumMismatch: return new Color(1f, 0.6f, 0.3f);
                case SaveRecoveryKind.BackupRestored: return new Color(0.5f, 1f, 0.6f);
                case SaveRecoveryKind.DefaultsApplied: return new Color(1f, 0.5f, 0.5f);
                case SaveRecoveryKind.LegacyV1Migrated: return new Color(0.6f, 0.8f, 1f);
                case SaveRecoveryKind.SerializerFailed: return new Color(1f, 0.4f, 0.4f);
                case SaveRecoveryKind.EncryptionMissing: return new Color(1f, 0.7f, 0.2f);
            }
            return Color.white;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 0) return "—";
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F1") + " KB";
            return (bytes / 1048576.0).ToString("F2") + " MB";
        }
    }
}
#endif

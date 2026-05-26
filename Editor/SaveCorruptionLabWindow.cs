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
    /// Tools/Save System/Corruption Lab. Deliberately damages a chosen file (truncate, flip byte,
    /// overwrite, delete main/backups) so you can verify the load recovery cascade. "Trigger Load"
    /// (Play mode) runs SaveSystem.LoadAsync and the recovery events are captured below.
    /// Intended for dev/throwaway slots only.
    /// </summary>
    public class SaveCorruptionLabWindow : EditorWindow
    {
        private string m_Slot;
        private string m_ModuleId;
        private int m_TruncateBytes = 64;
        private Vector2 m_Scroll;
        private Vector2 m_LogScroll;
        private readonly List<string> m_ActionLog = new List<string>();
        private readonly List<SaveRecoveryEvent> m_RecoveryLog = new List<SaveRecoveryEvent>();
        private bool m_Subscribed;
        private System.Random m_Rng = new System.Random();

        [MenuItem("Tools/Save System/Corruption Lab")]
        public static void ShowWindow()
        {
            var w = GetWindow<SaveCorruptionLabWindow>("Corruption Lab");
            w.minSize = new Vector2(600, 500);
            w.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            TrySubscribe();
        }

        private void OnDisable()
        {
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
            if (m_Subscribed || SaveV2.SaveSystem.Instance == null) return;
            SaveV2.SaveSystem.Instance.OnRecovery += OnRecovery;
            m_Subscribed = true;
        }
        private void TryUnsubscribe()
        {
            if (!m_Subscribed) return;
            if (SaveV2.SaveSystem.Instance != null) SaveV2.SaveSystem.Instance.OnRecovery -= OnRecovery;
            m_Subscribed = false;
        }

        private void OnRecovery(SaveRecoveryEvent ev)
        {
            m_RecoveryLog.Add(ev);
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("These actions deliberately damage save files. Use on a dev/throwaway slot only.", MessageType.Warning);
            DrawTargetPicker();
            EditorGUILayout.Space();
            DrawFileStatus();
            EditorGUILayout.Space();
            DrawActions();
            EditorGUILayout.Space();
            DrawLogs();
        }

        private void DrawTargetPicker()
        {
            EditorGUILayout.BeginHorizontal();
            var slots = new List<string>(SaveEditorUtils.EnumerateSlots());
            int slotIdx = m_Slot == null ? -1 : slots.IndexOf(m_Slot);
            int newSlotIdx = EditorGUILayout.Popup("Slot", slotIdx, slots.ToArray());
            if (newSlotIdx >= 0 && newSlotIdx < slots.Count) m_Slot = slots[newSlotIdx];
            EditorGUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(m_Slot)) return;

            var files = SaveEditorUtils.EnumerateModuleFiles(m_Slot);
            var ids = new List<string>();
            foreach (var f in files) ids.Add(SaveEditorUtils.ModuleIdFromPath(f));
            int idIdx = m_ModuleId == null ? -1 : ids.IndexOf(m_ModuleId);
            int newIdIdx = EditorGUILayout.Popup("Module", idIdx, ids.ToArray());
            if (newIdIdx >= 0 && newIdIdx < ids.Count) m_ModuleId = ids[newIdIdx];
        }

        private void DrawFileStatus()
        {
            if (string.IsNullOrEmpty(m_Slot) || string.IsNullOrEmpty(m_ModuleId))
            {
                EditorGUILayout.HelpBox("Pick a slot and module.", MessageType.Info);
                return;
            }
            var path = SaveEditorUtils.GetModulePath(m_Slot, m_ModuleId);
            EditorGUILayout.LabelField("Target file state", EditorStyles.boldLabel);
            DrawFileRow("main   ", path);
            DrawFileRow(".bak.1 ", path + ".bak.1");
            DrawFileRow(".bak.2 ", path + ".bak.2");
            DrawFileRow(".tmp   ", path + ".tmp");
        }

        private void DrawFileRow(string label, string path)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(60));
            var exists = File.Exists(path);
            EditorGUILayout.LabelField(exists ? "present" : "missing", GUILayout.Width(80));
            if (exists)
            {
                var size = new FileInfo(path).Length;
                EditorGUILayout.LabelField(size + " B", GUILayout.Width(100));
                if (GUILayout.Button("Inspect", GUILayout.Width(70)) && path.EndsWith(".save"))
                    SaveDataInspectorWindow.OpenFile(path);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActions()
        {
            if (string.IsNullOrEmpty(m_Slot) || string.IsNullOrEmpty(m_ModuleId)) return;

            var path = SaveEditorUtils.GetModulePath(m_Slot, m_ModuleId);
            EditorGUILayout.LabelField("Damage actions", EditorStyles.boldLabel);

            m_TruncateBytes = EditorGUILayout.IntField("Truncate (bytes)", m_TruncateBytes);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Truncate tail")) Do(() => TruncateTail(path, m_TruncateBytes));
            if (GUILayout.Button("Flip random byte")) Do(() => FlipRandomByte(path));
            if (GUILayout.Button("Overwrite random")) Do(() => OverwriteRandom(path));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Delete main")) Do(() => SafeDelete(path));
            if (GUILayout.Button("Delete .bak.1")) Do(() => SafeDelete(path + ".bak.1"));
            if (GUILayout.Button("Delete .bak.2")) Do(() => SafeDelete(path + ".bak.2"));
            if (GUILayout.Button("Delete ALL"))
            {
                if (EditorUtility.DisplayDialog("Delete all", "Delete main + all backups?", "Delete", "Cancel"))
                {
                    Do(() => SafeDelete(path));
                    Do(() => SafeDelete(path + ".bak.1"));
                    Do(() => SafeDelete(path + ".bak.2"));
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Trigger Load (play mode)"))
                {
                    if (SaveV2.SaveSystem.Instance != null)
                        _ = SaveV2.SaveSystem.Instance.LoadAsync(m_Slot, m_ModuleId);
                    Log("Triggered LoadAsync");
                }
            }
        }

        private void DrawLogs()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Logs", EditorStyles.boldLabel);
            if (GUILayout.Button("Clear", GUILayout.Width(60))) { m_ActionLog.Clear(); m_RecoveryLog.Clear(); }
            EditorGUILayout.EndHorizontal();

            m_LogScroll = EditorGUILayout.BeginScrollView(m_LogScroll, GUILayout.MinHeight(150));
            EditorGUILayout.LabelField("— Actions —", EditorStyles.miniBoldLabel);
            for (int i = m_ActionLog.Count - 1; i >= 0; i--)
                EditorGUILayout.LabelField(m_ActionLog[i]);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("— Recovery events —", EditorStyles.miniBoldLabel);
            for (int i = m_RecoveryLog.Count - 1; i >= 0; i--)
            {
                var ev = m_RecoveryLog[i];
                EditorGUILayout.LabelField($"[{ev.Kind}] {ev.SlotName}/{ev.ModuleId}  —  {ev.Detail}");
            }
            EditorGUILayout.EndScrollView();
        }

        private void Do(Action action)
        {
            try { action(); }
            catch (Exception e) { Log("ERROR: " + e.Message); }
            Repaint();
        }

        private void Log(string msg)
        {
            m_ActionLog.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + msg);
            if (m_ActionLog.Count > 200) m_ActionLog.RemoveAt(0);
        }

        private void TruncateTail(string path, int n)
        {
            if (!File.Exists(path)) { Log("file missing: " + path); return; }
            var bytes = File.ReadAllBytes(path);
            if (n >= bytes.Length) { File.WriteAllBytes(path, Array.Empty<byte>()); Log("truncated to 0"); return; }
            var trimmed = new byte[bytes.Length - n];
            Array.Copy(bytes, trimmed, trimmed.Length);
            File.WriteAllBytes(path, trimmed);
            Log($"truncated {n} bytes from {Path.GetFileName(path)}");
        }

        private void FlipRandomByte(string path)
        {
            if (!File.Exists(path)) { Log("file missing"); return; }
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length == 0) { Log("file empty"); return; }
            int idx = m_Rng.Next(bytes.Length);
            bytes[idx] ^= 0xFF;
            File.WriteAllBytes(path, bytes);
            Log($"flipped byte at offset {idx}");
        }

        private void OverwriteRandom(string path)
        {
            if (!File.Exists(path)) { Log("file missing"); return; }
            var bytes = new byte[new FileInfo(path).Length];
            m_Rng.NextBytes(bytes);
            File.WriteAllBytes(path, bytes);
            Log("overwrote with random bytes");
        }

        private void SafeDelete(string path)
        {
            if (File.Exists(path)) { File.Delete(path); Log("deleted " + Path.GetFileName(path)); }
            else Log("nothing to delete: " + Path.GetFileName(path));
        }
    }
}
#endif

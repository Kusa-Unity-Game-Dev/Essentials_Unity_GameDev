#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BB.Framework.SaveV2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace BB.Framework
{
    /// <summary>
    /// Shared helpers for the save-system editor windows. Wraps the runtime envelope codec
    /// (SaveEnvelope.Pack/TryUnpack) so editor tools never reimplement the file format, plus
    /// slot/module enumeration and an atomic-write that mirrors LocalDiskStorage's backup rotation.
    /// </summary>
    public static class SaveEditorUtils
    {
        public const string SaveDirectoryName = "GameSlots";
        public const string SlotPrefix = "GS_";
        public const string ModuleExtension = ".save";

        public static string GetSavesRoot()
            => Path.Combine(Application.persistentDataPath, SaveDirectoryName);

        public static string GetSlotPath(string slotName)
            => Path.Combine(GetSavesRoot(), SlotPrefix + slotName);

        public static string GetModulePath(string slotName, string moduleId)
            => Path.Combine(GetSlotPath(slotName), moduleId + ModuleExtension);

        public static IReadOnlyList<string> EnumerateSlots()
        {
            var root = GetSavesRoot();
            if (!Directory.Exists(root)) return Array.Empty<string>();
            var list = new List<string>();
            foreach (var dir in Directory.GetDirectories(root))
            {
                var name = Path.GetFileName(dir);
                if (name.StartsWith(SlotPrefix))
                    list.Add(name.Substring(SlotPrefix.Length));
            }
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        public static IReadOnlyList<string> EnumerateModuleFiles(string slotName)
        {
            var path = GetSlotPath(slotName);
            if (!Directory.Exists(path)) return Array.Empty<string>();
            var list = new List<string>();
            foreach (var f in Directory.GetFiles(path, "*" + ModuleExtension, SearchOption.TopDirectoryOnly))
                list.Add(f);
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        public static string ModuleIdFromPath(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            return fileName;
        }

        public class EnvelopeReadResult
        {
            public bool Ok;
            public string Error;
            public JObject Header;     // header subobject
            public JObject Data;       // payload (decrypted)
            public bool WasCompressed;
            public bool WasEncrypted;
            public string EncryptionAlgo;
        }

        public static EnvelopeReadResult ReadEnvelope(string filePath, ISaveEncryptor encryptorOrNull)
        {
            var result = new EnvelopeReadResult();
            if (!File.Exists(filePath))
            {
                result.Error = "file not found: " + filePath;
                return result;
            }

            byte[] bytes;
            try { bytes = File.ReadAllBytes(filePath); }
            catch (Exception e) { result.Error = "read failed: " + e.Message; return result; }

            result.WasCompressed = SaveEnvelope.LooksGZipped(bytes);
            if (!SaveEnvelope.TryUnpack(bytes, encryptorOrNull, out var env, out var error))
            {
                result.Error = error;
                return result;
            }

            result.Header = JObject.FromObject(env.Header);
            result.Data = (JObject)env.Data;
            result.WasEncrypted = env.Header.Encryption != null && env.Header.Encryption != "none";
            result.EncryptionAlgo = env.Header.Encryption;
            result.Ok = true;
            return result;
        }

        public static bool WriteEnvelope(string filePath, JObject data, SaveEnvelopeHeader header, bool compress, ISaveEncryptor encryptorOrNull, out string error)
        {
            error = null;
            try
            {
                var bytes = SaveEnvelope.Pack(header, data, compress, encryptorOrNull, null);
                AtomicWrite(filePath, bytes);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        public static void AtomicWrite(string path, byte[] bytes)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var tmp = path + ".tmp";
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush(true);
            }

            if (File.Exists(path))
            {
                RotateBackups(path, 2);
                var bak1 = path + ".bak.1";
                if (File.Exists(bak1)) File.Delete(bak1);
                File.Move(path, bak1);
            }
            File.Move(tmp, path);
        }

        public static void RotateBackups(string path, int backupCount)
        {
            for (int i = backupCount; i >= 2; i--)
            {
                var src = path + ".bak." + (i - 1);
                var dst = path + ".bak." + i;
                if (File.Exists(src))
                {
                    if (File.Exists(dst)) File.Delete(dst);
                    File.Move(src, dst);
                }
            }
        }

        public static ISaveEncryptor TryGetEncryptorFor(string moduleId)
        {
            var descriptor = SaveModuleRegistry.Get(moduleId);
            if (descriptor == null || !descriptor.Encrypted) return null;

            if (SaveV2.SaveSystem.Instance != null)
            {
                var enc = SaveV2.SaveSystem.Instance.Encryptor;
                if (enc != null) return enc;
            }

            return new AesEncryption(new DefaultKeyProvider());
        }

        public static SaveModuleDescriptor TryGetDescriptor(string moduleId)
        {
            return SaveModuleRegistry.Get(moduleId);
        }

        public static void OpenInExplorer(string path)
        {
            if (Directory.Exists(path) || File.Exists(path))
                EditorUtility.RevealInFinder(path);
            else
                Debug.LogWarning("[SaveEditor] path missing: " + path);
        }

        public static long GetFileSize(string path)
        {
            if (!File.Exists(path)) return -1;
            return new FileInfo(path).Length;
        }

        public static DateTime GetLastModified(string path)
        {
            if (!File.Exists(path)) return DateTime.MinValue;
            return File.GetLastWriteTime(path);
        }

        public static string ReadAllText(string path) => File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null;

        public static void WriteAllText(string path, string text)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, text, new UTF8Encoding(false));
        }

        public static string PrettyPrintJson(JToken token)
            => token == null ? "(null)" : token.ToString(Formatting.Indented);

        public const string FixturesAssetRoot = "Assets/SaveFixtures";
    }
}
#endif

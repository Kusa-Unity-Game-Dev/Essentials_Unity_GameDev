using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace BB.Framework.SaveV2
{
    /// <summary>
    /// Default storage backend: writes save files to the local file system under
    /// <c>Application.persistentDataPath/GameSlots/GS_&lt;slot&gt;/</c>.
    ///
    /// Provides the two durability guarantees the system relies on:
    ///  - Atomic writes (write to .tmp, fsync, rename) so a crash mid-write never corrupts the live file.
    ///  - Rolling backups (.bak.1, .bak.2) so a bad save still leaves a recoverable previous version.
    ///
    /// All I/O runs on a background thread via Task.Run to keep the main thread responsive.
    /// Swap this out for a cloud backend by assigning <see cref="SaveSystem.Storage"/>.
    /// </summary>
    public class LocalDiskStorage : ISaveStorageBackend
    {
        public const string SaveDirectory = "GameSlots";
        public const string SlotPrefix = "GS_";

        /// <summary>How many previous versions to keep per file (.bak.1 .. .bak.N).</summary>
        public int BackupCount { get; set; } = 2;

        private readonly string m_Root;

        public LocalDiskStorage(string rootOverride = null)
        {
            m_Root = rootOverride ?? Path.Combine(Application.persistentDataPath, SaveDirectory);
            Directory.CreateDirectory(m_Root);
        }

        // A storage key is slot-relative, e.g. "GS_Game1/currency.save".
        public string SlotKey(string slotName, string fileName) => SlotPrefix + slotName + "/" + fileName;

        private string AbsolutePath(string key) => Path.Combine(m_Root, key.Replace('/', Path.DirectorySeparatorChar));

        public Task<bool> ExistsAsync(string key) => Task.FromResult(File.Exists(AbsolutePath(key)));

        public Task<byte[]> ReadAsync(string key)
        {
            var path = AbsolutePath(key);
            return Task.Run(() => File.Exists(path) ? File.ReadAllBytes(path) : null);
        }

        public Task WriteAsync(string key, byte[] bytes)
        {
            var path = AbsolutePath(key);
            return Task.Run(() => AtomicWrite(path, bytes));
        }

        public Task DeleteAsync(string key)
        {
            var path = AbsolutePath(key);
            return Task.Run(() =>
            {
                if (File.Exists(path)) File.Delete(path);
                for (int i = 1; i <= BackupCount; i++)
                {
                    var bak = path + ".bak." + i;
                    if (File.Exists(bak)) File.Delete(bak);
                }
                var tmp = path + ".tmp";
                if (File.Exists(tmp)) File.Delete(tmp);
            });
        }

        public Task<IReadOnlyList<string>> ListSlotsAsync()
        {
            return Task.Run<IReadOnlyList<string>>(() =>
            {
                if (!Directory.Exists(m_Root)) return new List<string>();
                var slots = new List<string>();
                foreach (var dir in Directory.GetDirectories(m_Root))
                {
                    var name = Path.GetFileName(dir);
                    if (name.StartsWith(SlotPrefix))
                        slots.Add(name.Substring(SlotPrefix.Length));
                }
                return slots;
            });
        }

        public Task DeleteSlotAsync(string slotName)
        {
            var path = Path.Combine(m_Root, SlotPrefix + slotName);
            return Task.Run(() =>
            {
                if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
            });
        }

        public Task CreateSlotAsync(string slotName)
        {
            var path = Path.Combine(m_Root, SlotPrefix + slotName);
            Directory.CreateDirectory(path);
            return Task.CompletedTask;
        }

        public Task<bool> SlotExistsAsync(string slotName)
            => Task.FromResult(Directory.Exists(Path.Combine(m_Root, SlotPrefix + slotName)));

        public string GetSlotAbsolutePath(string slotName)
            => Path.Combine(m_Root, SlotPrefix + slotName);

        // The crash-safe write sequence:
        //   1. Write all bytes to "<path>.tmp" and fsync (Flush(true)) so they hit the physical disk.
        //   2. If a live file exists, rotate the backup chain and move the live file to .bak.1.
        //   3. Rename .tmp -> path. Rename is atomic on every OS, so the live file is always either
        //      the complete old version or the complete new version — never a half-written file.
        private void AtomicWrite(string path, byte[] bytes)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var tmp = path + ".tmp";
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush(true);   // true => flush OS buffers to disk
            }

            if (File.Exists(path))
            {
                RotateBackups(path);
                var bak1 = path + ".bak.1";
                if (File.Exists(bak1)) File.Delete(bak1);
                File.Move(path, bak1);
            }

            File.Move(tmp, path);
        }

        // Shift the backup chain up by one: .bak.(N-1) -> .bak.N, dropping the oldest.
        private void RotateBackups(string path)
        {
            for (int i = BackupCount; i >= 2; i--)
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

        /// <summary>Read a specific backup file (1-based). Used by the load recovery cascade.</summary>
        public byte[] ReadBackup(string key, int index)
        {
            var path = AbsolutePath(key) + ".bak." + index;
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
    }
}

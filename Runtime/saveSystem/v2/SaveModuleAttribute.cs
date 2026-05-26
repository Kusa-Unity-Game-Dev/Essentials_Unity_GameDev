using System;

namespace BB.Framework.SaveV2
{
    /// <summary>
    /// Marks a <see cref="SaveDataModule"/> subclass as a discoverable save module.
    /// <see cref="SaveSystem.AutoRegisterModules"/> reflection-scans for this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class SaveModuleAttribute : Attribute
    {
        /// <summary>On-disk identity and file name stem (e.g. "currency" -> currency.save). NEVER change after release — it's how saved files are found.</summary>
        public string Id { get; }

        /// <summary>Monotonic schema version. Bump by 1 on every field change and handle it in <see cref="SaveDataModule.Migrate"/>.</summary>
        public int Version { get; }

        /// <summary>When true, the payload is AES-encrypted on disk (see <see cref="AesEncryption"/>).</summary>
        public bool Encrypted { get; }

        /// <summary>When true (default), the file is GZip-compressed. Set false for dev modules you want to read in a text editor.</summary>
        public bool Compressed { get; }

        public SaveModuleAttribute(string id, int version = 1, bool encrypted = false, bool compressed = true)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("SaveModule id must be non-empty.", nameof(id));
            if (version < 1)
                throw new ArgumentOutOfRangeException(nameof(version), "Version must be >= 1.");

            Id = id;
            Version = version;
            Encrypted = encrypted;
            Compressed = compressed;
        }
    }
}

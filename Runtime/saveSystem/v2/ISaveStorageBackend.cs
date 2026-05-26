using System.Collections.Generic;
using System.Threading.Tasks;

namespace BB.Framework.SaveV2
{
    /// <summary>
    /// Abstraction over WHERE save bytes live. The default is <see cref="LocalDiskStorage"/>;
    /// implement this and assign it to <see cref="SaveSystem.Storage"/> to target Steam Cloud,
    /// PlayFab, Google Drive, an in-memory store for tests, etc. The rest of the save system
    /// only ever talks to this interface.
    ///
    /// A "key" is a slot-relative path like "GS_Game1/currency.save" — produced by <see cref="SlotKey"/>.
    /// </summary>
    public interface ISaveStorageBackend
    {
        Task<bool> ExistsAsync(string key);
        Task<byte[]> ReadAsync(string key);

        /// <summary>Write bytes durably. Implementations should be atomic (no half-written files).</summary>
        Task WriteAsync(string key, byte[] bytes);

        Task DeleteAsync(string key);

        Task<IReadOnlyList<string>> ListSlotsAsync();
        Task DeleteSlotAsync(string slotName);
        Task CreateSlotAsync(string slotName);
        Task<bool> SlotExistsAsync(string slotName);

        /// <summary>Compose a storage key from a slot name and a file name.</summary>
        string SlotKey(string slotName, string fileName);
    }
}

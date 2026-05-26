# Save System v2 — Implementation Guide

Industrial save/load for Unity games. Schema-versioned, integrity-checked, atomic, cloud-ready.

Namespace: `BB.Framework.SaveV2`
Package: `com.kusabb.essentials` ≥ 1.5.0
Unity: 6000.0+

---

## 1. What you get

| Feature | What it does |
|---|---|
| Envelope format | Every file wraps payload in `{ $envelope, data }` with SHA256, timestamp, engine + module version |
| Schema versioning | Each module declares `[SaveModule(id, version)]`; old saves auto-migrate field-by-field |
| Atomic writes | `.tmp` + fsync + rename — crash mid-save never corrupts the live file |
| Rolling backups | Keeps last 2 versions per file (`.bak.1`, `.bak.2`) for recovery |
| Integrity recovery | Checksum mismatch → try `.bak.1` → `.bak.2` → defaults; raises `SaveRecoveryEvent` |
| Slot manifest | Playtime, timestamp, screenshot, user label per slot — for save/load menus |
| Async API | `await SaveAsync(...)`. Disk work on background thread. No frame hitches. |
| Pluggable storage | Swap `LocalDiskStorage` for Steam Cloud / PlayFab / Drive — no API change |
| Pluggable serializer | Default is Newtonsoft.Json (handles dictionaries, polymorphism). Swap for MessagePack/Odin. |
| Optional encryption | Per-module opt-in AES-256-CBC, key derived via PBKDF2 |
| v1 auto-migration | Old `JsonUtility` saves are detected and upgraded on first load |

---

## 2. Install

The save system ships with the package — nothing extra to install.

**Dependencies** (resolved automatically by UPM):

- `com.unity.nuget.newtonsoft-json` ≥ 3.2.1

If you previously used the v1 save API (`SaveManager`, `SaveSlotManager`), your code keeps working. You will see `[Obsolete]` warnings. See section 11 to migrate.

---

## 3. Quick start

Five steps to a working save/load:

### Step 1 — Define a module

```csharp
using BB.Framework;
using BB.Framework.SaveV2;
using Newtonsoft.Json.Linq;

[SaveModule(id: "currency", version: 1)]
public class CurrencyModule : SaveDataModule
{
    public int Coins;
    public int Gems;

    public override void InitializeDefaults()
    {
        Coins = 100;
        Gems = 0;
    }
}
```

### Step 2 — Bootstrap the system

In a scene loader / boot scene:

```csharp
using BB.Framework.SaveV2;

void Awake()
{
    var sys = SaveSystem.EnsureInstance();   // creates GameObject + component
    sys.AutoRegisterModules();               // reflection-scans all [SaveModule] classes
}
```

### Step 3 — Create / load a slot

```csharp
await SaveSystem.Instance.CreateSlotAsync("Game1");
await SaveSystem.Instance.LoadAllAsync("Game1");
```

`LoadAllAsync` reads every registered module from the slot. Missing files → `InitializeDefaults()`.

### Step 4 — Read / mutate runtime data

```csharp
var currency = SaveSystem.Instance.GetModule<CurrencyModule>();
currency.Coins += 50;
```

### Step 5 — Save

```csharp
await SaveSystem.Instance.SaveAsync("Game1", "currency");
// or:
await SaveSystem.Instance.SaveAllAsync("Game1");
```

That's it. Files land in `Application.persistentDataPath/GameSlots/GS_Game1/`.

---

## 4. Defining save modules

A save module is **any class that inherits `SaveDataModule` and has `[SaveModule]`**.

```csharp
[SaveModule(id: "inventory", version: 1)]
public class InventoryModule : SaveDataModule
{
    public List<string> EquippedItemIds = new();
    public Dictionary<string, int> StackCounts = new();    // dictionaries OK — Newtonsoft
    public Vector3 LastPickupPos;
    public PlayerClass Class;                              // enums OK

    public override void InitializeDefaults()
    {
        EquippedItemIds.Clear();
        StackCounts.Clear();
    }
}
```

Rules:

- **Inherit `SaveDataModule`.** Direct, no intermediate base needed.
- **Tag with `[SaveModule(id, version)]`.** `id` is the on-disk identity — never change it after release. `version` starts at 1 and increases by 1 each schema change.
- **Public fields or `[JsonProperty]` properties** are serialized.
- Newtonsoft handles `Dictionary<K,V>`, `List<T>`, nullable types, polymorphism, Unity types like `Vector3`/`Quaternion`/`Color`, enums.
- **Don't** put `MonoBehaviour` references, `Transform`, `GameObject` in a save module — store IDs/names/coords instead.
- **One instance per module per `SaveSystem`.** The system instantiates it for you via `AutoRegisterModules()`.

### Attribute options

```csharp
[SaveModule(
    id: "secrets",
    version: 1,
    encrypted: true,     // opt-in AES; see section 10
    compressed: true     // default true; GZip the envelope
)]
public class SecretsModule : SaveDataModule { ... }
```

### Manual registration (if you skip `AutoRegisterModules`)

```csharp
SaveSystem.Instance.RegisterModule(new CurrencyModule());
SaveSystem.Instance.RegisterModule<InventoryModule>();
```

---

## 5. Slots

Slots are independent save universes (separate playthroughs, different characters, autosave vs manual, etc.).

```csharp
// Create
await SaveSystem.Instance.CreateSlotAsync("AutoSave");
await SaveSystem.Instance.CreateSlotAsync("Slot_Alice");

// List
var slots = await SaveSystem.Instance.ListSlotsAsync();
foreach (var m in slots) Debug.Log($"{m.SlotName}  saved={m.LastSavedUtc}  play={m.PlaytimeSeconds}s");

// Delete
await SaveSystem.Instance.DeleteSlotAsync("Slot_Alice");

// Exists
var exists = await SaveSystem.Instance.Storage.SlotExistsAsync("AutoSave");
```

Slot names are stored as-is in folder names. Avoid slashes, colons, or other path-illegal chars.

Files for a slot live under `Application.persistentDataPath/GameSlots/GS_<slotName>/`.

---

## 6. Slot manifest (save/load UI)

Every slot has a `manifest.save` with metadata you can show in a save-select screen without touching module files.

```csharp
public class SlotManifest
{
    public string SlotName;
    public string CreatedUtc;          // ISO 8601
    public string LastSavedUtc;
    public double PlaytimeSeconds;
    public Dictionary<string, int> Modules;   // moduleId → version present
    public string ScreenshotPath;
    public string UserLabel;
    public Dictionary<string, string> Custom; // free-form key/value
}
```

Update custom fields before saving:

```csharp
var m = await SaveSystem.Instance.GetManifestAsync("Game1");
m.UserLabel = "Chapter 2 — Forest";
m.PlaytimeSeconds += Time.realtimeSinceStartup;
m.Custom["chapterId"] = "ch_forest";
await SaveSystem.Instance.SaveAllAsync("Game1");   // manifest written automatically
```

The manifest is touched on every `SaveAsync`/`SaveAllAsync`.

---

## 7. Schema evolution and migration

When you change a module's fields, **bump its version and override `Migrate`**.

```csharp
[SaveModule(id: "currency", version: 3)]   // was 1, then 2, now 3
public class CurrencyModule : SaveDataModule
{
    public int Coins;
    public int Gems;                                 // added in v2
    public Dictionary<string, int> Wallet = new();   // added in v3

    public override void Migrate(JObject data, int fromVersion)
    {
        if (fromVersion < 2)
        {
            // v1 → v2: introduce Gems
            data["Gems"] = 0;
        }
        if (fromVersion < 3)
        {
            // v2 → v3: introduce Wallet, optionally migrate Coins into it
            var wallet = new JObject();
            wallet["soft"] = data["Coins"]?.Value<int>() ?? 0;
            data["Wallet"] = wallet;
        }
    }
}
```

How it works:

- File has `moduleVersion: 1`. Module's attribute says `version: 3`.
- `Migrate(data, 1)` runs → data is v2-shaped.
- `Migrate(data, 2)` runs → data is v3-shaped.
- Then deserialized into your module instance.
- On next save, file rewritten with `moduleVersion: 3`.

Rules:

- Versions are monotonic. Never decrease.
- Each call upgrades **one step**. The system loops until `fromVersion == version`.
- Removing a field? Just drop it from the C# class — Newtonsoft ignores unknown JSON keys.
- Renaming a field? Migrate it: `data["NewName"] = data["OldName"]; data.Remove("OldName");`.

---

## 8. Corruption recovery

On every load the system verifies the SHA256 checksum in the envelope. If it fails, recovery cascades:

1. Try `<file>.bak.1` (previous good save).
2. Try `<file>.bak.2` (one before that).
3. Call `InitializeDefaults()`.

Each step raises a `SaveRecoveryEvent`. Subscribe:

```csharp
// Direct callback
SaveSystem.Instance.OnRecovery += ev =>
    Debug.LogWarning($"Recovery: {ev.Kind} on '{ev.ModuleId}' in slot '{ev.SlotName}' — {ev.Detail}");

// Or via FSM global bus
FSM.AddListener_s(SaveRecoveryEvent.EventName, payload =>
{
    var ev = (SaveRecoveryEvent)payload;
    if (ev.Kind == SaveRecoveryKind.DefaultsApplied)
        ShowToast("Save data missing — starting fresh.");
});
```

Kinds:

| Kind | Meaning |
|---|---|
| `ChecksumMismatch` | File present but corrupt; falling through to backup |
| `BackupRestored` | Successfully recovered from `.bak.N` |
| `DefaultsApplied` | No usable file found; `InitializeDefaults()` used |
| `LegacyV1Migrated` | Old v1 file detected and converted |
| `SerializerFailed` | JSON parse / type mismatch |
| `EncryptionMissing` | File encrypted but no decryptor configured |

Backup count is tunable:

```csharp
((LocalDiskStorage)SaveSystem.Instance.Storage).BackupCount = 3;
```

---

## 9. Async usage in Unity

The API is `Task`-based. Three common patterns:

### Pattern A — `async void` Unity callback

```csharp
async void OnSaveButtonClicked()
{
    await SaveSystem.Instance.SaveAllAsync("Game1");
    saveButton.interactable = true;
}
```

### Pattern B — coroutine bridge

```csharp
IEnumerator SaveRoutine()
{
    var task = SaveSystem.Instance.SaveAllAsync("Game1");
    yield return new WaitUntil(() => task.IsCompleted);
    if (task.Exception != null) Debug.LogException(task.Exception);
}
```

### Pattern C — fire and forget (autosave)

```csharp
_ = SaveSystem.Instance.SaveAllAsync("AutoSave");
```

The save dispatches to a background thread, so the main thread stays free. Don't read/write module fields from another thread while a save is in flight.

---

## 10. Encryption (optional)

Add `encrypted: true` to the attribute:

```csharp
[SaveModule(id: "telemetry", version: 1, encrypted: true)]
public class TelemetryModule : SaveDataModule { ... }
```

The system uses AES-256-CBC with a key derived from `SystemInfo.deviceUniqueIdentifier` + a project-level salt via PBKDF2 (50,000 iterations). This **deters casual save editors**, not motivated attackers — the key lives on the device.

### Custom key provider

```csharp
public class MyKeyProvider : ISaveKeyProvider
{
    public byte[] DeriveKey(int byteLength)
    {
        // Pull from your own secret/seed
        return Convert.FromBase64String(PlayerPrefs.GetString("SaveKey"));
    }
}

void Awake()
{
    var sys = SaveSystem.EnsureInstance();
    sys.KeyProvider = new MyKeyProvider();
    sys.Encryptor = new AesEncryption(sys.KeyProvider);
    sys.AutoRegisterModules();
}
```

### Caveats

- Don't enable encryption for modules you want to inspect in `persistentDataPath` during dev.
- Changing the key invalidates existing encrypted saves. Plan key rotation as a migration.
- Sensitive data (cloud auth tokens, payment info, etc.) should not live in save files at all.

---

## 11. Migrating an existing project from v1

If your game was on `com.kusabb.essentials` ≤ 1.4.x, here's the path.

### Compile compatibility

Your old code compiles unchanged. `SaveManager`, `SaveSlotManager`, `ESaveModule`, and `SaveDataModule.SaveOnDemand/LoadOnDemand` are now `[Obsolete]` facades over v2. You'll see deprecation warnings — they are warnings only.

### Save-file compatibility

Old `JsonUtility` save files (`Currency.json`, `GameData.json`, etc.) are auto-detected on the next load and rewritten in v2 envelope format. No player data lost.

### Step-by-step migration

1. **Update the package** to 1.5.0. Let Unity import.
2. **Add `[SaveModule(id, version: 1)]` to your existing module classes.** Use `id` matching what `savemodule.ToString()` produced lowercased (e.g. `ECurrency` → `"currency"`). Without this, the system will synthesize an id from the enum — works but ugly.
3. **Bootstrap with `SaveSystem.EnsureInstance()` + `AutoRegisterModules()`** in your boot scene.
4. **Replace call sites gradually**:

| Old | New |
|---|---|
| `SaveSlotManager.Instance.CreateGameSlot("S")` | `await SaveSystem.Instance.CreateSlotAsync("S")` |
| `SaveManager.Instance.RegisterModule(new X())` | (auto) or `SaveSystem.Instance.RegisterModule<X>()` |
| `SaveManager.Instance.SaveModule("S", ESaveModule.ECurrency)` | `await SaveSystem.Instance.SaveAsync("S", "currency")` |
| `SaveManager.Instance.LoadModule("S", ESaveModule.ECurrency)` | `await SaveSystem.Instance.LoadAsync("S", "currency")` |
| `SaveManager.Instance.GetModule<X>(ESaveModule.ECurrency)` | `SaveSystem.Instance.GetModule<X>()` |
| `SaveSlotManager.Instance.GetAvailableSlots()` | `await SaveSystem.Instance.ListSlotsAsync()` |

5. **Drop `[System.Serializable]` and `override string FileName`** from new modules — not needed in v2.
6. **Optionally delete the old `.json` files** from `persistentDataPath/GameSlots/GS_*/` after first migrated save — the v2 system writes new `.save` files alongside and ignores the v1 ones from there on.

---

## 12. Implementing a custom storage backend (cloud)

Implement `ISaveStorageBackend`:

```csharp
public class SteamCloudStorage : ISaveStorageBackend
{
    public Task<bool> ExistsAsync(string key) => Task.FromResult(SteamRemoteStorage.FileExists(key));

    public async Task<byte[]> ReadAsync(string key)
    {
        if (!SteamRemoteStorage.FileExists(key)) return null;
        return await Task.Run(() => SteamRemoteStorage.FileRead(key));
    }

    public Task WriteAsync(string key, byte[] bytes)
        => Task.Run(() => SteamRemoteStorage.FileWrite(key, bytes));

    public Task DeleteAsync(string key) { SteamRemoteStorage.FileDelete(key); return Task.CompletedTask; }

    public Task<IReadOnlyList<string>> ListSlotsAsync() { /* enumerate files, extract slot names */ }
    public Task DeleteSlotAsync(string slotName) { /* delete all files with that prefix */ }
    public Task CreateSlotAsync(string slotName) => Task.CompletedTask;  // cloud has no folders
    public Task<bool> SlotExistsAsync(string slotName) { /* check any file with prefix */ }

    public string SlotKey(string slotName, string fileName) => $"GS_{slotName}/{fileName}";
}
```

Inject at bootstrap:

```csharp
var sys = SaveSystem.EnsureInstance();
sys.Storage = new SteamCloudStorage();
sys.AutoRegisterModules();
```

Atomic-write semantics are the backend's responsibility. `LocalDiskStorage` provides `.tmp` + rename; cloud backends typically need their own write-then-commit pattern.

---

## 13. Troubleshooting

**`SaveModule with id 'X' already registered on type Y, skipping`**
Two classes share the same `id` string. Each id must be unique. Rename one.

**`Module 'X' not registered`** thrown from `SaveAsync`
You forgot to call `AutoRegisterModules()` or to `RegisterModule<X>()`.

**`encrypted with 'aes256-cbc' but no matching decryptor configured`**
Module was saved with encryption enabled, but the running build has no `Encryptor` set, or `KeyProvider` returns a different key. Restore the matching provider.

**Newtonsoft loses my `Dictionary<int, Foo>` key type**
JSON dictionary keys are always strings. Use `Dictionary<string, T>` or wrap as `List<KeyValuePair<int,T>>`.

**`Save data missing — starting fresh.` on every launch**
Check `Application.persistentDataPath`. Make sure file write permissions are granted (mobile builds: check entitlements). Use `Debug.Log(Application.persistentDataPath)` to confirm the location.

**`UnityException: get_persistentDataPath can only be called from the main thread`**
You called `SaveSystem` methods before `Awake` (so `LocalDiskStorage` didn't initialize). Bootstrap on a `MonoBehaviour.Awake`, not in a static constructor.

**Save file is unreadable in a text editor**
By default modules are GZip-compressed. Set `compressed: false` on the `[SaveModule]` attribute for dev modules you want to eyeball.

**Migration ran but field is still default**
The migration runs **before** deserialization. If you set `data["Foo"] = 42` in `Migrate`, the field `Foo` on your module must exist (with the matching JSON property name) for the value to land.

---

## 14. File layout reference

```
<persistentDataPath>/
└── GameSlots/
    ├── GS_Game1/
    │   ├── manifest.save           ← slot metadata (envelope)
    │   ├── manifest.save.bak.1
    │   ├── currency.save           ← module file (envelope)
    │   ├── currency.save.bak.1
    │   ├── currency.save.bak.2
    │   ├── inventory.save
    │   └── inventory.save.bak.1
    └── GS_AutoSave/
        └── ...
```

Envelope content (before GZip):

```json
{
  "$envelope": {
    "format": 2,
    "moduleId": "currency",
    "moduleVersion": 3,
    "savedAtUtc": "2026-05-16T10:23:00.123Z",
    "engineVersion": "6000.0.20f1",
    "packageVersion": "1.5.0",
    "compression": "gzip",
    "encryption": "none",
    "checksum": "sha256:abcd1234..."
  },
  "data": {
    "Coins": 250,
    "Gems": 12,
    "Wallet": { "soft": 250 }
  }
}
```

---

## 15. Public API surface

```csharp
namespace BB.Framework.SaveV2
{
    public class SaveSystem : MonoBehaviour
    {
        static SaveSystem Instance { get; }
        static SaveSystem EnsureInstance();

        ISaveStorageBackend Storage { get; set; }
        ISerializer Serializer { get; set; }
        ISaveEncryptor Encryptor { get; set; }
        ISaveKeyProvider KeyProvider { get; set; }

        event Action<SaveRecoveryEvent> OnRecovery;

        void AutoRegisterModules();
        void RegisterModule(SaveDataModule module);
        T RegisterModule<T>() where T : SaveDataModule, new();

        T GetModule<T>() where T : SaveDataModule;
        T GetModule<T>(string moduleId) where T : SaveDataModule;
        SaveDataModule GetModule(string moduleId);

        Task SaveAsync(string slotName, string moduleId);
        Task SaveAllAsync(string slotName);
        Task LoadAsync(string slotName, string moduleId);
        Task<T> LoadAsync<T>(string slotName, string moduleId) where T : SaveDataModule;
        Task LoadAllAsync(string slotName);

        Task CreateSlotAsync(string slotName);
        Task DeleteSlotAsync(string slotName);
        Task<SlotManifest> GetManifestAsync(string slotName);
        Task<IReadOnlyList<SlotManifest>> ListSlotsAsync();
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SaveModuleAttribute : Attribute
    {
        public SaveModuleAttribute(string id, int version = 1, bool encrypted = false, bool compressed = true);
    }

    public abstract class SaveDataModule         // in BB.Framework, not BB.Framework.SaveV2
    {
        public virtual void InitializeDefaults();
        public virtual void Migrate(JObject data, int fromVersion);
    }
}
```

---

## 16. Recipe: complete bootstrap

```csharp
using BB.Framework.SaveV2;
using UnityEngine;

public class GameBoot : MonoBehaviour
{
    public string AutoSaveSlot = "AutoSave";

    async void Start()
    {
        var sys = SaveSystem.EnsureInstance();
        sys.AutoRegisterModules();

        sys.OnRecovery += ev => Debug.LogWarning(ev);

        if (!await sys.Storage.SlotExistsAsync(AutoSaveSlot))
            await sys.CreateSlotAsync(AutoSaveSlot);

        await sys.LoadAllAsync(AutoSaveSlot);

        InvokeRepeating(nameof(AutoSave), 60f, 60f);
    }

    async void AutoSave()
    {
        await SaveSystem.Instance.SaveAllAsync(AutoSaveSlot);
    }
}
```

Drop this on a single GameObject in your boot scene. Every module tagged `[SaveModule]` anywhere in your project gets discovered, loaded, and autosaved every minute. Done.

---

## 17. Editor tools

Four editor windows ship under `Tools/Save System/`. They share an asmdef (`BB.Framework.essentials.editor`) and read/write the same envelope format as the runtime, so anything they produce is interchangeable with what the game writes.

### 17.1 Save Debug Window — `Tools/Save System/Debug Window`

Central hub. Use it to:

- See every slot in `persistentDataPath` with size, module list, and per-module version.
- Click a `.save` file → opens the Inspector.
- In Play mode: trigger `SaveAsync` / `LoadAsync` / `DeleteSlotAsync` per slot or module.
- Watch `SaveRecoveryEvent` live as they fire (subscribes to `SaveSystem.OnRecovery`). Color-coded by `SaveRecoveryKind`.

Works in both Edit and Play mode. Edit mode is read-only (no save/load buttons).

### 17.2 Save Data Inspector — `Tools/Save System/Inspector`

Opens a single `.save` file and shows its contents.

- Envelope header panel shows `format`, `moduleId`, `moduleVersion`, `savedAtUtc`, `engineVersion`, `packageVersion`, `compression`, `encryption`, `checksum` (read-only).
- Data panel renders the payload as a foldout tree. Leaf values are editable.
- "Lock" toggle defaults to ON to prevent accidental writes.
- "Validate checksum" recomputes SHA256 vs stored value.
- "Recompute + Save" packs current state back into a fresh envelope (new checksum, new `savedAtUtc`), respecting the original compression and encryption flags. Atomic write to disk.
- Encrypted files: the inspector pulls the encryptor from `SaveSystem.Instance.Encryptor` if alive in play mode; otherwise it instantiates `AesEncryption` with `DefaultKeyProvider`. Set up a custom `ISaveKeyProvider` *before* opening the inspector if your project uses one.

Use this for: confirming what was actually written, debugging a value mismatch, hand-poking a field for a quick test without rebuilding the game.

### 17.3 Corruption Lab — `Tools/Save System/Corruption Lab`

Deliberately damages files so you can verify the recovery cascade.

Actions:

- **Truncate tail (N bytes)** — chops bytes off the end (configurable count)
- **Flip random byte** — XOR one byte with 0xFF
- **Overwrite random** — fills the file with random bytes
- **Delete main / .bak.1 / .bak.2 / ALL** — drops files outright

After damaging, hit "Trigger Load" (Play mode) and watch the recovery log. Expected cascade:

| Damage | Expected recovery |
|---|---|
| Truncate main, backups intact | `ChecksumMismatch` → `BackupRestored` from `.bak.1` |
| Delete main, .bak.1 intact | `BackupRestored` from `.bak.1` |
| Delete everything | `DefaultsApplied` |

Use this for: writing automated tests that *prove* the recovery code works on your real modules, not just the framework's own tests.

### 17.4 Fixtures — `Tools/Save System/Fixtures`

The high-value tool. Capture a slot's state as a project asset; inject it later to set up a known game state for testing.

**On-disk layout** (in your game project, not in this package):

```
Assets/SaveFixtures/
└── ch2_boss_start/
    ├── fixture.meta.json     ← name, description, mode, slotSource, modulesIncluded
    ├── currency.save.json    ← uncompressed plain-text envelope (diffable)
    ├── inventory.save.json
    └── progression.save.json
```

Each `*.save.json` is the literal envelope (`{ $envelope, data }`) saved uncompressed and unencrypted so it's git-diffable. The fixture system compresses + encrypts on injection according to each module's `[SaveModule]` attribute.

**Capture flow**:

1. Play the game to the state you want to bookmark. Save the slot (e.g. `Game1`).
2. Stop play. Open `Tools/Save System/Fixtures`.
3. Pick source slot = `Game1`, enter a fixture name like `ch2_boss_start` and a description.
4. Click **Capture current slot**. The system unpacks every `.save` file in the slot and writes them as `.save.json` files into `Assets/SaveFixtures/ch2_boss_start/`.
5. Commit to git. Anyone on the team can now repro that state in seconds.

**Inject flow**:

1. Select a fixture from the list.
2. Type a target slot name (defaults to whatever slot the fixture was captured from).
3. Pick mode:
   - **Full** — wipes the target slot, writes every fixture module.
   - **Partial** — leaves other modules alone, overwrites only the modules in the fixture.
4. Click **Inject**. Files land in `persistentDataPath/GameSlots/GS_<target>/`.
5. Enter play mode and call `LoadAllAsync(target)`. The injected state is now live.

**Use cases**:

- **Bug repro fixtures** — QA reports "currency wraps at 65535". Capture a fixture with `Coins: 65530`, commit. Anyone can repro in 5 seconds.
- **Feature testing** — testing the boss arena? Capture `pre_boss` + `post_boss` fixtures so designers can iterate without replaying the level.
- **Migration tests** — hand-craft a `legacy_v1_save.save.json` with `moduleVersion: 1`. Inject. Verify your `Migrate` chain.
- **Partial overrides** — drop a corrupt `currency.save.json` onto a normal save to confirm only that module triggers `DefaultsApplied`, others stay intact.

**Editing fixture files by hand**:

Each `.save.json` is plain text — open in any editor. Change `data.Coins` from 250 to 99999, save, inject. No tooling required. Just don't forget: if you edit the `data` payload, the `checksum` in the header becomes stale. The fixture system **recomputes the checksum on injection**, so stale checksums in your hand-edited fixture files are fine.

### 17.5 Shared utility — `SaveEditorUtils`

If you write your own editor tools, reach for `BB.Framework.SaveEditorUtils` (editor assembly):

- `EnumerateSlots()` / `EnumerateModuleFiles(slot)` — disk listing
- `ReadEnvelope(filePath, encryptorOrNull)` — returns header + decrypted data as JObject
- `WriteEnvelope(filePath, data, header, compress, encryptor, out error)` — atomic write with rotated backups
- `TryGetEncryptorFor(moduleId)` — pulls the right encryptor based on the module's `[SaveModule]` flags
- `TryGetDescriptor(moduleId)` — looks up the module's attribute (version, encrypted, compressed)

All of these go through the same `SaveEnvelope.Pack` / `TryUnpack` the runtime uses — no parallel implementation to drift.


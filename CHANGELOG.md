# Changelog

All notable changes to `com.kusabb.essentials` are documented here.

## [1.6.0] - 2026-07-02

### Changed

- `UIBase.ReadyAfterTransition` and `UIBase.HideAfterTransition` are now `protected virtual`
  (were `private`). Behavior is unchanged for existing UIs; subclasses can now substitute
  signal-based completion for the fixed delay. Motivated by `com.kusabb.chromeui`, whose
  interruptible transitions extend past the originally scheduled delay — its `ChromeUIBase`
  overrides both to wait until the transition director settles, and to skip disabling the
  canvas when the UI was re-shown mid-hide.

## [1.5.0] - 2026-05-16

### Added — Save System v2

A new industrial-grade save system lives alongside the v1 API under `BB.Framework.SaveV2`. The v1 API (`SaveManager`, `SaveSlotManager`, `ESaveModule`, `SaveDataModule.SaveOnDemand/LoadOnDemand`) is preserved as a deprecated facade — existing games keep compiling and running. v1 save files are auto-detected and migrated on first load.

**New features:**

- **Envelope format** — every save file is wrapped with `{ $envelope, data }`. Header carries `format`, `moduleId`, `moduleVersion`, `savedAtUtc`, `engineVersion`, `packageVersion`, `compression`, `encryption`, and SHA256 `checksum` of the payload.
- **Per-module schema versioning** — declare with `[SaveModule(id: "currency", version: 3)]`. Override `SaveDataModule.Migrate(JObject data, int fromVersion)` for stepwise field migration.
- **Attribute-based discovery** — `SaveSystem.AutoRegisterModules()` reflection-scans all loaded assemblies. Consumer games declare modules in their own code; no edits to this package required.
- **Atomic writes** — write to `.tmp`, fsync, then rename. Crash mid-save never corrupts the live file.
- **Rolling backups** — last 2 versions retained per module (`.bak.1`, `.bak.2`). Tunable on `LocalDiskStorage.BackupCount`.
- **Integrity + recovery** — SHA256 mismatch triggers automatic fallback to `.bak.1` → `.bak.2` → `InitializeDefaults()`. Each step raises a `SaveRecoveryEvent` via `FSM.DispatchEvent_s` and `SaveSystem.OnRecovery`.
- **Slot manifest** — `manifest.save` per slot carries playtime, timestamp, screenshot path, user label, and per-module versions. Drives save/load menus without parsing module files.
- **Async API** — `SaveAsync`, `LoadAsync`, `SaveAllAsync`, `LoadAllAsync`, `ListSlotsAsync`, `GetManifestAsync`, `DeleteSlotAsync`, `CreateSlotAsync`. Disk work runs on a background thread.
- **Pluggable storage** — `ISaveStorageBackend`. Default `LocalDiskStorage` writes under `Application.persistentDataPath/GameSlots/`. Cloud backends (Steam, PlayFab, Drive) can drop in without API change.
- **Pluggable serializer** — `ISerializer`. Default `NewtonsoftSerializer` handles dictionaries, polymorphism, nullables, and `TypeNameHandling.Auto` for runtime-typed fields. Swap to MessagePack or Odin if needed.
- **Optional AES-256-CBC encryption** — per-module opt-in via `[SaveModule(..., encrypted: true)]`. Key derived through `ISaveKeyProvider`; default uses PBKDF2 over `SystemInfo.deviceUniqueIdentifier` + project salt. Deters casual save editing; not a security guarantee.

**v1 → v2 migration:**

- v1 files (no envelope header) are detected on load, parsed via `JsonUtility.FromJsonOverwrite`, then rewritten in v2 envelope format on the next save.
- v1 subclasses without `[SaveModule]` attribute get a synthesized descriptor from `savemodule.ToString().ToLowerInvariant()` so they auto-register.
- `SaveDataModule.FileName` and `savemodule` are now `virtual` (was `abstract`). New v2 modules can skip them.

### Added — Save System v2 Editor Tools

Four editor windows under `Tools/Save System/`:

- **Save Debug Window** — central hub: slot list, module list with size/version/mtime, per-slot save/load/delete (play mode), live `SaveRecoveryEvent` log.
- **Save Data Inspector** — view + edit the JSON inside any `.save` file. Envelope header panel, editable data tree, validate-checksum and recompute-and-save buttons. Locked by default to prevent accidental writes.
- **Corruption Lab** — deliberately damage files (truncate / flip byte / overwrite / delete file or backups) and trigger load to verify recovery cascade.
- **Fixture System** — capture a slot's state as a git-diffable project asset under `Assets/SaveFixtures/<name>/`. Each module is stored as plain-text `.save.json`. Inject in Full or Partial mode to set up known states for bug repro and feature testing.

Shared editor utility `SaveEditorUtils` exposes envelope read/write and slot enumeration for custom tooling.

Editor asmdef now references `Unity.Newtonsoft.Json` directly.

### Changed

- `package.json`: version bumped `1.4.3 → 1.5.0`. Added `com.unity.nuget.newtonsoft-json: 3.2.1` dependency.

### Deprecated

- `SaveManager`, `SaveSlotManager` (classes flagged `[Obsolete]`; facades remain functional).
- `SaveDataModule.SaveOnDemand` / `LoadOnDemand` (methods flagged `[Obsolete]`; still work).
- `ESaveModule` enum (still used by the v1 facade; new code should use string IDs).

### Compatibility

- Source-compatible: any code targeting v1.4.x compiles against 1.5.0 with deprecation warnings only.
- Save-file-compatible: v1 save files load and auto-upgrade on first save.

---

## [1.4.3] and earlier

See git history.

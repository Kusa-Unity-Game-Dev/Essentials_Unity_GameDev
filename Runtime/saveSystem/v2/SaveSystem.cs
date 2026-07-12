using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BB.Framework.SaveV2
{
    /// <summary>
    /// Central orchestrator for the v2 save system. Singleton MonoBehaviour.
    ///
    /// Responsibilities:
    ///  - Holds the live instances of every registered <see cref="SaveDataModule"/> (one per moduleId).
    ///  - Coordinates serialize -> envelope -> storage on save, and the reverse + recovery cascade on load.
    ///  - Owns the pluggable strategy objects: where bytes go (<see cref="ISaveStorageBackend"/>),
    ///    how objects become JSON (<see cref="ISerializer"/>), and how payloads are encrypted
    ///    (<see cref="ISaveEncryptor"/> / <see cref="ISaveKeyProvider"/>).
    ///
    /// The strategy objects are swappable: assign a cloud backend or a different serializer before
    /// calling any save/load method and the rest of the system is unaffected.
    ///
    /// Typical bootstrap:
    ///   var sys = SaveSystem.EnsureInstance();
    ///   sys.AutoRegisterModules();          // discover [SaveModule] classes
    ///   await sys.CreateSlotAsync("Game1");
    ///   await sys.LoadAllAsync("Game1");
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        // --- Pluggable strategies. Default impls assigned in Awake; override before first save/load. ---
        public ISaveStorageBackend Storage { get; set; }
        public ISerializer Serializer { get; set; }
        public ISaveEncryptor Encryptor { get; set; }
        public ISaveKeyProvider KeyProvider { get; set; }

        // Live module instances keyed by their on-disk id (case-insensitive).
        private readonly Dictionary<string, SaveDataModule> m_Modules = new(StringComparer.OrdinalIgnoreCase);

        // Manifest cache so the save/load UI doesn't re-read disk on every repaint.
        private readonly Dictionary<string, SlotManifest> m_ManifestCache = new(StringComparer.OrdinalIgnoreCase);

        // In-flight write bookkeeping per gate key (a slot+module, or a slot for the
        // shared manifest). Lets concurrent saves serialize + coalesce onto one file
        // instead of racing. See the "Concurrent-save coalescing" region below.
        private readonly Dictionary<string, CoalescingGate> m_SaveGates = new(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, SaveDataModule> RegisteredModules => m_Modules;

        /// <summary>Fired whenever a load has to fall back (checksum fail, backup restore, defaults, legacy migrate).</summary>
        public event Action<SaveRecoveryEvent> OnRecovery;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Lazily assign the defaults only if a caller hasn't already injected a custom strategy.
            Storage ??= new LocalDiskStorage();
            Serializer ??= new NewtonsoftSerializer();
            KeyProvider ??= new DefaultKeyProvider();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Returns the existing instance, or spawns a hidden GameObject hosting one.
        /// Lets you use the system without manually placing a SaveSystem component in a scene.
        /// </summary>
        public static SaveSystem EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject(nameof(SaveSystem));
            return go.AddComponent<SaveSystem>();
        }

        // --- Module registration -------------------------------------------------

        /// <summary>Register a pre-built module instance. Its id comes from its [SaveModule] attribute (or is synthesized for legacy modules).</summary>
        public void RegisterModule(SaveDataModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            var d = SaveModuleRegistry.GetOrSynthesize(module);
            m_Modules[d.Id] = module;
        }

        /// <summary>Construct and register a module by type. Requires a parameterless constructor.</summary>
        public T RegisterModule<T>() where T : SaveDataModule, new()
        {
            var module = new T();
            RegisterModule(module);
            return module;
        }

        /// <summary>
        /// Reflection-scan every loaded assembly for [SaveModule] classes and instantiate any that
        /// aren't already registered. Call once at boot. Consumer games get their modules discovered
        /// without ever editing this package.
        /// </summary>
        public void AutoRegisterModules()
        {
            SaveModuleRegistry.ScanAssemblies();
            foreach (var pair in SaveModuleRegistry.All)
            {
                var d = pair.Value;
                if (m_Modules.ContainsKey(d.Id)) continue;
                try
                {
                    var instance = (SaveDataModule)Activator.CreateInstance(d.ModuleType);
                    m_Modules[d.Id] = instance;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveSystem] Failed to instantiate {d.ModuleType.FullName}: {e.Message}");
                }
            }
        }

        /// <summary>Get the live module instance by C# type.</summary>
        public T GetModule<T>() where T : SaveDataModule
        {
            var d = SaveModuleRegistry.Get(typeof(T));
            if (d == null) return null;
            m_Modules.TryGetValue(d.Id, out var m);
            return m as T;
        }

        public T GetModule<T>(string moduleId) where T : SaveDataModule
        {
            m_Modules.TryGetValue(moduleId, out var m);
            return m as T;
        }

        public SaveDataModule GetModule(string moduleId)
        {
            m_Modules.TryGetValue(moduleId, out var m);
            return m;
        }

        // --- Save ----------------------------------------------------------------

        /// <summary>Persist a single module to a slot, then refresh the slot manifest.</summary>
        public async Task SaveAsync(string slotName, string moduleId)
        {
            if (!m_Modules.TryGetValue(moduleId, out var module))
                throw new InvalidOperationException($"Module '{moduleId}' not registered.");

            var d = SaveModuleRegistry.GetOrSynthesize(module);
            // Route through the coalescing gate so two SaveAsync calls for the same
            // slot+module never write the same file at once, and a burst of requests
            // collapses into a single trailing write of the latest module state.
            await RunCoalescedAsync(ModuleGateKey(slotName, d.Id), () => WriteAndTouchAsync(slotName, module, d));
        }

        /// <summary>Persist every registered module to a slot.</summary>
        public async Task SaveAllAsync(string slotName)
        {
            foreach (var pair in m_Modules)
            {
                var d = SaveModuleRegistry.GetOrSynthesize(pair.Value);
                await RunCoalescedAsync(ModuleGateKey(slotName, d.Id), () => WriteAndTouchAsync(slotName, pair.Value, d));
            }
        }

        // --- Load ----------------------------------------------------------------

        /// <summary>Load a module and return the typed instance in one call.</summary>
        public async Task<T> LoadAsync<T>(string slotName, string moduleId) where T : SaveDataModule
        {
            await LoadAsync(slotName, moduleId);
            return GetModule<T>(moduleId);
        }

        /// <summary>
        /// Load a single module from a slot into its live instance. If the module isn't registered
        /// yet but is discoverable via [SaveModule], it is instantiated on demand.
        /// </summary>
        public async Task LoadAsync(string slotName, string moduleId)
        {
            if (!m_Modules.TryGetValue(moduleId, out var module))
            {
                var d = SaveModuleRegistry.Get(moduleId);
                if (d == null)
                    throw new InvalidOperationException($"Module '{moduleId}' not registered and not discoverable.");
                module = (SaveDataModule)Activator.CreateInstance(d.ModuleType);
                m_Modules[d.Id] = module;
            }
            await ReadModuleAsync(slotName, module);
        }

        public async Task LoadAllAsync(string slotName)
        {
            foreach (var module in m_Modules.Values)
                await ReadModuleAsync(slotName, module);
        }

        // --- Slots + manifest ----------------------------------------------------

        /// <summary>Read a slot's manifest (cached). Returns null if the slot has none yet.</summary>
        public async Task<SlotManifest> GetManifestAsync(string slotName)
        {
            if (m_ManifestCache.TryGetValue(slotName, out var cached)) return cached;
            var key = Storage.SlotKey(slotName, SlotManifest.FileName);
            var bytes = await Storage.ReadAsync(key);
            if (bytes == null) return null;

            if (SaveEnvelope.TryUnpack(bytes, Encryptor, out var env, out _))
            {
                var m = env.Data.ToObject<SlotManifest>();
                m_ManifestCache[slotName] = m;
                return m;
            }
            return null;
        }

        /// <summary>List all slots with their manifests. Slots without a manifest get a name-only stub.</summary>
        public async Task<IReadOnlyList<SlotManifest>> ListSlotsAsync()
        {
            var names = await Storage.ListSlotsAsync();
            var result = new List<SlotManifest>(names.Count);
            foreach (var n in names)
            {
                var m = await GetManifestAsync(n);
                if (m != null) result.Add(m);
                else result.Add(new SlotManifest { SlotName = n });
            }
            return result;
        }

        public async Task CreateSlotAsync(string slotName)
        {
            await Storage.CreateSlotAsync(slotName);
            var manifest = SlotManifest.CreateNew(slotName);
            m_ManifestCache[slotName] = manifest;
            await WriteManifestAsync(slotName, manifest);
        }

        public async Task DeleteSlotAsync(string slotName)
        {
            await Storage.DeleteSlotAsync(slotName);
            m_ManifestCache.Remove(slotName);
        }

        /// <summary>Notify listeners (direct callback + FSM global bus) that a recovery step occurred.</summary>
        public void RaiseRecovery(SaveRecoveryEvent ev)
        {
            try { OnRecovery?.Invoke(ev); } catch { }
            try { FSM.DispatchEvent_s(SaveRecoveryEvent.EventName, ev); } catch { }
            Debug.LogWarning(ev.ToString());
        }

        // --- Internal write/read mechanics ---------------------------------------

        // One logical save unit: persist the module file, then refresh the slot manifest.
        // This is the delegate the coalescing gate re-runs for the trailing write, so it
        // re-serializes the module's *current* state each time it is invoked.
        private async Task WriteAndTouchAsync(string slotName, SaveDataModule module, SaveModuleDescriptor d)
        {
            await WriteModuleAsync(slotName, module, d);
            await TouchManifestAsync(slotName, d);
        }

        // Serialize the module -> wrap in an envelope (checksum, header) -> hand bytes to storage.
        // The storage backend is responsible for atomic write + backup rotation.
        private async Task WriteModuleAsync(string slotName, SaveDataModule module, SaveModuleDescriptor d)
        {
            var json = Serializer.ToJObject(module);
            var header = new SaveEnvelopeHeader
            {
                ModuleId = d.Id,
                ModuleVersion = d.Version,
            };
            var encryptor = d.Encrypted ? GetOrCreateEncryptor() : null;
            var bytes = SaveEnvelope.Pack(header, json, d.Compressed, encryptor, Serializer);
            var key = Storage.SlotKey(slotName, d.Id + ".save");
            await Storage.WriteAsync(key, bytes);
        }

        // Load with a recovery cascade:
        //   1. v1 legacy file present (no envelope)?  -> parse with JsonUtility, re-save as v2.
        //   2. main file missing                      -> InitializeDefaults().
        //   3. main file present + valid              -> migrate + populate.
        //   4. main file corrupt                      -> try .bak.1, then .bak.2.
        //   5. everything failed                      -> InitializeDefaults().
        // Every fallback raises a SaveRecoveryEvent so games can surface a warning.
        private async Task ReadModuleAsync(string slotName, SaveDataModule module)
        {
            var d = SaveModuleRegistry.GetOrSynthesize(module);
            var key = Storage.SlotKey(slotName, d.Id + ".save");
            var bytes = await Storage.ReadAsync(key);

            // No v2 file yet — look for an old v1 file (e.g. "Currency.json") and migrate it forward.
            if (bytes == null && !string.IsNullOrEmpty(d.LegacyFileName))
            {
                var legacyKey = Storage.SlotKey(slotName, d.LegacyFileName);
                bytes = await Storage.ReadAsync(legacyKey);
                if (bytes != null)
                {
                    if (TryLoadLegacy(slotName, module, d, bytes, legacyKey))
                    {
                        await WriteModuleAsync(slotName, module, d);   // rewrite in v2 format
                        return;
                    }
                }
            }

            if (bytes == null)
            {
                module.InitializeDefaults();
                RaiseRecovery(new SaveRecoveryEvent
                {
                    Kind = SaveRecoveryKind.DefaultsApplied,
                    SlotName = slotName,
                    ModuleId = d.Id,
                    Detail = "No save file found; defaults applied.",
                });
                return;
            }

            // Happy path: main file parses and checksum matches.
            if (await TryLoadBytesAsync(slotName, module, d, bytes, key, attemptIndex: 0)) return;

            // Main file corrupt — walk the backups newest-first.
            if (Storage is LocalDiskStorage local)
            {
                for (int i = 1; i <= local.BackupCount; i++)
                {
                    var bak = local.ReadBackup(key, i);
                    if (bak == null) continue;
                    if (await TryLoadBytesAsync(slotName, module, d, bak, key, attemptIndex: i))
                    {
                        RaiseRecovery(new SaveRecoveryEvent
                        {
                            Kind = SaveRecoveryKind.BackupRestored,
                            SlotName = slotName,
                            ModuleId = d.Id,
                            FilePath = key + ".bak." + i,
                            Detail = "Recovered from backup " + i,
                        });
                        await WriteModuleAsync(slotName, module, d);   // promote the good backup to main
                        return;
                    }
                }
            }

            // Nothing usable anywhere.
            module.InitializeDefaults();
            RaiseRecovery(new SaveRecoveryEvent
            {
                Kind = SaveRecoveryKind.DefaultsApplied,
                SlotName = slotName,
                ModuleId = d.Id,
                FilePath = key,
                Detail = "All recovery paths failed; defaults applied.",
            });
        }

        // Parse a v1 (pre-envelope) file with the legacy JsonUtility reader.
        private bool TryLoadLegacy(string slotName, SaveDataModule module, SaveModuleDescriptor d, byte[] bytes, string key)
        {
            if (!LegacyV1Reader.IsLegacy(bytes)) return false;
            if (!LegacyV1Reader.TryPopulate(bytes, module, out var err))
            {
                RaiseRecovery(new SaveRecoveryEvent
                {
                    Kind = SaveRecoveryKind.SerializerFailed,
                    SlotName = slotName,
                    ModuleId = d.Id,
                    FilePath = key,
                    Detail = err,
                });
                return false;
            }
            RaiseRecovery(new SaveRecoveryEvent
            {
                Kind = SaveRecoveryKind.LegacyV1Migrated,
                SlotName = slotName,
                ModuleId = d.Id,
                FilePath = key,
                Detail = "Legacy v1 file detected and migrated.",
            });
            return true;
        }

        // Unpack one blob of bytes into the module. Handles legacy sniffing, checksum verification,
        // and the stepwise schema migration loop. Returns false (without throwing) on any failure so
        // the caller can move on to the next backup.
        private Task<bool> TryLoadBytesAsync(string slotName, SaveDataModule module, SaveModuleDescriptor d, byte[] bytes, string key, int attemptIndex)
        {
            var encryptor = d.Encrypted ? GetOrCreateEncryptor() : null;

            // A backup could itself be an un-migrated v1 file.
            if (LegacyV1Reader.IsLegacy(bytes))
            {
                var ok = TryLoadLegacy(slotName, module, d, bytes, key);
                return Task.FromResult(ok);
            }

            if (!SaveEnvelope.TryUnpack(bytes, encryptor, out var env, out var error))
            {
                RaiseRecovery(new SaveRecoveryEvent
                {
                    Kind = SaveRecoveryKind.ChecksumMismatch,
                    SlotName = slotName,
                    ModuleId = d.Id,
                    FilePath = key + (attemptIndex > 0 ? ".bak." + attemptIndex : ""),
                    Detail = error,
                });
                return Task.FromResult(false);
            }

            try
            {
                var dataObj = (JObject)env.Data;
                // Apply migrations one version at a time until the payload matches the current schema.
                var fromVersion = env.Header.ModuleVersion;
                while (fromVersion < d.Version)
                {
                    module.Migrate(dataObj, fromVersion);
                    fromVersion++;
                }
                Serializer.PopulateFromJObject(dataObj, module);
                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                RaiseRecovery(new SaveRecoveryEvent
                {
                    Kind = SaveRecoveryKind.SerializerFailed,
                    SlotName = slotName,
                    ModuleId = d.Id,
                    FilePath = key,
                    Detail = e.Message,
                });
                return Task.FromResult(false);
            }
        }

        // Update the slot manifest (playtime/timestamp/module versions) after a module save.
        private async Task TouchManifestAsync(string slotName, SaveModuleDescriptor d)
        {
            if (!m_ManifestCache.TryGetValue(slotName, out var manifest))
            {
                manifest = await GetManifestAsync(slotName) ?? SlotManifest.CreateNew(slotName);
                m_ManifestCache[slotName] = manifest;
            }
            manifest.Modules[d.Id] = d.Version;
            manifest.Touch();
            // The manifest is one per-slot file shared by every module, so route its
            // write through a per-slot gate too — otherwise two different modules
            // saving to the same slot at once would clobber each other's manifest.
            await RunCoalescedAsync(ManifestGateKey(slotName), () => WriteManifestAsync(slotName, manifest));
        }

        // The manifest is itself an enveloped+checksummed file (never encrypted, always compressed).
        private async Task WriteManifestAsync(string slotName, SlotManifest manifest)
        {
            var header = new SaveEnvelopeHeader
            {
                ModuleId = SlotManifest.ModuleId,
                ModuleVersion = 1,
            };
            var json = Serializer.ToJObject(manifest);
            var bytes = SaveEnvelope.Pack(header, json, compress: true, encryptor: null, serializer: Serializer);
            var key = Storage.SlotKey(slotName, SlotManifest.FileName);
            await Storage.WriteAsync(key, bytes);
        }

        // Build (and cache) the AES encryptor from whatever key provider is configured.
        private ISaveEncryptor GetOrCreateEncryptor()
        {
            if (Encryptor != null) return Encryptor;
            Encryptor = new AesEncryption(KeyProvider ??= new DefaultKeyProvider());
            return Encryptor;
        }

        // --- Concurrent-save coalescing ------------------------------------------
        //
        // WriteModuleAsync serializes the module synchronously, but the actual file
        // I/O (Storage.WriteAsync) runs on a thread-pool thread. So two SaveAsync
        // calls for the same slot+module would run the atomic write on the SAME path
        // at once — the second open of the ".tmp" file (FileShare.None) throws, or the
        // backup-rotation moves interleave and shred the .bak chain. Net effect: one
        // of the two saves is silently lost.
        //
        // The gate guarantees at most one write per key runs at a time. Requests that
        // arrive while a write is in flight do NOT each queue their own write — they
        // COALESCE into a single trailing write. Because every save re-reads the
        // module's *current* state, one write after the burst already holds the
        // latest data, making the intermediate writes redundant (the "keep the last
        // one" behaviour). Each caller's returned Task still completes only after a
        // write that started at or after its request finished, so `await SaveAsync`
        // remains a real "it's on disk now" guarantee.
        //
        // NOTE: gate state is only touched from async continuations that resume on
        // Unity's main thread, so no locking is needed — provided save/load calls
        // originate on the main thread (the normal case, and the same assumption the
        // rest of the system already makes).

        private sealed class CoalescingGate
        {
            public bool Redirty;                          // a save was requested while the active write ran
            public TaskCompletionSource<bool> Trailing;   // shared completion for the coalesced trailing write
        }

        private static string ModuleGateKey(string slotName, string moduleId) => "m\0" + slotName + "\0" + moduleId;
        private static string ManifestGateKey(string slotName) => "s\0" + slotName;

        // Run <paramref name="work"/> so that only one invocation per <paramref name="key"/>
        // is in flight at a time, and any calls arriving during a run fold into a single
        // trailing run. Returns a Task that completes once a run covering the caller's
        // request has finished (or faults if that run threw).
        private Task RunCoalescedAsync(string key, Func<Task> work)
        {
            if (m_SaveGates.TryGetValue(key, out var gate))
            {
                // A write is already running for this key. Fold this request into the
                // single trailing write instead of starting a competing one.
                gate.Redirty = true;
                gate.Trailing ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                return gate.Trailing.Task;
            }

            gate = new CoalescingGate();
            m_SaveGates[key] = gate;
            return DrainAsync(key, gate, work);
        }

        private async Task DrainAsync(string key, CoalescingGate gate, Func<Task> work)
        {
            try
            {
                await work();   // first pass: the state captured by the caller that opened the gate

                // Collapse everything requested during the write into one more pass.
                while (gate.Redirty)
                {
                    gate.Redirty = false;
                    var trailing = gate.Trailing;
                    gate.Trailing = null;
                    try { await work(); }
                    catch (Exception e) { trailing?.TrySetException(e); throw; }
                    trailing?.TrySetResult(true);
                }
            }
            catch (Exception e)
            {
                // Don't leave a folded-in request awaiting forever if a pass failed.
                gate.Trailing?.TrySetException(e);
                throw;
            }
            finally
            {
                m_SaveGates.Remove(key);
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace BB.Framework 
{
    /// <summary>
    /// Centralized save/load system that manages modular save data.
    /// Implements the Singleton pattern and persists across scene loads.
    /// Each save module can be independently saved and loaded.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        /// <summary>
        /// Gets the singleton instance of the SaveManager.
        /// </summary>
        public static SaveManager Instance { get; private set; }

        private readonly Dictionary<ESaveModule, SaveDataModule> _saveModules = new Dictionary<ESaveModule, SaveDataModule>();

        private void Awake()
        {
            // Ensure only one instance exists
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[SaveManager] Duplicate instance detected. Destroying new instance.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Registers a save data module with the system.
        /// Once registered, the module can be saved and loaded via slot names.
        /// </summary>
        /// <param name="module">The module to register. Cannot be null.</param>
        public void RegisterModule(SaveDataModule module)
        {
            if (module == null)
            {
                Debug.LogError("[SaveManager] Cannot register null module");
                return;
            }

            if (!_saveModules.ContainsKey(module.savemodule))
            {
                _saveModules[module.savemodule] = module;
                Debug.Log($"[SaveManager] Registered module: {module.savemodule}");
            }
            else
            {
                Debug.LogWarning($"[SaveManager] Module {module.savemodule} is already registered");
            }
        }

        /// <summary>
        /// Unregisters a save data module from the system.
        /// </summary>
        /// <param name="moduleType">The type of module to unregister</param>
        public void UnregisterModule(ESaveModule moduleType)
        {
            if (_saveModules.ContainsKey(moduleType))
            {
                _saveModules.Remove(moduleType);
                Debug.Log($"[SaveManager] Unregistered module: {moduleType}");
            }
        }

        /// <summary>
        /// Saves a specific module to the specified save slot.
        /// </summary>
        /// <param name="slotName">The name of the save slot. Cannot be null or empty.</param>
        /// <param name="moduleType">The type of module to save</param>
        public void SaveModule(string slotName, ESaveModule moduleType)
        {
            if (string.IsNullOrWhiteSpace(slotName))
            {
                Debug.LogError("[SaveManager] Cannot save: slotName is null or empty");
                return;
            }

            if (SaveSlotManager.Instance == null)
            {
                Debug.LogError("[SaveManager] SaveSlotManager.Instance is null. Cannot get slot path.");
                return;
            }

            if (_saveModules.TryGetValue(moduleType, out var module))
            {
                string slotPath = SaveSlotManager.Instance.GetSlotPath(slotName);
                if (!string.IsNullOrEmpty(slotPath))
                {
                    module.SaveOnDemand(slotPath);
                    Debug.Log($"[SaveManager] Saved module {moduleType} to slot '{slotName}'");
                }
                else
                {
                    Debug.LogError($"[SaveManager] Failed to get slot path for '{slotName}'");
                }
            }
            else
            {
                Debug.LogWarning($"[SaveManager] Module {moduleType} not found. Cannot save.");
            }
        }

        /// <summary>
        /// Saves all registered modules to the specified save slot.
        /// </summary>
        /// <param name="slotName">The name of the save slot. Cannot be null or empty.</param>
        public void SaveAllModule(string slotName)
        {
            if (string.IsNullOrWhiteSpace(slotName))
            {
                Debug.LogError("[SaveManager] Cannot save all: slotName is null or empty");
                return;
            }

            if (SaveSlotManager.Instance == null)
            {
                Debug.LogError("[SaveManager] SaveSlotManager.Instance is null. Cannot get slot path.");
                return;
            }

            string slotPath = SaveSlotManager.Instance.GetSlotPath(slotName);
            if (string.IsNullOrEmpty(slotPath))
            {
                Debug.LogError($"[SaveManager] Failed to get slot path for '{slotName}'");
                return;
            }

            int savedCount = 0;
            foreach (SaveDataModule module in _saveModules.Values)
            {
                if (module != null)
                {
                    module.SaveOnDemand(slotPath);
                    savedCount++;
                }
            }

            Debug.Log($"[SaveManager] Saved {savedCount} modules to slot '{slotName}'");
        }

        /// <summary>
        /// Loads a specific module from the specified save slot.
        /// </summary>
        /// <param name="slotName">The name of the save slot. Cannot be null or empty.</param>
        /// <param name="moduleType">The type of module to load</param>
        public void LoadModule(string slotName, ESaveModule moduleType)
        {
            if (string.IsNullOrWhiteSpace(slotName))
            {
                Debug.LogError("[SaveManager] Cannot load: slotName is null or empty");
                return;
            }

            if (SaveSlotManager.Instance == null)
            {
                Debug.LogError("[SaveManager] SaveSlotManager.Instance is null. Cannot get slot path.");
                return;
            }

            if (_saveModules.TryGetValue(moduleType, out var module))
            {
                string slotPath = SaveSlotManager.Instance.GetSlotPath(slotName);
                if (!string.IsNullOrEmpty(slotPath))
                {
                    module.LoadOnDemand(slotPath);
                    Debug.Log($"[SaveManager] Loaded module {moduleType} from slot '{slotName}'");
                }
                else
                {
                    Debug.LogError($"[SaveManager] Failed to get slot path for '{slotName}'");
                }
            }
            else
            {
                Debug.LogWarning($"[SaveManager] Module {moduleType} not found. Cannot load.");
            }
        }

        /// <summary>
        /// Loads all registered modules from the specified save slot.
        /// </summary>
        /// <param name="slotName">The name of the save slot. Cannot be null or empty.</param>
        public void LoadAllModule(string slotName)
        {
            if (string.IsNullOrWhiteSpace(slotName))
            {
                Debug.LogError("[SaveManager] Cannot load all: slotName is null or empty");
                return;
            }

            if (SaveSlotManager.Instance == null)
            {
                Debug.LogError("[SaveManager] SaveSlotManager.Instance is null. Cannot get slot path.");
                return;
            }

            string slotPath = SaveSlotManager.Instance.GetSlotPath(slotName);
            if (string.IsNullOrEmpty(slotPath))
            {
                Debug.LogError($"[SaveManager] Failed to get slot path for '{slotName}'");
                return;
            }

            int loadedCount = 0;
            foreach (SaveDataModule module in _saveModules.Values)
            {
                if (module != null)
                {
                    module.LoadOnDemand(slotPath);
                    loadedCount++;
                }
            }

            Debug.Log($"[SaveManager] Loaded {loadedCount} modules from slot '{slotName}'");
        }

        /// <summary>
        /// Retrieves a registered save module for direct access.
        /// Useful for reading or modifying save data before saving.
        /// </summary>
        /// <typeparam name="T">The specific SaveDataModule type</typeparam>
        /// <param name="moduleType">The type of module to retrieve</param>
        /// <returns>The module instance, or null if not found</returns>
        public T GetModule<T>(ESaveModule moduleType) where T : SaveDataModule
        {
            if (_saveModules.TryGetValue(moduleType, out var module))
            {
                return module as T;
            }

            Debug.LogWarning($"[SaveManager] Module {moduleType} not found or wrong type");
            return null;
        }

        /// <summary>
        /// Checks if a module is registered with the system.
        /// </summary>
        /// <param name="moduleType">The module type to check</param>
        /// <returns>True if the module is registered, false otherwise</returns>
        public bool IsModuleRegistered(ESaveModule moduleType)
        {
            return _saveModules.ContainsKey(moduleType);
        }

        /// <summary>
        /// Gets the number of registered modules.
        /// </summary>
        /// <returns>The count of registered modules</returns>
        public int GetRegisteredModuleCount()
        {
            return _saveModules.Count;
        }
    }
}
}


/*
 * 
 * the way to use :
SaveSlotManager.Instance.CreateGameSlot("Game1");

CurrencySaveModule currencyModule = new CurrencySaveModule();
SaveManager.Instance.RegisterModule(currencyModule);

SaveManager.Instance.LoadModule("Game1", ESaveModule.ECurrency);


var currency = SaveManager.Instance.GetModule<CurrencySaveModule>(ESaveModule.ECurrency);
if (currency != null)
{
    currency.AddCoins(50);
    currency.AddGems(10);

    // Save the updated data
    SaveManager.Instance.SaveModule("Game1", ESaveModule.ECurrency);
}


 * 
 */
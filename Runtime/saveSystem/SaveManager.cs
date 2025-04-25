using System.Collections.Generic;
using UnityEngine;


public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private Dictionary<ESaveModule, SaveDataModule> saveModules = new Dictionary<ESaveModule, SaveDataModule>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void RegisterModule(SaveDataModule module)
    {
        if (!saveModules.ContainsKey(module.savemodule))
        {
            saveModules[module.savemodule] = module;
        }
    }

    // Save a specific module on demand
    public void SaveModule(string slotName, ESaveModule moduleType)
    {
        if (saveModules.TryGetValue(moduleType, out var module))
        {
            string slotPath = SaveSlotManager.Instance.GetSlotPath(slotName);
            module.SaveOnDemand(slotPath);
        }
    }
    
    //save all
    public void SaveAllModule(string slotName)
    {
        foreach (SaveDataModule module in saveModules.Values)
        {
            string slotPath = SaveSlotManager.Instance.GetSlotPath(slotName);
            module.SaveOnDemand(slotPath);
        }
    }

    // Load a specific module on demand
    public void LoadModule(string slotName, ESaveModule moduleType)
    {
        if (saveModules.TryGetValue(moduleType, out var module))
        {
            string slotPath = SaveSlotManager.Instance.GetSlotPath(slotName);
            module.LoadOnDemand(slotPath);
        }
    }

    //save all
    public void LoadAllModule(string slotName)
    {
        foreach (SaveDataModule module in saveModules.Values)
        {
            string slotPath = SaveSlotManager.Instance.GetSlotPath(slotName);
            module.LoadOnDemand(slotPath);
        }
    }

    // Retrieve a specific module for use in the game
    public T GetModule<T>(ESaveModule moduleType) where T : SaveDataModule
    {
        if (saveModules.TryGetValue(moduleType, out var module))
        {
            return module as T;
        }
        return null;
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
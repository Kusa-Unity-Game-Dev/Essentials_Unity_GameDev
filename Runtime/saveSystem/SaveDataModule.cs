using System.IO;
using UnityEngine;

public abstract class SaveDataModule
{
    public abstract string FileName { get; }
    public abstract ESaveModule savemodule { get; }

    // Optional: A hook to initialize default values if no data exists
    public virtual void InitializeDefaults() { }

    // New: Load this module on demand
    public void LoadOnDemand(string slotPath)
    {
        string fullPath = Path.Combine(slotPath, FileName);
        if (File.Exists(fullPath))
        {
            string json = File.ReadAllText(fullPath);
            JsonUtility.FromJsonOverwrite(json, this);
            Debug.Log($"Module '{savemodule}' loaded on demand from {fullPath}");
        }
        else
        {
            Debug.LogWarning($"Module file '{FileName}' not found. Using default values.");
            InitializeDefaults();
            SaveOnDemand(slotPath);
        }
    }

    // New: Save this module on demand
    public void SaveOnDemand(string slotPath)
    {
        string fullPath = Path.Combine(slotPath, FileName);
        string json = JsonUtility.ToJson(this);
        File.WriteAllText(fullPath, json);
        Debug.Log($"Module '{savemodule}' saved on demand to {fullPath}");
    }
}


/*public enum ESaveModule
{
    ECurrency = 0,
    EGameData,

    EG1_ballJump =100,
    EG2_EggBasket,
    EG3_GameSaveData,
    EG4_GameSaveData,
}*/

/*
 * how to use the system : 
using System.IO;
using UnityEngine;

[System.Serializable]
public class CurrencySaveModule : SaveDataModule
{
    public override string FileName => "Currency.json";

    public int Coins;
    public int Gems;

    public override void OnSave(string path)
    {
        string fullPath = Path.Combine(path, FileName);
        string json = JsonUtility.ToJson(this);
        File.WriteAllText(fullPath, json);
        Debug.Log($"Currency saved to {fullPath}");
    }

    public override void OnLoad(string path)
    {
        string fullPath = Path.Combine(path, FileName);
        if (File.Exists(fullPath))
        {
            string json = File.ReadAllText(fullPath);
            JsonUtility.FromJsonOverwrite(json, this);
            Debug.Log($"Currency loaded from {fullPath}");
        }
        else
        {
            Debug.LogWarning("Currency file not found.");
        }
    }
}

 */
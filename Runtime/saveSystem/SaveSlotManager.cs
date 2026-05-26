using System;
using System.Collections.Generic;
using System.IO;
using BB.Framework.SaveV2;
using UnityEngine;

namespace BB.Framework {

[Obsolete("Use BB.Framework.SaveV2.SaveSystem instead. SaveSlotManager is a v1 compatibility facade.")]
public class SaveSlotManager : MonoBehaviour
{
    public static SaveSlotManager Instance { get; private set; }

    private const string SaveDirectory = "GameSlots";
    private const string SlotPrefix = "GS_";

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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void DeleteGameSlot(string slotName)
    {
        SaveSystem.EnsureInstance().DeleteSlotAsync(slotName).GetAwaiter().GetResult();
    }

    public string CreateGameSlot(string slotName)
    {
        SaveSystem.EnsureInstance().CreateSlotAsync(slotName).GetAwaiter().GetResult();
        return GetSlotPath(slotName);
    }

    public List<string> GetAvailableSlots()
    {
        string directory = Path.Combine(Application.persistentDataPath, SaveDirectory);
        if (!Directory.Exists(directory)) return new List<string>();

        var slots = new List<string>();
        foreach (var dir in Directory.GetDirectories(directory))
        {
            slots.Add(Path.GetFileName(dir));
        }
        return slots;
    }

    public string GetSlotPath(string slotName)
    {
        return Path.Combine(Application.persistentDataPath, SaveDirectory, SlotPrefix + slotName);
    }
}
}

using System;
using System.Collections.Generic;
using BB.Framework.SaveV2;
using UnityEngine;

namespace BB.Framework {

[Obsolete("Use BB.Framework.SaveV2.SaveSystem instead. SaveManager is a v1 compatibility facade.")]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private readonly Dictionary<ESaveModule, string> m_EnumToId = new();

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

    private static SaveSystem Sys => SaveSystem.EnsureInstance();

    public void RegisterModule(SaveDataModule module)
    {
        if (module == null) return;
        Sys.RegisterModule(module);
        var d = SaveModuleRegistry.GetOrSynthesize(module);
        m_EnumToId[module.savemodule] = d.Id;
    }

    public void SaveModule(string slotName, ESaveModule moduleType)
    {
        if (!m_EnumToId.TryGetValue(moduleType, out var id)) return;
        Sys.SaveAsync(slotName, id).GetAwaiter().GetResult();
    }

    public void SaveAllModule(string slotName)
    {
        Sys.SaveAllAsync(slotName).GetAwaiter().GetResult();
    }

    public void LoadModule(string slotName, ESaveModule moduleType)
    {
        if (!m_EnumToId.TryGetValue(moduleType, out var id)) return;
        Sys.LoadAsync(slotName, id).GetAwaiter().GetResult();
    }

    public void LoadAllModule(string slotName)
    {
        Sys.LoadAllAsync(slotName).GetAwaiter().GetResult();
    }

    public T GetModule<T>(ESaveModule moduleType) where T : SaveDataModule
    {
        if (!m_EnumToId.TryGetValue(moduleType, out var id)) return null;
        return Sys.GetModule<T>(id);
    }
}
}

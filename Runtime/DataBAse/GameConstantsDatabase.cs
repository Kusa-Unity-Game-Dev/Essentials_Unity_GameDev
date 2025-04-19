using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameConstantsDatabase", menuName = "kusa/GameData/Game Constants Database")]
public class GameConstantsDatabase : ScriptableObject
{
    [Serializable]
    public class GameConstant
    {
        public string ID;     
        public string Value;  
    }

    [SerializeField] private List<GameConstant> constants = new List<GameConstant>();

    private static Dictionary<string, string> lookupTable;

    private void OnEnable()
    {
        InitializeLookup();
    }

    private void InitializeLookup()
    {
        if (lookupTable == null)
        {
            lookupTable = new Dictionary<string, string>();
            foreach (var constant in constants)
            {
                if (!lookupTable.ContainsKey(constant.ID))
                    lookupTable.Add(constant.ID, constant.Value);
            }
        }
    }

    public static string GetValue(string id)
    {
        if (lookupTable != null && lookupTable.TryGetValue(id, out string value))
            return value;

        Debug.LogWarning($"Key '{id}' not found in GameConstantsDatabase.");
        return string.Empty;
    }
}

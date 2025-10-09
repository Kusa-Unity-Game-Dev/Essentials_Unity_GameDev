using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BB.Framework
{
    [CreateAssetMenu(fileName = "GameConstantsDatabase", menuName = "kusa/GameData/Game Constants Database")]
    public class GameConstantsDatabase : ScriptableObject
    {
        [Serializable]
        public class GameConstant
        {
            public string ID;
            public Object Value;
        }

        [SerializeField] private List<GameConstant> constants = new List<GameConstant>();

        private static Dictionary<string, Object> lookupTable;

        private void OnEnable()
        {
            InitializeLookup();
        }

        private void InitializeLookup()
        {
            if (lookupTable == null)
            {
                lookupTable = new Dictionary<string, Object>();
                foreach (var constant in constants)
                {
                    if (!lookupTable.ContainsKey(constant.ID))
                        lookupTable.Add(constant.ID, constant.Value);
                }
            }
        }

        public static Object GetValue(string id)
        {
            if (lookupTable != null && lookupTable.TryGetValue(id, out Object value))
                return value;

            Debug.LogWarning($"Key '{id}' not found in GameConstantsDatabase.");
            return null;
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BB.Framework
{
    [CreateAssetMenu(fileName = "PersistenceConfig", menuName = "kusa/RuntimeData/Persistence Config")]
    public sealed class PersistenceConfig : ScriptableObject
    {
        [Tooltip("PlayerPrefs key to store serialized values")]
        public string playerPrefsKey = "RUNTIME_DATA_STORE";

        public IPersistenceProvider Create() => new PlayerPrefsJsonProvider(playerPrefsKey);
    }

    public sealed class PlayerPrefsJsonProvider : IPersistenceProvider
    {
        private readonly string _key;
        public PlayerPrefsJsonProvider(string key) { _key = key; }

        [Serializable]
        private class DictWrapper
        {
            public string[] keys;
            public string[] values;

            public static DictWrapper From(Dictionary<string, string> d)
            {
                var w = new DictWrapper
                {
                    keys = new string[d.Count],
                    values = new string[d.Count]
                };
                int i = 0;
                foreach (var kv in d) { w.keys[i] = kv.Key; w.values[i] = kv.Value; i++; }
                return w;
            }

            public Dictionary<string, string> ToDict()
            {
                var d = new Dictionary<string, string>(keys?.Length ?? 0);
                for (int i = 0; i < keys.Length; i++) d[keys[i]] = values[i];
                return d;
            }
        }

        public bool TryLoad(out Dictionary<string, string> blob)
        {
            if (!PlayerPrefs.HasKey(_key)) { blob = null; return false; }
            var json = PlayerPrefs.GetString(_key);
            var wrap = JsonUtility.FromJson<DictWrapper>(json);
            blob = wrap.ToDict();
            return true;
        }

        public void Save(Dictionary<string, string> blob)
        {
            var wrap = DictWrapper.From(blob);
            var json = JsonUtility.ToJson(wrap);
            PlayerPrefs.SetString(_key, json);
            PlayerPrefs.Save();
        }
    }
}

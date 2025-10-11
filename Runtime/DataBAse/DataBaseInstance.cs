using UnityEngine;

namespace BB.Framework{
    
    public enum DataBaseType : byte
    {
        String,
        Float,
        GameObject,
        Object,
        Bool
    }
    
    public class DataBaseInstance : MonoBehaviour
    {
        private static DataBaseInstance s_Instance;
        public GameConstantsDatabase<string> m_GameDataBaseString;
        public GameConstantsDatabase<float> m_GameDataBaseFloat;
        public GameConstantsDatabase<GameObject> m_GameDataBaseGameObject;
        public GameConstantsDatabase<Object> m_GameDataBaseObject;
        public GameConstantsDatabase<bool> m_GameDataBaseBool;
        
        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                s_Instance = this;
            }
        }

        /*public static GameConstantsDatabase trygetGameConst()
        {
            if (!s_Instance && !s_Instance.m_GameDataBase) return null;
            return s_Instance.m_GameDataBase;
        }*/
        
        public static GameConstantsDatabase<T> TryGetGameConst<T>(DataBaseType type)
        {
            if (!s_Instance) return null;
            return type switch
            {
                DataBaseType.String => s_Instance.m_GameDataBaseString as GameConstantsDatabase<T>,
                DataBaseType.Float => s_Instance.m_GameDataBaseFloat as GameConstantsDatabase<T>,
                DataBaseType.GameObject => s_Instance.m_GameDataBaseGameObject as GameConstantsDatabase<T>,
                DataBaseType.Object => s_Instance.m_GameDataBaseObject as GameConstantsDatabase<T>,
                DataBaseType.Bool => s_Instance.m_GameDataBaseBool as GameConstantsDatabase<T>,
                _ => null
            };
        }
        
        public static T GetValue<T>(string id, DataBaseType type)
        {
            var db = TryGetGameConst<T>(type);
            if (db == null)
            {
                Debug.LogWarning($"Database for type '{type}' is not available.");
                return default;
            }
            return db.GetValue(id);
        }
        

        //public static 

    }	
}
using UnityEngine;

public class DataBaseInstance : MonoBehaviour
{
    private static DataBaseInstance s_Instance;
    public GameConstantsDatabase m_GameDataBase;


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

    public static GameConstantsDatabase trygetGameConst()
    {
        if (!s_Instance && !s_Instance.m_GameDataBase) return null;
        return s_Instance.m_GameDataBase;
    }

    //public static 

}

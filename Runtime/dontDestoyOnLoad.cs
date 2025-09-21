using UnityEngine;

namespace BB.Framework
{

    public class dontDestoyOnLoad : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
using UnityEngine;

namespace BB.Framework
{
    /// <summary>
    /// Thread-safe singleton MonoBehaviour base class.
    /// Inherit from this class to create a singleton component that persists across scenes.
    /// Automatically handles duplicate instances and provides thread-safe access.
    /// </summary>
    /// <typeparam name="T">The type of the singleton class</typeparam>
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static readonly object _lock = new object();
        private static T _instance;
        private static bool _isQuitting = false;

        /// <summary>
        /// Gets the singleton instance of this MonoBehaviour.
        /// Thread-safe and null-safe implementation.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_isQuitting)
                {
                    Debug.LogWarning($"[SingletonMonoBehaviour] Instance of '{typeof(T)}' already destroyed on application quit. Returning null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        // Try to find existing instance
                        _instance = FindObjectOfType<T>();

                        if (_instance == null)
                        {
                            Debug.LogWarning($"[SingletonMonoBehaviour] No instance of '{typeof(T)}' found. Create one in the scene or use a factory method.");
                        }
                        else
                        {
                            Debug.Log($"[SingletonMonoBehaviour] Found existing instance of '{typeof(T)}'");
                        }
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Checks if an instance of this singleton exists.
        /// </summary>
        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                OnSingletonAwake();
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[SingletonMonoBehaviour] Duplicate instance of '{typeof(T)}' detected. Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                OnSingletonDestroy();
                _instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        /// <summary>
        /// Called when the singleton is first initialized.
        /// Override this instead of Awake() in derived classes.
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        /// <summary>
        /// Called when the singleton is destroyed.
        /// Override this instead of OnDestroy() in derived classes.
        /// </summary>
        protected virtual void OnSingletonDestroy() { }
    }
}

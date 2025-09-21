using UnityEngine;

namespace BB.Framework
{
    /// <summary>
    /// Single entry point you add to a bootstrap scene.
    /// Exposes IValueStore via Service property.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class RuntimeDataHub : MonoBehaviour
    {
        public static IValueStore Service { get; private set; }

        [SerializeField] private bool dontDestroyOnLoad = false;
        [SerializeField] private bool autoLoadOnAwake = true;
        [SerializeField] private PersistenceConfig2 persistenceConfig;

        private ValueStore _store;

        private void Awake()
        {
            // Only one hub
            if (Service != null && Service is Object so && so != this)
            {
                Destroy(gameObject);
                return;
            }

            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

            var provider = persistenceConfig != null
                ? persistenceConfig.Create()
                : new PlayerPrefsJsonProvider("RUNTIME_DATA_STORE");

            _store = new ValueStore(provider);
            Service = _store;

            if (autoLoadOnAwake)
                _store.LoadPersistent();

            _store.HookLifecycle(); // scene/match cleanup hooks
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Service, _store)) Service = null;
        }
    }
}
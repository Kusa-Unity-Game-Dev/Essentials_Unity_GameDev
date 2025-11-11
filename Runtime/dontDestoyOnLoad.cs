using UnityEngine;

namespace BB.Framework
{
    /// <summary>
    /// Simple component that marks a GameObject to persist across scene loads.
    /// Attach this to any GameObject that should not be destroyed when loading new scenes.
    /// Note: Consider using this sparingly as persisted objects can accumulate over time.
    /// </summary>
    public class dontDestoyOnLoad : MonoBehaviour
    {
        [Tooltip("If true, prevents duplicate instances of this GameObject")]
        [SerializeField] private bool preventDuplicates = false;

        [Tooltip("Tag used to identify this GameObject for duplicate checking")]
        [SerializeField] private string uniqueTag = "";

        private void Awake()
        {
            // Check for duplicates if enabled
            if (preventDuplicates && !string.IsNullOrEmpty(uniqueTag))
            {
                GameObject[] existingObjects = GameObject.FindGameObjectsWithTag(uniqueTag);
                if (existingObjects.Length > 1)
                {
                    Debug.LogWarning($"[DontDestroyOnLoad] Duplicate GameObject with tag '{uniqueTag}' detected. Destroying this instance.");
                    Destroy(gameObject);
                    return;
                }
            }

            DontDestroyOnLoad(gameObject);
        }
    }
}
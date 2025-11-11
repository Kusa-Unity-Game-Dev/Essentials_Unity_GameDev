using System.Collections.Generic;
using UnityEngine;

namespace BB.Framework 
{
    /// <summary>
    /// Centralized UI management system that handles UI lifecycle, visibility, and stacking.
    /// Implements the Singleton pattern to ensure only one instance exists.
    /// Supports UI stacking and maintains a history of shown screens.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        /// <summary>
        /// Gets the singleton instance of the UIManager.
        /// </summary>
        public static UIManager Instance { get; private set; }

        /// <summary>
        /// List of currently visible UI screens.
        /// </summary>
        public List<UIBase> uiScreens = new List<UIBase>();

        /// <summary>
        /// History of recently shown UI screens (limited to LAST_UI_REMEMBER_LIST).
        /// </summary>
        public List<UIBase> uiLastShownScreens = new List<UIBase>();

        /// <summary>
        /// Stack of UI screens that were hidden but can be restored.
        /// </summary>
        public List<UIBase> ui_stackedScreens = new List<UIBase>();

        private const short LAST_UI_REMEMBER_LIST = 10;

        private void Awake()
        {
            // Ensure only one instance exists
            if (Instance == null)
            {
                Instance = this;
                // Uncomment if you want UIManager to persist between scenes
                // DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.LogWarning("[UIManager] Duplicate instance detected. Destroying new instance.");
                Destroy(gameObject);
                return;
            }

            _onAwake();
        }

        private void Start()
        {
            _onStart();
            // Optional: Hook into FSM for clearing UI on specific events
            // FSM.AddListener_s(GameConstants.E__CLEARUI, privateClearAllData);
        }

        private void OnDestroy()
        {
            // Clean up singleton reference
            if (Instance == this)
            {
                Instance = null;
            }

            _onDestroy();
            // Optional: Remove FSM listener
            // FSM.RemoveListener_s(GameConstants.E__CLEARUI, privateClearAllData);
        }

        /// <summary>
        /// Registers a UI screen as currently active/visible.
        /// </summary>
        /// <param name="uiScreen">The UI screen to register. Cannot be null.</param>
        public void RegisterUI(UIBase uiScreen)
        {
            if (uiScreen == null)
            {
                Debug.LogError("[UIManager] Cannot register null UI screen");
                return;
            }

            if (!uiScreens.Contains(uiScreen))
            {
                uiScreens.Add(uiScreen);
            }
        }

        /// <summary>
        /// Unregisters a UI screen and optionally manages UI stacking.
        /// </summary>
        /// <param name="uiScreen">The UI screen to unregister. Cannot be null.</param>
        /// <param name="addToStack">If true, adds this UI to the stack for later restoration.</param>
        public void UnregisterUI(UIBase uiScreen, bool addToStack = false)
        {
            if (uiScreen == null)
            {
                Debug.LogError("[UIManager] Cannot unregister null UI screen");
                return;
            }

            if (uiScreens.Contains(uiScreen))
            {
                uiScreens.Remove(uiScreen);
            }

            // Restore previous stacked UI if allowed
            if (uiScreen.isAllowedToStack && !addToStack)
            {
                if (ui_stackedScreens.Count > 0)
                {
                    UIBase previousUI = ui_stackedScreens[ui_stackedScreens.Count - 1];
                    ui_stackedScreens.RemoveAt(ui_stackedScreens.Count - 1);
                    
                    if (previousUI != null)
                    {
                        ShowUI(previousUI);
                    }
                }
            }

            // Add to stack if requested
            if (addToStack)
            {
                ui_stackedScreens.Add(uiScreen);
            }

            // Add to history
            uiLastShownScreens.Add(uiScreen);

            // Maintain history size limit
            if (uiLastShownScreens.Count >= LAST_UI_REMEMBER_LIST)
            {
                uiLastShownScreens.RemoveAt(0);
            }
        }

        /// <summary>
        /// Shows a UI screen with optional delay.
        /// </summary>
        /// <param name="uiScreen">The UI screen to show. Cannot be null.</param>
        /// <param name="delay">Optional delay before showing the UI.</param>
        public void ShowUI(UIBase uiScreen, float delay = 0)
        {
            if (uiScreen == null)
            {
                Debug.LogError("[UIManager] Cannot show null UI screen");
                return;
            }

            uiScreen._UIBaseShowUI(delay);
            RegisterUI(uiScreen);
        }

        /// <summary>
        /// Hides a UI screen.
        /// </summary>
        /// <param name="uiScreen">The UI screen to hide. Cannot be null.</param>
        public void HideUI(UIBase uiScreen)
        {
            if (uiScreen == null)
            {
                Debug.LogError("[UIManager] Cannot hide null UI screen");
                return;
            }

            UnregisterUI(uiScreen);
            uiScreen._UIBaseHideUI();
        }

        /// <summary>
        /// Hides all currently visible UI screens.
        /// Creates a copy of the list to avoid modification during iteration.
        /// </summary>
        public void HideAllUI()
        {
            // Create a copy to avoid modification during iteration
            var screensToHide = new List<UIBase>(uiScreens);
            
            foreach (var ui in screensToHide)
            {
                if (ui != null)
                {
                    HideUI(ui);
                }
            }
        }

        /// <summary>
        /// Clears all UI tracking lists. Use with caution.
        /// </summary>
        public static void ClearAllList_s()
        {
            if (Instance == null)
            {
                Debug.LogWarning("[UIManager] Cannot clear lists: Instance is null");
                return;
            }

            Instance.uiScreens.Clear();
            Instance.uiLastShownScreens.Clear();
            Instance.ui_stackedScreens.Clear();
        }

        /// <summary>
        /// Private method for clearing all UI data via event system.
        /// </summary>
        /// <param name="eventData">Event data (unused)</param>
        private void privateClearAllData(string eventData)
        {
            uiScreens.Clear();
            uiLastShownScreens.Clear();
            ui_stackedScreens.Clear();
        }

        #region Virtual Methods

        /// <summary>
        /// Virtual method called during Awake. Override in derived classes to add custom initialization.
        /// </summary>
        protected virtual void _onAwake()
        {
            // Override this method in derived classes to add custom behavior
        }

        /// <summary>
        /// Virtual method called during Start. Override in derived classes to add custom initialization.
        /// </summary>
        protected virtual void _onStart()
        {
            // Override this method in derived classes to add custom behavior
        }

        /// <summary>
        /// Virtual method called during OnDestroy. Override in derived classes to add custom cleanup.
        /// </summary>
        protected virtual void _onDestroy()
        {
            // Override this method in derived classes to add custom behavior
        }

        #endregion
    }
}
}
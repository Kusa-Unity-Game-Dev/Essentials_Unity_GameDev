using UnityEngine;

namespace BB.Framework.Examples
{
    /// <summary>
    /// Example implementations demonstrating the UI layering system.
    /// These are reference implementations - copy and modify for your needs.
    /// </summary>

    #region Basic UI Examples

    /// <summary>
    /// Example: Simple main menu UI
    /// </summary>
    public class MainMenuUI : UIBase
    {
        protected override void OnCanvasShowBegin()
        {
            // Configure as background layer UI
            Layer = UILayer.Background;
            
            // Animation start logic here
            Debug.Log("Main Menu showing");
        }

        protected override void OnCanvasShowEnd()
        {
            // UI is fully visible and interactive
            Debug.Log("Main Menu visible");
        }

        protected override void OnCanvasHideBegin()
        {
            // Start hide animation
            Debug.Log("Main Menu hiding");
        }

        protected override void OnCanvasHideEnd()
        {
            // UI is now hidden
            Debug.Log("Main Menu hidden");
        }
    }

    /// <summary>
    /// Example: Popup dialog that should appear on top
    /// </summary>
    public class DialogUI : UIBase
    {
        private void Awake()
        {
            // Set as popup with high priority
            SetLayerAndPriority(UILayer.Popup, 5);
        }

        protected override void OnCanvasShowBegin()
        {
            // Ensure this dialog is on top when shown
            BringToFront();
        }

        protected override void OnCanvasShowEnd()
        {
            Debug.Log($"Dialog visible at sort order: {SortOrder}");
        }

        protected override void OnCanvasHideBegin() { }
        protected override void OnCanvasHideEnd() { }
    }

    /// <summary>
    /// Example: HUD that should stay in main layer
    /// </summary>
    public class GameHUD : UIBase
    {
        private void Start()
        {
            // HUD stays in main layer
            Layer = UILayer.Main;
            LayerPriority = 0; // Behind other main UI elements
        }

        protected override void OnCanvasShowBegin() { }
        protected override void OnCanvasShowEnd() { }
        protected override void OnCanvasHideBegin() { }
        protected override void OnCanvasHideEnd() { }
    }

    /// <summary>
    /// Example: Notification that appears as overlay
    /// </summary>
    public class NotificationUI : UIBase
    {
        [SerializeField] private float autoHideDelay = 3f;

        protected override void OnCanvasShowBegin()
        {
            // Notifications should be overlays
            Layer = UILayer.Overlay;
            BringToFront(); // Ensure latest notification is on top
        }

        protected override void OnCanvasShowEnd()
        {
            // Auto-hide after delay
            Invoke(nameof(AutoHide), autoHideDelay);
        }

        protected override void OnCanvasHideBegin()
        {
            CancelInvoke(nameof(AutoHide));
        }

        protected override void OnCanvasHideEnd() { }

        private void AutoHide()
        {
            UIManager.Instance?.HideUI(this);
        }
    }

    #endregion

    #region Advanced Examples

    /// <summary>
    /// Example: Settings menu that can be shown from multiple contexts
    /// </summary>
    public class SettingsUI : UIBase
    {
        private UIBase previousUI;

        public void ShowFromContext(UIBase callingUI)
        {
            previousUI = callingUI;
            
            // Settings should be a popup
            Layer = UILayer.Popup;
            
            UIManager.Instance.ShowUI(this);
        }

        protected override void OnCanvasShowBegin() { }
        protected override void OnCanvasShowEnd() { }
        protected override void OnCanvasHideBegin() { }

        protected override void OnCanvasHideEnd()
        {
            // Return to previous UI if it exists
            if (previousUI != null)
            {
                UIManager.Instance.ShowUI(previousUI);
            }
        }

        public void OnCloseButtonClicked()
        {
            UIManager.Instance.HideUI(this);
        }
    }

    /// <summary>
    /// Example: Loading screen that should block everything
    /// </summary>
    public class LoadingScreenUI : UIBase
    {
        private void Awake()
        {
            // System layer should be on top of everything except debug
            SetLayerAndPriority(UILayer.System, 0);
        }

        protected override void OnCanvasShowBegin()
        {
            // Hide all popups while loading
            UIManager.Instance.HideAllUIInLayer(UILayer.Popup);
        }

        protected override void OnCanvasShowEnd() { }
        protected override void OnCanvasHideBegin() { }
        protected override void OnCanvasHideEnd() { }
    }

    #endregion

    #region Manager Example

    /// <summary>
    /// Example: Custom UI controller that manages multiple screens
    /// </summary>
    public class GameUIController : MonoBehaviour
    {
        [SerializeField] private GameHUD hudUI;
        [SerializeField] private DialogUI pauseDialog;
        [SerializeField] private SettingsUI settingsUI;

        private void Start()
        {
            // Show HUD at game start
            UIManager.Instance.ShowUI(hudUI);
        }

        public void OnPauseButtonPressed()
        {
            // Show pause dialog
            UIManager.Instance.ShowUI(pauseDialog);
        }

        public void OnSettingsButtonPressed()
        {
            // Show settings from current context
            settingsUI.ShowFromContext(pauseDialog);
        }

        public void OnResumeButtonPressed()
        {
            // Hide all popups to resume game
            UIManager.Instance.HideAllUIInLayer(UILayer.Popup);
        }

        public void HideAllGameUI()
        {
            // Hide everything in main layer
            UIManager.Instance.HideAllUIInLayer(UILayer.Main);
        }
    }

    #endregion
}

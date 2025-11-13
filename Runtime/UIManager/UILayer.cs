namespace BB.Framework
{
    /// <summary>
    /// Defines the predefined UI layers for automatic sorting.
    /// Lower enum values appear behind higher values.
    /// </summary>
    public enum UILayer
    {
        /// <summary>Background UI elements (e.g., main menu background)</summary>
        Background = 0,
        
        /// <summary>Main game UI elements (e.g., HUD, main screens)</summary>
        Main = 100,
        
        /// <summary>Popup and dialog windows</summary>
        Popup = 200,
        
        /// <summary>Overlay UI elements (e.g., tooltips, notifications)</summary>
        Overlay = 300,
        
        /// <summary>System UI elements (e.g., loading screens, transitions)</summary>
        System = 400,
        
        /// <summary>Debug UI elements (always on top)</summary>
        Debug = 500
    }

    /// <summary>
    /// Constants for UI sorting and layering system.
    /// </summary>
    public static class UILayerConstants
    {
        /// <summary>Number of sort order slots available per layer</summary>
        public const int SORT_ORDER_STEP = 10;
        
        /// <summary>Maximum number of UIs per layer before overlapping</summary>
        public const int MAX_UI_PER_LAYER = 10;
    }
}

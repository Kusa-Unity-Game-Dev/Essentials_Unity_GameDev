using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BB.Framework {

/// <summary>
/// Manages UI screens with automatic layer-based sorting and proper stacking behavior.
/// Provides industry-standard UI management with automatic sorting order assignment.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    /// <summary>
    /// All currently visible UI screens.
    /// </summary>
    public List<UIBase> uiScreens = new List<UIBase>();
    
    /// <summary>
    /// History of recently shown UI screens.
    /// </summary>
    public List<UIBase> uiLastShownScreens = new List<UIBase>();

    /// <summary>
    /// Dictionary tracking UIs by their layer for efficient layer management.
    /// </summary>
    private Dictionary<UILayer, List<UIBase>> m_layerUIMap = new Dictionary<UILayer, List<UIBase>>();
    
    /// <summary>
    /// Stacked screens per layer for proper layer-specific stacking behavior.
    /// </summary>
    private Dictionary<UILayer, Stack<UIBase>> m_layerStacks = new Dictionary<UILayer, Stack<UIBase>>();

    /// <summary>
    /// Increment for sorting order within each layer.
    /// </summary>
    [SerializeField]
    [Tooltip("The increment between sorting orders for UIs within the same layer.")]
    private int m_sortingOrderIncrement = 10;

    private const short LAST_UI_REMEMBER_LIST = 10;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeLayerMaps();
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        _onAwake();
    }

    private void Start()
    {
        _onStart();
        //FSM.AddListener_s(GameConstants.E__CLEARUI, privateClearAllData);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        _onDestroy();
        //Instance = null;
        //FSM.RemoveListener_s(GameConstants.E__CLEARUI, privateClearAllData);
    }

    /// <summary>
    /// Initializes the layer dictionaries for all UILayer enum values.
    /// </summary>
    private void InitializeLayerMaps()
    {
        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
        {
            m_layerUIMap[layer] = new List<UIBase>();
            m_layerStacks[layer] = new Stack<UIBase>();
        }
    }

    /// <summary>
    /// Registers a UI and assigns it a proper sorting order based on its layer.
    /// </summary>
    /// <param name="ui">The UI to register.</param>
    public void RegisterUI(UIBase ui)
    {
        if (ui == null) return;

        if (!uiScreens.Contains(ui))
        {
            uiScreens.Add(ui);
        }

        UILayer layer = ui.Layer;
        List<UIBase> layerList = m_layerUIMap[layer];
        
        if (!layerList.Contains(ui))
        {
            layerList.Add(ui);
        }

        // Assign sorting order based on layer and position
        RecalculateSortingOrdersForLayer(layer);
    }

    /// <summary>
    /// Unregisters a UI and handles stacking behavior properly.
    /// </summary>
    /// <param name="ui">The UI to unregister.</param>
    /// <param name="pushToStack">If true, the UI will be pushed to the stack for later restoration.</param>
    public void UnregisterUI(UIBase ui, bool pushToStack = false)
    {
        if (ui == null) return;

        UILayer layer = ui.Layer;

        // Remove from main screens list
        if (uiScreens.Contains(ui))
        {
            uiScreens.Remove(ui);
        }

        // Remove from layer-specific list
        List<UIBase> layerList = m_layerUIMap[layer];
        if (layerList.Contains(ui))
        {
            layerList.Remove(ui);
        }

        // Handle stacking logic
        Stack<UIBase> layerStack = m_layerStacks[layer];
        
        if (pushToStack)
        {
            // Push to stack for later restoration (only if stackable)
            if (ui.isAllowedToStack)
            {
                layerStack.Push(ui);
            }
        }
        else
        {
            // Try to restore a stacked UI only if the closing UI is stackable
            // This prevents issues when non-stackable UIs close
            if (ui.isAllowedToStack && layerStack.Count > 0)
            {
                UIBase stackedUI = layerStack.Pop();
                // Ensure the stacked UI is still valid and not destroyed
                if (stackedUI != null)
                {
                    ShowUI(stackedUI);
                }
            }
        }

        // Recalculate sorting orders for remaining UIs in this layer
        RecalculateSortingOrdersForLayer(layer);

        // Add to last shown screens history
        AddToHistory(ui);
    }

    /// <summary>
    /// Called when a UI's layer is changed while it's visible.
    /// </summary>
    /// <param name="ui">The UI whose layer changed.</param>
    /// <param name="oldLayer">The previous layer.</param>
    /// <param name="newLayer">The new layer.</param>
    public void OnUILayerChanged(UIBase ui, UILayer oldLayer, UILayer newLayer)
    {
        // Remove from old layer
        List<UIBase> oldLayerList = m_layerUIMap[oldLayer];
        if (oldLayerList.Contains(ui))
        {
            oldLayerList.Remove(ui);
            RecalculateSortingOrdersForLayer(oldLayer);
        }

        // Add to new layer
        List<UIBase> newLayerList = m_layerUIMap[newLayer];
        if (!newLayerList.Contains(ui))
        {
            newLayerList.Add(ui);
        }
        RecalculateSortingOrdersForLayer(newLayer);
    }

    /// <summary>
    /// Recalculates and assigns sorting orders for all UIs in a specific layer.
    /// </summary>
    /// <param name="layer">The layer to recalculate.</param>
    private void RecalculateSortingOrdersForLayer(UILayer layer)
    {
        List<UIBase> layerList = m_layerUIMap[layer];
        int baseOrder = (int)layer;

        for (int i = 0; i < layerList.Count; i++)
        {
            UIBase ui = layerList[i];
            if (ui != null)
            {
                int sortingOrder = baseOrder + (i * m_sortingOrderIncrement);
                ui.SetSortingOrder(sortingOrder);
            }
        }
    }

    /// <summary>
    /// Adds a UI to the history list.
    /// </summary>
    /// <param name="ui">The UI to add to history.</param>
    private void AddToHistory(UIBase ui)
    {
        uiLastShownScreens.Add(ui);
        if (uiLastShownScreens.Count >= LAST_UI_REMEMBER_LIST)
        {
            uiLastShownScreens.RemoveAt(0);
        }
    }

    /// <summary>
    /// Shows a UI with proper layer management.
    /// </summary>
    /// <param name="ui">The UI to show.</param>
    /// <param name="delay">Optional delay before showing.</param>
    public void ShowUI(UIBase ui, float delay = 0)
    {
        if (ui == null) return;
        
        ui._UIBaseShowUI(delay);
    }

    /// <summary>
    /// Hides a UI with proper cleanup.
    /// </summary>
    /// <param name="ui">The UI to hide.</param>
    /// <param name="delay">Optional delay for hide animation.</param>
    public void HideUI(UIBase ui, float delay = 0)
    {
        if (ui == null) return;
        
        ui._UIBaseHideUI(delay);
    }

    /// <summary>
    /// Hides all visible UIs across all layers.
    /// </summary>
    public void HideAllUI()
    {
        // Create a copy to avoid modification during iteration
        List<UIBase> screensToHide = new List<UIBase>(uiScreens);
        foreach (var ui in screensToHide)
        {
            HideUI(ui);
        }
    }

    /// <summary>
    /// Hides all UIs in a specific layer.
    /// </summary>
    /// <param name="layer">The layer to clear.</param>
    public void HideAllUIInLayer(UILayer layer)
    {
        List<UIBase> layerList = new List<UIBase>(m_layerUIMap[layer]);
        foreach (var ui in layerList)
        {
            HideUI(ui);
        }
    }

    /// <summary>
    /// Gets all visible UIs in a specific layer.
    /// </summary>
    /// <param name="layer">The layer to query.</param>
    /// <returns>Read-only list of UIs in the layer.</returns>
    public IReadOnlyList<UIBase> GetUIsInLayer(UILayer layer)
    {
        return m_layerUIMap[layer].AsReadOnly();
    }

    /// <summary>
    /// Gets the top (highest sorting order) UI in a specific layer.
    /// </summary>
    /// <param name="layer">The layer to query.</param>
    /// <returns>The topmost UI in the layer, or null if empty.</returns>
    public UIBase GetTopUIInLayer(UILayer layer)
    {
        List<UIBase> layerList = m_layerUIMap[layer];
        return layerList.Count > 0 ? layerList[layerList.Count - 1] : null;
    }

    /// <summary>
    /// Gets the currently active top UI across all layers.
    /// </summary>
    /// <returns>The topmost visible UI, or null if no UIs are visible.</returns>
    public UIBase GetTopUI()
    {
        UIBase topUI = null;
        int highestOrder = int.MinValue;

        foreach (var ui in uiScreens)
        {
            if (ui != null && ui.CurrentSortingOrder > highestOrder)
            {
                highestOrder = ui.CurrentSortingOrder;
                topUI = ui;
            }
        }

        return topUI;
    }

    /// <summary>
    /// Brings a UI to the front within its layer.
    /// </summary>
    /// <param name="ui">The UI to bring to front.</param>
    public void BringToFront(UIBase ui)
    {
        if (ui == null || !ui.IsVisible) return;

        UILayer layer = ui.Layer;
        List<UIBase> layerList = m_layerUIMap[layer];

        if (layerList.Contains(ui))
        {
            layerList.Remove(ui);
            layerList.Add(ui);
            RecalculateSortingOrdersForLayer(layer);
        }
    }

    /// <summary>
    /// Sends a UI to the back within its layer.
    /// </summary>
    /// <param name="ui">The UI to send to back.</param>
    public void SendToBack(UIBase ui)
    {
        if (ui == null || !ui.IsVisible) return;

        UILayer layer = ui.Layer;
        List<UIBase> layerList = m_layerUIMap[layer];

        if (layerList.Contains(ui))
        {
            layerList.Remove(ui);
            layerList.Insert(0, ui);
            RecalculateSortingOrdersForLayer(layer);
        }
    }

    /// <summary>
    /// Clears the stack for a specific layer.
    /// </summary>
    /// <param name="layer">The layer whose stack to clear.</param>
    public void ClearStackForLayer(UILayer layer)
    {
        m_layerStacks[layer].Clear();
    }

    /// <summary>
    /// Clears all stacks across all layers.
    /// </summary>
    public void ClearAllStacks()
    {
        foreach (var layer in m_layerStacks.Keys.ToList())
        {
            m_layerStacks[layer].Clear();
        }
    }

    /// <summary>
    /// Gets the count of stacked UIs for a specific layer.
    /// </summary>
    /// <param name="layer">The layer to query.</param>
    /// <returns>Number of stacked UIs in the layer.</returns>
    public int GetStackCountForLayer(UILayer layer)
    {
        return m_layerStacks[layer].Count;
    }

    /// <summary>
    /// Static method to clear all UI lists and stacks.
    /// </summary>
    public static void ClearAllList_s()
    {
        if (Instance == null) return;
        
        Instance.uiScreens.Clear();
        Instance.uiLastShownScreens.Clear();
        Instance.ClearAllStacks();
        
        foreach (var layer in Instance.m_layerUIMap.Keys.ToList())
        {
            Instance.m_layerUIMap[layer].Clear();
        }
    }

    /// <summary>
    /// Clears all data (called from external events).
    /// </summary>
    /// <param name="game">Game identifier (unused, kept for compatibility).</param>
    public void privateClearAllData(string game)
    {
        ClearAllList_s();
    }

    // Legacy compatibility properties
    [Obsolete("Use m_layerStacks instead for layer-specific stacking")]
    public List<UIBase> ui_stackedScreens
    {
        get
        {
            // Returns a combined list for backward compatibility
            List<UIBase> combined = new List<UIBase>();
            foreach (var stack in m_layerStacks.Values)
            {
                combined.AddRange(stack);
            }
            return combined;
        }
    }
    
    // Virtual Methods for derived class customization
    public virtual void _onAwake()
    {
        // Override this method in derived classes to add custom behavior
    }
    
    public virtual void _onStart()
    {
        // Override this method in derived classes to add custom behavior
    }
    
    public virtual void _onDestroy()
    {
        // Override this method in derived classes to add custom behavior
    }
}
}
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BB.Framework {

/// <summary>
/// Defines the layer category for UI elements.
/// Each layer has a base sorting order that determines its rendering priority.
/// UIs within the same layer are automatically managed for proper ordering.
/// Layers are spaced 1000 apart to accommodate up to 100 UIs per layer (with default increment of 10).
/// </summary>
public enum UILayer
{
    /// <summary>Background layer for backdrop UIs (base order: 0)</summary>
    Background = 0,
    /// <summary>Main UI layer for primary game screens (base order: 1000)</summary>
    Main = 1000,
    /// <summary>HUD layer for heads-up display elements (base order: 2000)</summary>
    HUD = 2000,
    /// <summary>Popup layer for dialog boxes and popups (base order: 3000)</summary>
    Popup = 3000,
    /// <summary>Overlay layer for overlays and modals (base order: 4000)</summary>
    Overlay = 4000,
    /// <summary>Tooltip layer for tooltips and hints (base order: 5000)</summary>
    Tooltip = 5000,
    /// <summary>Toast layer for notifications and toasts (base order: 5000)</summary>
    Toast = 6000,
    /// <summary>System layer for system-level UIs (base order: 6000)</summary>
    System = 7000,
    /// <summary>Debug layer for debug/dev console UIs (base order: 7000)</summary>
    Debug = 8000
}

public abstract class UIBase : MonoBehaviour
{
    [SerializeField]
    protected Canvas m_canvas;
    [SerializeField]
    protected GraphicRaycaster m_graphicRaycaster;

    [Space, Header("TimeForAnimation"), SerializeField]
    protected float m_initAnimationTime = .4f;
    [SerializeField]
    protected float m_outroAnimationTime = .4f;

    [Space, Header("Layer Settings")]
    [SerializeField]
    [Tooltip("The UI layer this element belongs to. Determines rendering order relative to other UIs.")]
    private UILayer m_uiLayer = UILayer.Main;

    [SerializeField]
    [Tooltip("If true, this UI can be stacked and restored when another UI closes.")]
    public bool isAllowedToStack = true;

    /// <summary>
    /// Gets or sets the UI layer for this element. Changing this will update the sorting order.
    /// </summary>
    public UILayer Layer
    {
        get => m_uiLayer;
        set
        {
            if (m_uiLayer != value)
            {
                UILayer oldLayer = m_uiLayer;
                m_uiLayer = value;
                if (IsVisible && UIManager.Instance != null)
                {
                    UIManager.Instance.OnUILayerChanged(this, oldLayer, value);
                }
            }
        }
    }

    /// <summary>
    /// Gets whether this UI is currently visible.
    /// </summary>
    public bool IsVisible { get; private set; }

    /// <summary>
    /// Gets the current sorting order assigned to this UI by the UIManager.
    /// </summary>
    public int CurrentSortingOrder { get; private set; }

    protected abstract void OnCanvasShowBegin();
    protected abstract void OnCanvasShowEnd();
    protected abstract void OnCanvasHideBegin();
    protected abstract void OnCanvasHideEnd();

    private void Start()
    {
        /*m_canvas.enabled = false;
        m_graphicRaycaster.enabled = false;*/
    }

    /// <summary>
    /// Sets the sorting order for this UI's canvas.
    /// Called by UIManager to manage layer-based ordering.
    /// </summary>
    /// <param name="sortingOrder">The sorting order to set.</param>
    public void SetSortingOrder(int sortingOrder)
    {
        CurrentSortingOrder = sortingOrder;
        if (m_canvas != null)
        {
            m_canvas.sortingOrder = sortingOrder;
        }
    }

    public void _UIBaseShowUI(float a_delay = 0.0f)
    {
        if (IsVisible) return;

        m_canvas.enabled = true;
        IsVisible = true;

        UIManager.Instance.RegisterUI(this);

        OnCanvasShowBegin();
        StartCoroutine(ReadyAfterTransition(a_delay));
    }

    public void _UIBaseHideUI(float a_delay = 0.0f)
    {
        if (!IsVisible) return;

        IsVisible = false;
        OnCanvasHideBegin();

        UIManager.Instance.UnregisterUI(this);

        // Optionally delay to allow transitions to finish
        StartCoroutine(HideAfterTransition(a_delay));
    }

    public void _UIBaseHideUI_AfterStacking(float a_delay = 0.0f)
    {
        if (!IsVisible) return;

        OnCanvasHideBegin();

        IsVisible = false;

        UIManager.Instance.UnregisterUI(this, true);

        // Optionally delay to allow transitions to finish
        StartCoroutine(HideAfterTransition(a_delay));
    }

    private IEnumerator HideAfterTransition(float a_delay)
    {
        yield return new WaitForSecondsRealtime(a_delay); // Adjust for transition duration
        
        m_canvas.enabled = false;
        m_graphicRaycaster.enabled = false;

        OnCanvasHideEnd();
    }

    private IEnumerator ReadyAfterTransition(float a_delay)
    {
        yield return new WaitForSecondsRealtime(a_delay); // Adjust for transition duration
        m_graphicRaycaster.enabled = true;
        OnCanvasShowEnd();
    }
}
}
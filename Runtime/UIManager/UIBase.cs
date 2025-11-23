using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BB.Framework {
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

    [SerializeField]
    public bool isAllowedToStack = true;

    [Space, Header("UI Layering")]
    [SerializeField, Tooltip("The UI layer this element belongs to")]
    private UILayer m_uiLayer = UILayer.Main;
    
    [SerializeField, Tooltip("Priority within the layer (0-9, higher = front)")]
    private int m_layerPriority = 0;

    public bool IsVisible { get; private set; }
    
    /// <summary>
    /// Gets or sets the UI layer. Automatically updates sort order when changed.
    /// </summary>
    public UILayer Layer
    {
        get => m_uiLayer;
        set
        {
            if (m_uiLayer != value)
            {
                m_uiLayer = value;
                UpdateSortOrder();
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the priority within the current layer (0-9).
    /// Higher values appear in front. Automatically updates sort order.
    /// </summary>
    public int LayerPriority
    {
        get => m_layerPriority;
        set
        {
            m_layerPriority = Mathf.Clamp(value, 0, UILayerConstants.MAX_UI_PER_LAYER - 1);
            UpdateSortOrder();
        }
    }
    
    /// <summary>
    /// Gets the calculated sort order based on layer and priority.
    /// </summary>
    public int SortOrder => CalculateSortOrder(m_uiLayer, m_layerPriority);

    protected abstract void OnCanvasShowBegin();
    protected abstract void OnCanvasShowEnd();
    protected abstract void OnCanvasHideBegin();
    protected abstract void OnCanvasHideEnd();

    private void Start()
    {
        /*m_canvas.enabled = false;
        m_graphicRaycaster.enabled = false;*/
        
        // Initialize sort order on start
        UpdateSortOrder();
    }
    
    /// <summary>
    /// Calculates the sort order based on layer and priority.
    /// </summary>
    private int CalculateSortOrder(UILayer layer, int priority)
    {
        return (int)layer + Mathf.Clamp(priority, 0, UILayerConstants.MAX_UI_PER_LAYER - 1);
    }
    
    /// <summary>
    /// Updates the canvas sort order based on current layer and priority.
    /// </summary>
    private void UpdateSortOrder()
    {
        if (m_canvas != null)
        {
            m_canvas.sortingOrder = SortOrder;
        }
    }
    
    /// <summary>
    /// Brings this UI to the front of its current layer.
    /// </summary>
    public void BringToFront()
    {
        LayerPriority = UILayerConstants.MAX_UI_PER_LAYER - 1;
    }
    
    /// <summary>
    /// Sends this UI to the back of its current layer.
    /// </summary>
    public void SendToBack()
    {
        LayerPriority = 0;
    }
    
    /// <summary>
    /// Sets the UI layer and priority in one call.
    /// </summary>
    public void SetLayerAndPriority(UILayer layer, int priority)
    {
        m_uiLayer = layer;
        m_layerPriority = Mathf.Clamp(priority, 0, UILayerConstants.MAX_UI_PER_LAYER - 1);
        UpdateSortOrder();
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
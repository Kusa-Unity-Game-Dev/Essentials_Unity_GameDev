using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

    public bool IsVisible { get; private set; }

    protected abstract void OnCanvasShowBegin();
    protected abstract void OnCanvasShowEnd();
    protected abstract void OnCanvasHideBegin();
    protected abstract void OnCanvasHideEnd();

    private void Start()
    {
        /*m_canvas.enabled = false;
        m_graphicRaycaster.enabled = false;*/
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

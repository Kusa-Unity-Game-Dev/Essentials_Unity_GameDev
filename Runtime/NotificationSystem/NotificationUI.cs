using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotificationUI : UIBase
{
    [Header("UI Components")]
    public Image iconImage;
    public TextMeshProUGUI messageText;
    public Image backgroundImage;

    private float m_displayDuration = 0;
    private Action onDismiss;

    [Header("UI Management")]
    [SerializeField]
    protected RectTransform rectTransform;

    //make instance
    public static NotificationUI s_Instance;

    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            s_Instance = this;
        }
    }

    #region static methods
    public static void ShowUI_s(NotificationData data, Action onDismiss)
    {
        if (!s_Instance)
            return;
        s_Instance.ShowUI(data, onDismiss);
    }
    #endregion

    #region overriden methods
    protected override void OnCanvasShowBegin() 
    {

        rectTransform.localScale = Vector3.zero;
        rectTransform.DOScale(Vector3.one, m_initAnimationTime).SetEase(Ease.OutBack);
        
    }
    protected override void OnCanvasShowEnd() 
    {
        StartCoroutine(HideAfter(m_displayDuration));
    }
    protected override void OnCanvasHideBegin() 
    {
        rectTransform.DOScale(Vector3.zero, m_outroAnimationTime).SetEase(Ease.InBack);
    }
    protected override void OnCanvasHideEnd() 
    { 
        onDismiss?.Invoke();
    }
    #endregion


    #region private methods
    private void ShowUI(NotificationData data, Action onDismiss)
    {
        // Set the message and style
        messageText.text = data.message;
        if (data.type.icon != null) iconImage.sprite = data.type.icon;
        backgroundImage.color = data.type.backgroundColor;

        m_displayDuration = data.type.displayDuration;
        this.onDismiss = onDismiss;

        // Play notification sound if available
        if (data.type.soundID != null)
        {
            SoundManager.PlaySound(data.type.soundID);
        }

        _UIBaseShowUI(m_initAnimationTime);
        
    }
    
    private System.Collections.IEnumerator HideAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        _UIBaseHideUI(m_outroAnimationTime);
    }
    #endregion


}

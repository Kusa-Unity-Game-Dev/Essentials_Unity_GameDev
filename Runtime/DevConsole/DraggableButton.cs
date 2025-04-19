using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableButton : MonoBehaviour, IDragHandler, IPointerClickHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        // Hide in release mode
#if !UNITY_EDITOR
        gameObject.SetActive(false);
#endif
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Toggle Dev Console
        if (DevConsole.Instance != null)
        {
            DevConsole.Instance.SendMessage("ToggleConsole", SendMessageOptions.DontRequireReceiver);
        }
    }
}

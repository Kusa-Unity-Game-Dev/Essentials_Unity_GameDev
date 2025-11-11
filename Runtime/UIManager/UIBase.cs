using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BB.Framework 
{
    /// <summary>
    /// Abstract base class for all UI screens in the game.
    /// Provides lifecycle management, visibility control, and animation hooks.
    /// Must be inherited to implement custom UI behavior.
    /// </summary>
    public abstract class UIBase : MonoBehaviour
    {
        [SerializeField]
        protected Canvas m_canvas;
        
        [SerializeField]
        protected GraphicRaycaster m_graphicRaycaster;

        [Space, Header("TimeForAnimation"), SerializeField]
        protected float m_initAnimationTime = 0.4f;
        
        [SerializeField]
        protected float m_outroAnimationTime = 0.4f;

        /// <summary>
        /// Determines if this UI can be added to the stack when hidden.
        /// If true, hiding this UI may restore a previously shown UI.
        /// </summary>
        [SerializeField]
        public bool isAllowedToStack = true;

        /// <summary>
        /// Gets whether this UI is currently visible.
        /// </summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// Called when the UI starts to show. Implement custom show animations here.
        /// </summary>
        protected abstract void OnCanvasShowBegin();

        /// <summary>
        /// Called when the UI is fully shown and ready for interaction.
        /// </summary>
        protected abstract void OnCanvasShowEnd();

        /// <summary>
        /// Called when the UI starts to hide. Implement custom hide animations here.
        /// </summary>
        protected abstract void OnCanvasHideBegin();

        /// <summary>
        /// Called when the UI is fully hidden and disabled.
        /// </summary>
        protected abstract void OnCanvasHideEnd();

        private void Start()
        {
            // Optional: Initialize UI as hidden
            // m_canvas.enabled = false;
            // m_graphicRaycaster.enabled = false;
        }

        /// <summary>
        /// Internal method to show the UI. Called by UIManager.
        /// Do not call directly - use UIManager.ShowUI() instead.
        /// </summary>
        /// <param name="delay">Optional delay before enabling interactions</param>
        public void _UIBaseShowUI(float delay = 0.0f)
        {
            if (IsVisible)
            {
                Debug.LogWarning($"[UIBase] UI '{gameObject.name}' is already visible");
                return;
            }

            m_canvas.enabled = true;
            IsVisible = true;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.RegisterUI(this);
            }

            OnCanvasShowBegin();
            StartCoroutine(ReadyAfterTransition(delay));
        }

        /// <summary>
        /// Internal method to hide the UI. Called by UIManager.
        /// Do not call directly - use UIManager.HideUI() instead.
        /// </summary>
        /// <param name="delay">Optional delay before disabling canvas</param>
        public void _UIBaseHideUI(float delay = 0.0f)
        {
            if (!IsVisible)
            {
                Debug.LogWarning($"[UIBase] UI '{gameObject.name}' is already hidden");
                return;
            }

            IsVisible = false;
            OnCanvasHideBegin();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UnregisterUI(this);
            }

            // Delay to allow transitions to finish
            StartCoroutine(HideAfterTransition(delay));
        }

        /// <summary>
        /// Internal method to hide the UI and add it to the stack.
        /// Used when you want to temporarily hide this UI but restore it later.
        /// </summary>
        /// <param name="delay">Optional delay before disabling canvas</param>
        public void _UIBaseHideUI_AfterStacking(float delay = 0.0f)
        {
            if (!IsVisible)
            {
                Debug.LogWarning($"[UIBase] UI '{gameObject.name}' is already hidden");
                return;
            }

            OnCanvasHideBegin();
            IsVisible = false;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UnregisterUI(this, addToStack: true);
            }

            // Delay to allow transitions to finish
            StartCoroutine(HideAfterTransition(delay));
        }

        /// <summary>
        /// Coroutine that waits for hide transition to complete before fully disabling the UI.
        /// </summary>
        private IEnumerator HideAfterTransition(float delay)
        {
            yield return new WaitForSecondsRealtime(delay + m_outroAnimationTime);

            m_canvas.enabled = false;
            m_graphicRaycaster.enabled = false;

            OnCanvasHideEnd();
        }

        /// <summary>
        /// Coroutine that waits for show transition to complete before enabling interactions.
        /// </summary>
        private IEnumerator ReadyAfterTransition(float delay)
        {
            yield return new WaitForSecondsRealtime(delay + m_initAnimationTime);
            
            m_graphicRaycaster.enabled = true;
            OnCanvasShowEnd();
        }
    }
}
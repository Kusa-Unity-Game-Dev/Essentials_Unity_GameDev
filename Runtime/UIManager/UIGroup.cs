using System.Collections.Generic;
using UnityEngine;

namespace BB.Framework
{
    /// <summary>
    /// Manages a group of related UI elements as a single unit.
    /// Useful for managing multi-panel screens or UI sets that should be shown/hidden together.
    /// </summary>
    public class UIGroup : MonoBehaviour
    {
        [SerializeField, Tooltip("All UI elements in this group")]
        private List<UIBase> m_uiElements = new List<UIBase>();
        
        [SerializeField, Tooltip("Show UIs in sequence or all at once")]
        private bool m_showSequentially = false;
        
        [SerializeField, Tooltip("Delay between showing each UI when sequential")]
        private float m_sequentialDelay = 0.2f;
        
        /// <summary>
        /// Gets the list of UI elements in this group.
        /// </summary>
        public List<UIBase> UIElements => m_uiElements;
        
        /// <summary>
        /// Adds a UI element to this group.
        /// </summary>
        public void AddUI(UIBase ui)
        {
            if (!m_uiElements.Contains(ui))
            {
                m_uiElements.Add(ui);
            }
        }
        
        /// <summary>
        /// Removes a UI element from this group.
        /// </summary>
        public void RemoveUI(UIBase ui)
        {
            m_uiElements.Remove(ui);
        }
        
        /// <summary>
        /// Shows all UI elements in this group.
        /// </summary>
        public void ShowAll()
        {
            if (m_showSequentially)
            {
                ShowSequentially();
            }
            else
            {
                ShowAllAtOnce();
            }
        }
        
        /// <summary>
        /// Shows all UI elements at once.
        /// </summary>
        public void ShowAllAtOnce()
        {
            foreach (var ui in m_uiElements)
            {
                if (ui != null)
                {
                    UIManager.Instance.ShowUI(ui);
                }
            }
        }
        
        /// <summary>
        /// Shows UI elements one after another with a delay.
        /// </summary>
        public void ShowSequentially()
        {
            StartCoroutine(ShowSequentiallyCoroutine());
        }
        
        private System.Collections.IEnumerator ShowSequentiallyCoroutine()
        {
            foreach (var ui in m_uiElements)
            {
                if (ui != null)
                {
                    UIManager.Instance.ShowUI(ui);
                    yield return new WaitForSeconds(m_sequentialDelay);
                }
            }
        }
        
        /// <summary>
        /// Hides all UI elements in this group.
        /// </summary>
        public void HideAll()
        {
            foreach (var ui in m_uiElements)
            {
                if (ui != null)
                {
                    UIManager.Instance.HideUI(ui);
                }
            }
        }
        
        /// <summary>
        /// Sets the layer for all UI elements in this group.
        /// </summary>
        public void SetGroupLayer(UILayer layer)
        {
            foreach (var ui in m_uiElements)
            {
                if (ui != null)
                {
                    ui.Layer = layer;
                }
            }
        }
        
        /// <summary>
        /// Sets incremental priorities for all UIs in the group (first UI = priority 0, second = 1, etc.)
        /// </summary>
        public void SetIncrementalPriorities(int startPriority = 0)
        {
            int priority = startPriority;
            foreach (var ui in m_uiElements)
            {
                if (ui != null)
                {
                    ui.LayerPriority = priority;
                    priority++;
                }
            }
        }
        
        /// <summary>
        /// Checks if all UI elements in the group are visible.
        /// </summary>
        public bool AreAllVisible()
        {
            foreach (var ui in m_uiElements)
            {
                if (ui != null && !ui.IsVisible)
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// Checks if any UI element in the group is visible.
        /// </summary>
        public bool IsAnyVisible()
        {
            foreach (var ui in m_uiElements)
            {
                if (ui != null && ui.IsVisible)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

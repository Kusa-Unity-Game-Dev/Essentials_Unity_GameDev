using System.Collections.Generic;
using UnityEngine;

namespace BB.Framework 
{
    /// <summary>
    /// Queue-based notification system that displays notifications one at a time.
    /// Implements the Singleton pattern to ensure only one notification manager exists.
    /// Notifications are shown sequentially to prevent UI overlap.
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        /// <summary>
        /// Gets the singleton instance of the NotificationManager.
        /// Note: Uses 's_Instance' naming for consistency with existing codebase.
        /// </summary>
        public static NotificationManager s_Instance { get; private set; }

        private readonly Queue<NotificationData> _notificationQueue = new Queue<NotificationData>();
        private bool _isDisplaying = false;

        private void Awake()
        {
            // Ensure only one instance exists
            if (s_Instance != null && s_Instance != this)
            {
                Debug.LogWarning("[NotificationManager] Duplicate instance detected. Destroying new instance.");
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            // Uncomment if you want NotificationManager to persist between scenes
            // DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (s_Instance == this)
            {
                s_Instance = null;
            }
        }

        /// <summary>
        /// Queues a notification for display. If no notification is currently showing,
        /// it will be displayed immediately. Otherwise, it will be shown after the current one finishes.
        /// </summary>
        /// <param name="data">The notification data to display. Cannot be null.</param>
        public void ShowNotification(NotificationData data)
        {
            if (data == null)
            {
                Debug.LogError("[NotificationManager] Cannot show null notification data");
                return;
            }

            _notificationQueue.Enqueue(data);

            if (!_isDisplaying)
            {
                DisplayNextNotification();
            }
        }

        /// <summary>
        /// Displays the next notification in the queue.
        /// Called automatically after the previous notification finishes.
        /// </summary>
        private void DisplayNextNotification()
        {
            if (_notificationQueue.Count == 0)
            {
                _isDisplaying = false;
                return;
            }

            _isDisplaying = true;
            NotificationData data = _notificationQueue.Dequeue();

            // Show the notification UI with a callback for when it completes
            NotificationUI.ShowUI_s(data, () => DisplayNextNotification());
        }

        /// <summary>
        /// Gets the number of notifications currently in the queue.
        /// </summary>
        /// <returns>The number of pending notifications</returns>
        public int GetQueuedNotificationCount()
        {
            return _notificationQueue.Count;
        }

        /// <summary>
        /// Clears all pending notifications from the queue.
        /// Does not affect the currently displayed notification.
        /// </summary>
        public void ClearQueue()
        {
            _notificationQueue.Clear();
            Debug.Log("[NotificationManager] Notification queue cleared");
        }

        /// <summary>
        /// Checks if a notification is currently being displayed.
        /// </summary>
        /// <returns>True if a notification is showing, false otherwise</returns>
        public bool IsDisplayingNotification()
        {
            return _isDisplaying;
        }
    }
}
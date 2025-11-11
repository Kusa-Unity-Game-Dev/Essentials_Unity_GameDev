using System;
using UnityEngine;

namespace BB.Framework 
{
    /// <summary>
    /// Finite State Machine (FSM) that provides global event management for game state changes.
    /// Implements the Singleton pattern and IEventListener interface.
    /// Uses string-based events for flexible state management.
    /// </summary>
    public class FSM : MonoBehaviour, IEventListener<string>
    {
        private static FSM s_Instance;
        
        /// <summary>
        /// Gets the singleton instance of the FSM.
        /// </summary>
        private static FSM Instance => s_Instance;

        private EventManager<string> _eventManager;

        #region Lifecycle

        private void Awake()
        {
            // Ensure only one instance exists
            if (s_Instance != null && s_Instance != this)
            {
                Debug.LogWarning("[FSM] Duplicate instance detected. Destroying new instance.");
                Destroy(gameObject);
                return;
            }

            _eventManager = new EventManager<string>();
            s_Instance = this;
            _onAwake();
        }

        private void OnDestroy()
        {
            if (s_Instance == this)
            {
                _eventManager?.RemoveAllEvents();
                s_Instance = null;
                _onDestroy();
            }
        }

        private void Start()
        {
            _onStart();
        }

        #endregion


        #region Static API

        /// <summary>
        /// Gets the underlying EventManager for advanced usage.
        /// </summary>
        /// <returns>The EventManager instance, or null if FSM is not initialized</returns>
        public static EventManager<string> GetEventManager()
        {
            if (!s_Instance)
            {
                Debug.LogWarning("[FSM] Cannot get EventManager: Instance is null");
                return null;
            }
            return s_Instance._eventManager;
        }

        /// <summary>
        /// Adds a listener for a specific event (static method).
        /// </summary>
        /// <param name="eventName">The event identifier. Cannot be null or empty.</param>
        /// <param name="listener">The callback to invoke. Cannot be null.</param>
        public static void AddListener_s(string eventName, Action<string> listener)
        {
            if (!s_Instance)
            {
                Debug.LogWarning("[FSM] Cannot add listener: Instance is null");
                return;
            }
            s_Instance.AddEventListener(eventName, listener);
        }

        /// <summary>
        /// Removes a listener for a specific event (static method).
        /// </summary>
        /// <param name="eventName">The event identifier</param>
        /// <param name="listener">The callback to remove</param>
        public static void RemoveListener_s(string eventName, Action<string> listener)
        {
            if (!s_Instance)
            {
                Debug.LogWarning("[FSM] Cannot remove listener: Instance is null");
                return;
            }
            s_Instance.RemoveEventListener(eventName, listener);
        }

        /// <summary>
        /// Dispatches an event to all registered listeners (static method).
        /// </summary>
        /// <param name="eventName">The event identifier</param>
        /// <param name="eventData">The event data to pass to listeners</param>
        public static void DispatchEvent_s(string eventName, string eventData)
        {
            if (!s_Instance)
            {
                Debug.LogWarning("[FSM] Cannot dispatch event: Instance is null");
                return;
            }
            s_Instance.DispatchEvent(eventName, eventData);
        }

        #endregion

        #region IEventListener Implementation

        /// <summary>
        /// Adds a listener for a specific event (instance method).
        /// Implements IEventListener interface.
        /// </summary>
        /// <param name="eventName">The event identifier</param>
        /// <param name="listener">The callback to invoke</param>
        public void AddEventListener(string eventName, Action<string> listener)
        {
            if (_eventManager == null)
            {
                Debug.LogWarning("[FSM] EventManager is null, creating new instance");
                _eventManager = new EventManager<string>();
            }
            _eventManager.AddListener(eventName, listener);
        }

        /// <summary>
        /// Removes a listener for a specific event (instance method).
        /// Implements IEventListener interface.
        /// </summary>
        /// <param name="eventName">The event identifier</param>
        /// <param name="listener">The callback to remove</param>
        public void RemoveEventListener(string eventName, Action<string> listener)
        {
            if (_eventManager == null)
            {
                Debug.LogWarning("[FSM] EventManager is null, cannot remove listener");
                return;
            }
            _eventManager.RemoveListener(eventName, listener);
        }

        /// <summary>
        /// Dispatches an event to all registered listeners (instance method).
        /// Implements IEventListener interface.
        /// </summary>
        /// <param name="eventName">The event identifier</param>
        /// <param name="eventData">The event data to pass to listeners</param>
        public void DispatchEvent(string eventName, string eventData)
        {
            if (_eventManager == null)
            {
                Debug.LogWarning("[FSM] EventManager is null, cannot dispatch event");
                return;
            }
            _eventManager.DispatchEvent(eventName, eventData);
        }

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Virtual method called during Start. Override in derived classes to implement custom initialization.
        /// </summary>
        protected virtual void _onStart()
        {
            // Override this method in derived classes to implement custom start logic
        }

        /// <summary>
        /// Virtual method called during OnDestroy. Override in derived classes to implement custom cleanup.
        /// </summary>
        protected virtual void _onDestroy()
        {
            // Override this method in derived classes to implement custom destroy logic
        }

        /// <summary>
        /// Virtual method called during Awake. Override in derived classes to implement custom initialization.
        /// </summary>
        protected virtual void _onAwake()
        {
            // Override this method in derived classes to implement custom awake logic
        }

        #endregion
    }
}
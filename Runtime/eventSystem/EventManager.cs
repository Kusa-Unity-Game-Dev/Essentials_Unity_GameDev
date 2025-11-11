using System;
using System.Collections.Generic;
using UnityEngine;

namespace BB.Framework
{
    /// <summary>
    /// Generic event management system that provides type-safe event dispatching and subscription.
    /// Supports any data type for event payloads, enabling decoupled communication between systems.
    /// </summary>
    /// <typeparam name="T">The type of data that will be passed with events</typeparam>
    public class EventManager<T>
    {
        private readonly Dictionary<string, List<Action<T>>> _eventDictionary = new Dictionary<string, List<Action<T>>>();
        private readonly object _lock = new object();

        #region Public API

        /// <summary>
        /// Registers a listener callback for a specific event.
        /// Multiple listeners can be registered for the same event.
        /// </summary>
        /// <param name="eventName">The unique identifier for the event. Cannot be null or empty.</param>
        /// <param name="listener">The callback to invoke when the event is dispatched. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when eventName or listener is null</exception>
        /// <exception cref="ArgumentException">Thrown when eventName is empty or whitespace</exception>
        public void AddListener(string eventName, Action<T> listener)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                Debug.LogError("[EventManager] Cannot add listener: eventName is null or empty");
                throw new ArgumentException("Event name cannot be null or empty", nameof(eventName));
            }

            if (listener == null)
            {
                Debug.LogError($"[EventManager] Cannot add null listener for event: {eventName}");
                throw new ArgumentNullException(nameof(listener), "Listener cannot be null");
            }

            lock (_lock)
            {
                if (!_eventDictionary.ContainsKey(eventName))
                {
                    _eventDictionary[eventName] = new List<Action<T>>();
                }

                if (!_eventDictionary[eventName].Contains(listener))
                {
                    _eventDictionary[eventName].Add(listener);
                }
                else
                {
                    Debug.LogWarning($"[EventManager] Listener already registered for event: {eventName}");
                }
            }
        }

        /// <summary>
        /// Removes a previously registered listener for a specific event.
        /// Safe to call even if the listener was not registered.
        /// </summary>
        /// <param name="eventName">The event identifier. Cannot be null or empty.</param>
        /// <param name="listener">The callback to remove. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when eventName or listener is null</exception>
        public void RemoveListener(string eventName, Action<T> listener)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                Debug.LogWarning("[EventManager] Cannot remove listener: eventName is null or empty");
                return;
            }

            if (listener == null)
            {
                Debug.LogWarning($"[EventManager] Cannot remove null listener for event: {eventName}");
                return;
            }

            lock (_lock)
            {
                if (_eventDictionary.TryGetValue(eventName, out var listeners))
                {
                    listeners.Remove(listener);

                    // Clean up empty event entries to prevent memory buildup
                    if (listeners.Count == 0)
                    {
                        _eventDictionary.Remove(eventName);
                    }
                }
            }
        }

        /// <summary>
        /// Removes all registered event listeners.
        /// Use with caution as this will affect all systems using this EventManager instance.
        /// </summary>
        public void RemoveAllEvents()
        {
            lock (_lock)
            {
                _eventDictionary.Clear();
            }
        }

        /// <summary>
        /// Dispatches an event to all registered listeners.
        /// Listeners are invoked in the order they were registered.
        /// If a listener throws an exception, it will be logged but other listeners will still execute.
        /// </summary>
        /// <param name="eventName">The event identifier. Cannot be null or empty.</param>
        /// <param name="eventData">The data to pass to all listeners</param>
        /// <exception cref="ArgumentNullException">Thrown when eventName is null</exception>
        public void DispatchEvent(string eventName, T eventData)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                Debug.LogWarning("[EventManager] Cannot dispatch: eventName is null or empty");
                return;
            }

            List<Action<T>> listenersToInvoke = null;

            lock (_lock)
            {
                if (_eventDictionary.TryGetValue(eventName, out var listeners))
                {
                    // Create a copy to avoid modification during iteration
                    listenersToInvoke = new List<Action<T>>(listeners);
                }
            }

            if (listenersToInvoke != null && listenersToInvoke.Count > 0)
            {
                foreach (var listener in listenersToInvoke)
                {
                    try
                    {
                        listener?.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[EventManager] Exception in event listener for '{eventName}': {ex.Message}\n{ex.StackTrace}");
                    }
                }
            }
        }

        /// <summary>
        /// Gets the number of listeners registered for a specific event.
        /// Useful for debugging and diagnostics.
        /// </summary>
        /// <param name="eventName">The event identifier</param>
        /// <returns>The number of listeners, or 0 if the event has no listeners</returns>
        public int GetListenerCount(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return 0;
            }

            lock (_lock)
            {
                return _eventDictionary.TryGetValue(eventName, out var listeners) ? listeners.Count : 0;
            }
        }

        /// <summary>
        /// Checks if any listeners are registered for a specific event.
        /// </summary>
        /// <param name="eventName">The event identifier</param>
        /// <returns>True if at least one listener is registered, false otherwise</returns>
        public bool HasListeners(string eventName)
        {
            return GetListenerCount(eventName) > 0;
        }

        #endregion
    }
}
/*
 Example to use
private EventManager<T> m_EventManager;

m_EventManager.AddListener(GameConstants.E__DEATH, DeathItis);
m_EventManager.RemoveListener(GameConstants.E__DEATH, DeathItis);

private void DeathItis(T a_data)
    {
        //Logic
    }
*/

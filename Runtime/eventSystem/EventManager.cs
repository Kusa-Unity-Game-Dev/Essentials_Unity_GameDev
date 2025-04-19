using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager <T>
{
    private Dictionary<string, List<Action<T>>> EventDictionary = new Dictionary<string, List<Action<T>>>();


    #region inner workings
    /// <summary>
    /// Registers a listener for a specific event.
    /// </summary>
    public void AddListener(string eventName, Action<T> listener)
    {
        if (!EventDictionary.ContainsKey(eventName))
        {
            EventDictionary[eventName] = new List<Action<T>>();
        }
        EventDictionary[eventName].Add(listener);
    }

    /// <summary>
    /// Removes a listener for a specific event.
    /// </summary>
    public void RemoveListener(string eventName, Action<T> listener)
    {
        if (EventDictionary.TryGetValue(eventName, out var sortedList))
        {
            sortedList.Remove(listener);
        }
    }

    ///remove all events
    public void RemoveAllEvents()
    {
        EventDictionary.Clear();
    }


    /// <summary>
    /// Dispatches an event to all listeners.
    /// </summary>
    public void DispatchEvent(string eventName, T eventData)
    {
        if (EventDictionary.TryGetValue(eventName, out var List))
        {
            foreach (var kvp in List)
            {
                kvp.Invoke(eventData);
            }
        }
    }

    #endregion
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

using System;
using UnityEngine;

public class FSM : MonoBehaviour, IEventListener<string>
{
    private static FSM s_Instance;
    private static FSM Instance => s_Instance;

    private EventManager<string> m_EventManager;
    
    #region essentials
    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            m_EventManager = new EventManager<string>();
            s_Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (s_Instance == this)
        {
            m_EventManager.RemoveAllEvents();
            s_Instance = null;
        }
    }

    private void Start()
    {
        
    }
    #endregion


    #region static methods
    public static EventManager<string> GetEventManager()
    {
        if (!s_Instance) return null;
        return s_Instance.m_EventManager;
    }

    public static void AddListener_s(string a_eventName, Action<string> listner)
    {
        if (!s_Instance) return;
        s_Instance.AddEventListener(a_eventName, listner);
    }

    public static void RemoveListener_s(string a_eventName, Action<string> listner)
    {
        if (!s_Instance) return;
        s_Instance.RemoveEventListener(a_eventName, listner);
    }

    public static void DispatchEvent_s(string a_eventName, string a_eventData)
    {
        if (!s_Instance) return;
        s_Instance.DispatchEvent(a_eventName, a_eventData);
    }

    #endregion


    #region IEventListener
    public void AddEventListener(string eventName, Action<string> listener)
    {
        if (m_EventManager == null)
        {
            m_EventManager = new EventManager<string>();
        }
        m_EventManager.AddListener(eventName, listener);
    }

    public void RemoveEventListener(string eventName, Action<string> listener)
    {
        if (m_EventManager == null)
        {
            return;
        }
        m_EventManager.RemoveListener(eventName, listener);
    }

    public void DispatchEvent(string a_eventName, string a_eventData)
    {
        if (m_EventManager == null)
        {
            return;
        }
        m_EventManager.DispatchEvent(a_eventName, a_eventData);
    }
    #endregion
}

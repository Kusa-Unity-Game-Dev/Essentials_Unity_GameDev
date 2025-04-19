using System;

public interface IEventListener <T>
{
    public void AddEventListener(string eventName, Action<T> listener);
    public void RemoveEventListener(string eventName, Action<T> listener);
    public void DispatchEvent(string a_eventName, T a_eventData);

}

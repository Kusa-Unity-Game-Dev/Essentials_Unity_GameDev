public class GameEvent
{
    public string eventName;

    public GameEvent(string name)
    {
        eventName = name;
    }

    public string EventName
    {
        get { return eventName; }
    }

    //setevent
    public void SetEventName(string name)
    {
        eventName = name;
    }
}

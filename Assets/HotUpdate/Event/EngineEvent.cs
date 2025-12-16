using System;

public class EngineEvent
{
    public object eventParams = null;
    public int eventType;
    public EngineEvent(int eventType, object eventParams = null)
    {
        this.eventType = eventType;
        this.eventParams = eventParams;
    }
}


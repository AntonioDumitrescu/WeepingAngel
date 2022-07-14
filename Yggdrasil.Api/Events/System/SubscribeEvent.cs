namespace Yggdrasil.Api.Events.System;

public enum EventPriority
{
    Lowest = -2,
    Low = -1,
    Normal = 0,
    High = 1,
    VeryHigh = 2,
    RealTime = 3
}

/// <summary>
///     Used to mark a method for receiving events.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class SubscribeEvent : Attribute
{
    public SubscribeEvent(EventPriority priority = EventPriority.Normal)
    {
        Priority = priority;
    }

    public SubscribeEvent(Type eventType, EventPriority priority = EventPriority.Normal)
    {
        Priority = priority;
        EventType = eventType;
    }

    public EventPriority Priority { get; set; }

    public Type? EventType { get; set; }
}
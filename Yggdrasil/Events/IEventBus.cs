using Yggdrasil.Api.Events.System;

namespace Yggdrasil.Events;

internal interface IEventBus
{
    Type EventType { get; }

    EventPriority Priority { get; }

    ValueTask InvokeAsync(object? eventHandler, object @event, IServiceProvider provider);
}
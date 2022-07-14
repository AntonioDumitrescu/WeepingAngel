﻿using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Yggdrasil.Api.Events.System;
using Yggdrasil.Api.Extensions;

namespace Yggdrasil.Events;

public sealed class EventManager : IEventManager
{
    private sealed class DisposeList : IDisposable
    {
        public DisposeList()
        {
            Disposables = new List<IDisposable>();
        }

        public List<IDisposable> Disposables { get; }

        public void Dispose()
        {
            foreach (var disposable in Disposables)
            {
                disposable.Dispose();
            }
        }
    }

    private readonly struct EventHandler
    {
        public EventHandler(IEventReceiver? o, IEventBus eventBus)
        {
            Object = o;
            EventBus = eventBus;
        }

        public IEventReceiver? Object { get; }

        public IEventBus EventBus { get; }

        public void Deconstruct(out IEventReceiver? o, out IEventBus eventBus)
        {
            o = Object;
            eventBus = EventBus;
        }
    }

    private readonly ConcurrentDictionary<Type, DeferredCallbackList> _temporaryEventListeners = new();
    private readonly ConcurrentDictionary<Type, List<EventHandler>> _sortedHandlers = new();
    private readonly ILogger<EventManager> _logger;
    private readonly IServiceProvider _serviceProvider;

    public EventManager(ILogger<EventManager> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public IDisposable AddReceiver<TListener>(TListener listener) where TListener : IEventReceiver
    {
        var disposeList = new DisposeList();
        var listeners = ExpressionEventBus.FromType(typeof(TListener));

        foreach (var eventExpressionListener in listeners)
        {
            var wrapper = new EventBusWrapper(eventExpressionListener, listener);
            var register = _temporaryEventListeners.GetOrAdd(wrapper.EventType,
                _ => new DeferredCallbackList());
            disposeList.Disposables.Add(register.Add(wrapper));
        }

        if (listeners.Count > 0)
        {
            _sortedHandlers.TryRemove(typeof(TListener), out _);
        }

        return disposeList;
    }

    private List<EventHandler> SortHandlers<TEvent>() where TEvent : IEvent
    {
        var handlers = GetHandlers<TEvent>()
            .OrderByDescending(e => e.EventBus.Priority)
            .ToList();

        _sortedHandlers[typeof(TEvent)] = handlers;

        return handlers;
    }

    public async ValueTask SendAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {

        try
        {
            if (!_sortedHandlers.TryGetValue(typeof(TEvent), out var handlers))
            {
                handlers = SortHandlers<TEvent>();
            }

            foreach (var (handler, eventListener) in handlers)
            {
                await eventListener.InvokeAsync(handler, @event, _serviceProvider);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error handling {type}: {ex}", typeof(TEvent), e);
        }
    }

    private IEnumerable<EventHandler> GetHandlers<TEvent>() where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        var interfaces = eventType.GetInterfaces();

        foreach (var @interface in interfaces)
        {
            if (_temporaryEventListeners.TryGetValue(@interface, out var callbackList))
            {
                foreach (var eventListener in callbackList.GetBusCollection())
                {
                    yield return new EventHandler(null, eventListener);
                }
            }
        }

        foreach (var handler in _serviceProvider.GetServices<IEventReceiver>())
        {
            var events = ExpressionEventBus.FromType(handler.GetType());

            foreach (var eventHandler in events)
            {
                if (eventHandler.EventType != typeof(TEvent) && !interfaces.Contains(eventHandler.EventType))
                {
                    continue;
                }

                yield return new EventHandler(handler, eventHandler);
            }
        }

        if (_temporaryEventListeners.TryGetValue(eventType, out var cl2))
        {
            foreach (var eventListener in cl2.GetBusCollection())
            {
                yield return new EventHandler(null, eventListener);
            }
        }
    }
}
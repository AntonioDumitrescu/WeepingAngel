using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Yggdrasil.Api.Events.System;
using Yggdrasil.Events;

IHostBuilder CreateHostBuilder() =>
    Host.CreateDefaultBuilder()
        .ConfigureServices((_, services) =>
            services.AddSingleton<EventManager>().AddLogging());

var host = CreateHostBuilder().Build();
var manager = host.Services.GetRequiredService<EventManager>();
manager.AddReceiver(new EventReceiver());

var sent = 0L;

_ = Task.Run(async () =>
{
    var startTime = DateTime.Now;

    while (true)
    {
        await Task.Delay(200);
        Console.Clear();
        var elapsed = (DateTime.Now - startTime).TotalSeconds;
        Console.WriteLine($"Sent: {sent}, Received: {EventReceiver.received}");
        Console.WriteLine($"Average rate [sending]  : {sent / elapsed} events / second");
        Console.WriteLine($"Average rate [receiving]: {EventReceiver.received / elapsed} events / second");
    }
});

while (true)
{
    await manager.SendAsync(new Event());
    Interlocked.Increment(ref sent);
}

class Event : IEvent { }

class EventReceiver : IEventReceiver
{
    public static long received = 0L;

    [SubscribeEvent]
    public void ReceiveEvent(Event @event, ILogger<EventReceiver> logger, EventManager manager)
    {
        Interlocked.Increment(ref received);
    }
}
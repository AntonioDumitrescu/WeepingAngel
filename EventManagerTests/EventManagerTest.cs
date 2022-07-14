using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Yggdrasil.Api.Events;
using Yggdrasil.Api.Events.System;
using Yggdrasil.Events;

namespace EventManagerTests
{
    public class Tests
    {
        private IHostBuilder GetDefaultBuilder => Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
                services.AddSingleton<EventManager>().AddLogging());

        [Test]
        public async Task TestVoidMethod()
        {
            var host = GetDefaultBuilder.Build();
            var manager = host.Services.GetRequiredService<EventManager>();
            var tcs = new TaskCompletionSource();
            manager.AddReceiver(new TestVoidMethodReceiver(tcs));
            await manager.SendAsync(new Event());
            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.Pass();
        }

        [Test]
        public async Task TestValueTask()
        {
            var host = GetDefaultBuilder.Build();
            var manager = host.Services.GetRequiredService<EventManager>();
            var tcs = new TaskCompletionSource();
            manager.AddReceiver(new TestValueTaskReceiver(tcs));
            await manager.SendAsync(new Event());
            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.Pass();
        }

        [Test]
        public async Task TestVoidMethodWithInjection()
        {
            var host = GetDefaultBuilder.Build();
            var manager = host.Services.GetRequiredService<EventManager>();
            var tcs = new TaskCompletionSource();
            manager.AddReceiver(new TestVoidMethodWithInjectionReceiver(tcs));
            await manager.SendAsync(new Event());
            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.Pass();
        }

        [Test]
        public async Task TestPriorities()
        {
            var host = GetDefaultBuilder.Build();
            var manager = host.Services.GetRequiredService<EventManager>();
            var tcs = new TaskCompletionSource();
            var @event = new Event();
            manager.AddReceiver(new TestPriorityReceiver(tcs, @event));
            await manager.SendAsync(@event);
            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.Pass();
        }

        [Test]
        public async Task TestPrioritiesWithInjection()
        {
            var host = GetDefaultBuilder.Build();
            var manager = host.Services.GetRequiredService<EventManager>();
            var tcs = new TaskCompletionSource();
            var @event = new Event();
            manager.AddReceiver(new TestPriorityWithInjectionReceiver(tcs, @event));
            await manager.SendAsync(@event);
            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.Pass();
        }

        [Test]
        public async Task TestPrioritiesWithInjectionAndMultipleReceivers()
        {
            var host = GetDefaultBuilder.Build();
            var manager = host.Services.GetRequiredService<EventManager>();
            var tcs1 = new TaskCompletionSource();
            var tcs2 = new TaskCompletionSource();
            var tcs3 = new TaskCompletionSource();
            var @event = new Event();
            manager.AddReceiver(new TestPriorityWithInjectionReceiver(tcs1, @event));
            manager.AddReceiver(new TestPriorityWithInjectionReceiver(tcs2, @event));
            manager.AddReceiver(new TestPriorityWithInjectionReceiver(tcs3, @event));
            await manager.SendAsync(@event);
            await tcs1.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await tcs2.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await tcs3.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.Pass();
        }

        class Event : IEvent { }

        private class TestVoidMethodReceiver : IEventReceiver
        {
            private readonly TaskCompletionSource _tcs;

            public TestVoidMethodReceiver(TaskCompletionSource tcs)
            {
                _tcs = tcs;
            }

            [SubscribeEvent]
            private void ReceiveEvent(Event @event)
            {
                Assert.IsNotNull(@event);
                _tcs.SetResult();
            }
        }

        private class TestValueTaskReceiver : IEventReceiver
        {
            private readonly TaskCompletionSource _tcs;

            public TestValueTaskReceiver(TaskCompletionSource tcs)
            {
                _tcs = tcs;
            }

            [SubscribeEvent]
            public async ValueTask ReceiveEvent(Event @event)
            {
                Assert.IsNotNull(@event);
                _tcs.SetResult();
            }
        }

        private class TestVoidMethodWithInjectionReceiver : IEventReceiver
        {
            private readonly TaskCompletionSource _tcs;

            public TestVoidMethodWithInjectionReceiver(TaskCompletionSource tcs)
            {
                _tcs = tcs;
            }

            [SubscribeEvent]
            public void ReceiveEvent(Event @event, ILogger<TestVoidMethodWithInjectionReceiver> logger, EventManager manager)
            {
                Assert.IsNotNull(@event);
                Assert.IsNotNull(logger);
                Assert.IsNotNull(manager);
                _tcs.SetResult();
            }
        }

        private class TestPriorityReceiver : IEventReceiver
        {
            private readonly TaskCompletionSource _tcs;
            private readonly object _expected;
            private int _index;

            public TestPriorityReceiver(TaskCompletionSource tcs, object expected)
            {
                _tcs = tcs;
                _expected = expected;
            }

            [SubscribeEvent(EventPriority.Lowest)]
            public void Lowest(Event e)
            {
                Assert.IsTrue(_expected == e, "received wrong event");
                Assert.IsTrue(_index++ == 5, "priority broken");
                _tcs.SetResult();
            }

            [SubscribeEvent(EventPriority.Low)]
            public void Low(Event e)
            {
                Assert.IsTrue(_expected == e, "received wrong event");
                Assert.IsTrue(_index++ == 4, "priority broken");
            }

            [SubscribeEvent(EventPriority.Normal)]
            public void Normal(Event e)
            {
                Assert.IsTrue(_expected == e, "received wrong event");
                Assert.IsTrue(_index++ == 3, "priority broken");
            }

            [SubscribeEvent(EventPriority.High)]
            public void High(Event e)
            {
                Assert.IsTrue(_expected == e, "received wrong event");
                Assert.IsTrue(_index++ == 2, "priority broken");
            }

            [SubscribeEvent(EventPriority.VeryHigh)]
            public void VeryHigh(Event e)
            {
                Assert.IsTrue(_expected == e, "received wrong event");
                Assert.IsTrue(_index++ == 1, "priority broken");
            }

            [SubscribeEvent(EventPriority.RealTime)]
            public void RealTime(Event e)
            {
                Assert.IsTrue(_expected == e, "received wrong event");
                Assert.IsTrue(_index++ == 0, "priority broken");
            }
        }

        private class TestPriorityWithInjectionReceiver : IEventReceiver
        {
            private readonly TaskCompletionSource _tcs;
            private readonly object _expected;
            private int _index;

            public TestPriorityWithInjectionReceiver(TaskCompletionSource tcs, object expected)
            {
                _tcs = tcs;
                _expected = expected;
            }

            [SubscribeEvent(EventPriority.Lowest)]
            public void Lowest(Event e, ILogger<TestPriorityWithInjectionReceiver> logger, EventManager manager, IServiceProvider sp)
            {
                Assert.IsNotNull(logger);
                Assert.IsNotNull(manager);
                Assert.IsNotNull(sp);

                Assert.IsTrue(_expected == e, "received wrong event");
                Assert.IsTrue(_index++ == 5, "priority broken");
                _tcs.SetResult();
            }

            [SubscribeEvent(EventPriority.Low)]
            public void Low(Event e, ILogger<TestPriorityWithInjectionReceiver> logger, EventManager manager, IServiceProvider sp)
            {
                Assert.IsNotNull(logger);
                Assert.IsNotNull(manager);
                Assert.IsNotNull(sp);

                Assert.IsTrue(_expected == e, "received wrong event");
                Assert.IsTrue(_index++ == 4, "priority broken");
            }

            [SubscribeEvent(EventPriority.Normal)]
            public void Normal(Event e, ILogger<TestPriorityWithInjectionReceiver> logger, EventManager manager, IServiceProvider sp)
            {
                Assert.IsNotNull(logger);
                Assert.IsNotNull(manager);
                Assert.IsNotNull(sp);

                Assert.IsTrue(_expected == e, "received wrong event");
                Assert.IsTrue(_index++ == 3, "priority broken");
            }

            [SubscribeEvent(EventPriority.High)]
            public void High(Event e, ILogger<TestPriorityWithInjectionReceiver> logger, EventManager manager, IServiceProvider sp)
            {
                Assert.IsNotNull(logger);
                Assert.IsNotNull(manager);
                Assert.IsNotNull(sp);

                Assert.IsTrue(_expected == e, "received wrong event");
                Assert.IsTrue(_index++ == 2, "priority broken");
            }

            [SubscribeEvent(EventPriority.VeryHigh)]
            public void VeryHigh(Event e, ILogger<TestPriorityWithInjectionReceiver> logger, EventManager manager, IServiceProvider sp)
            {
                Assert.IsNotNull(logger);
                Assert.IsNotNull(manager);
                Assert.IsNotNull(sp);

                Assert.IsTrue(_expected == e, "received wrong event");
                Assert.IsTrue(_index++ == 1, "priority broken");
            }

            [SubscribeEvent(EventPriority.RealTime)]
            public void RealTime(Event e, ILogger<TestPriorityWithInjectionReceiver> logger, EventManager manager, IServiceProvider sp)
            {
                Assert.IsNotNull(logger);
                Assert.IsNotNull(manager);
                Assert.IsNotNull(sp);

                Assert.IsTrue(_expected == e, "received wrong event");
                Assert.IsTrue(_index++ == 0, "priority broken");
            }
        }
    }
}
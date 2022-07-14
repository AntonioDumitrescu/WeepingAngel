using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Yggdrasil.Api;

namespace ProfilerTests
{
    public class ProfilerTest
    {
        private const int MarginOfError = 50;

        [Test]
        public async Task TestBeginEndSection()
        {
            var profiler = new Profiler();

            profiler.BeginSection("section");
            await Task.Delay(1000);
            profiler.EndSection("section");

            Assert.IsTrue(Math.Abs(1000 - profiler.GetSection("section").ElapsedMilliseconds) < MarginOfError);

            profiler.BeginSection("section");
            await Task.Delay(200);
            profiler.EndSection("section");

            Assert.IsTrue(Math.Abs(1200 - profiler.GetSection("section").ElapsedMilliseconds) < MarginOfError);
        }

        [Test]
        public async Task TestStack()
        {
            var profiler = new Profiler();

            profiler.PushSection("section");
            await Task.Delay(1000);
            profiler.PopSection();

            Assert.IsTrue(Math.Abs(1000 - profiler.GetSection("section").ElapsedMilliseconds) < MarginOfError);

            profiler.PushSection("section");
            await Task.Delay(200);

            Assert.IsTrue(Math.Abs(1200 - profiler.PopSectionRemove().ElapsedMilliseconds) < MarginOfError);
        }
    }
}
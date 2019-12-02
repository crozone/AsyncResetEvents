using crozone.AsyncResetEvents;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class UnitTests
    {
        [Fact]
        public async Task BasicTest()
        {
            AsyncAutoResetEvent asyncAutoResetEvent = new AsyncAutoResetEvent();

            Task triggerTask = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                asyncAutoResetEvent.Set();
            });

            await asyncAutoResetEvent.WaitAsync();
        }

        [Fact]
        public async Task BasicTestWithTimeout()
        {
            AsyncAutoResetEvent asyncAutoResetEvent = new AsyncAutoResetEvent();

            Task triggerTask = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                asyncAutoResetEvent.Set();
            });

            Assert.True(await asyncAutoResetEvent.WaitAsync(TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public async Task BasicTestWithTimeoutTriggered()
        {
            AsyncAutoResetEvent asyncAutoResetEvent = new AsyncAutoResetEvent();

            Task triggerTask = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                asyncAutoResetEvent.Set();
            });

            Assert.False(await asyncAutoResetEvent.WaitAsync(TimeSpan.FromSeconds(1)));
        }
    }
}

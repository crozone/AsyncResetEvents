using crozone.AsyncResetEvents;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class UnitTests
    {
        [Fact]
        public void Test1()
        {
            AsyncAutoResetEvent asyncAutoResetEvent = new AsyncAutoResetEvent();

            Task awaitingTask = Task.Run(async () =>
            {
                await asyncAutoResetEvent.WaitAsync();
            });


        }
    }
}

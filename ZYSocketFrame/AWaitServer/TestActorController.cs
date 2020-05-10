using Microsoft.Extensions.Logging;
using Netx.Actor;
using Netx.Loggine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AWaitServer
{
    //[ActorOption(maxQueueCount: 1000, ideltime: 3000)]
    public class TestActorController : ActorController, ITestActorController
    {
        public ILog Log { get; }

        public TestActorController(ILogger<TestActorController> logger)
        {
            Log = new DefaultLog(logger);
        }

        public async Task Run()
        {
            await Task.Delay(1000);
        }
    }
}

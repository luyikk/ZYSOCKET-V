using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZYSocket.FiberStream.Synchronization
{

    internal class LineByLineScheduler : SyncScheduler
    {
        public override Task Scheduler(Func<Task> action) => action();
    }


    internal class TaskScheduler : SyncScheduler
    {
        public override Task Scheduler(Func<Task> action) => Task.Factory.StartNew(action, TaskCreationOptions.DenyChildAttach);
    }

    public abstract class SyncScheduler
    {

        public static SyncScheduler LineByLine { get => new LineByLineScheduler(); }
        public static SyncScheduler TaskFactory { get => new TaskScheduler(); }

        public abstract Task Scheduler(Func<Task> action);
    }
}

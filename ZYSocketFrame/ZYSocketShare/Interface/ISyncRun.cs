using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ZYSocket.FiberStream.Synchronization
{
    public interface ISyncRun
    {
        ConcurrentQueue<SyncMessage> SyncRunQueue { get; }
        SyncScheduler SyncScheduler { get; }

        ValueTask Ask(Action action);
        ValueTask<dynamic> Ask(Func<dynamic> func);
        void Tell(Action action);
        ValueTask Delay<T>(int millisecondsDelay, Func<Task<T>> func);
    }
}
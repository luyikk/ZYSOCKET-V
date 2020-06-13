using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZYSocket.FiberStream.Synchronization
{
    public class SyncRun : ISyncRun
    {
        public const int Idle = 0;
        public const int Open = 1;

        private int status = Idle;

        private int delaystatus = Idle;

        private readonly Lazy<ConcurrentQueue<SyncMessage>> syncRunQueue;

        public ConcurrentQueue<SyncMessage> SyncRunQueue { get => syncRunQueue.Value; }


        public SyncScheduler SyncScheduler { get; }
     
        public SyncRun()
        {
            SyncScheduler = SyncScheduler.LineByLine;          
            syncRunQueue = new Lazy<ConcurrentQueue<SyncMessage>>(System.Threading.LazyThreadSafetyMode.PublicationOnly);
        }

        public SyncRun(SyncScheduler syncScheduler)
        {
            SyncScheduler = syncScheduler;           
            syncRunQueue = new Lazy<ConcurrentQueue<SyncMessage>>(System.Threading.LazyThreadSafetyMode.PublicationOnly);
        }

        public void Tell(Action action)
        {
            var sync = new SyncMessage<object>(0, action);
            SyncRunQueue.Enqueue(sync);
            Runing().Wait();          
        }

        public async ValueTask Ask(Action action)
        {

            var sync = new SyncMessage<object>(1, action);
            SyncRunQueue.Enqueue(sync);
            Runing().Wait();
            await sync.Awaiter;
        }


        public ValueTask<dynamic> Ask(Func<dynamic> func)
        {
            var sync = new SyncMessage<dynamic>(2, func);
            SyncRunQueue.Enqueue(sync);
            Runing().Wait();
            return sync.Awaiter;
        }


        public async ValueTask<T> Delay<T>(int millisecondsDelay, Func<Task<T>> func)
        {
            if (Interlocked.Exchange(ref delaystatus, Open) == Idle)
            {
                await Task.Delay(millisecondsDelay);
                Interlocked.CompareExchange(ref delaystatus, Idle, Open);
                return await await Ask(func);
            }

            return default!;
        }


        public async ValueTask Delay(int millisecondsDelay, Func<Task> func)
        {
            if (Interlocked.Exchange(ref delaystatus, Open) == Idle)
            {
                await Task.Delay(millisecondsDelay);
                Interlocked.CompareExchange(ref delaystatus, Idle, Open);
                await await Ask(func);
            }
        }



        private Task Runing()
        {
            if (Interlocked.Exchange(ref status, Open) == Idle)
            {
                async Task RunNext()
                {
                    try
                    {
                       
                        while (SyncRunQueue.TryDequeue(out SyncMessage msg))
                        {
                            try
                            {
                                var res = await Call_runing(msg);                              
                                msg.Completed(res);

                            }
                            catch (Exception er)
                            {
                                msg.SetException(er);
                            }
                        }

                        
                    }
                    finally
                    {
                        Interlocked.CompareExchange(ref status, Idle, Open);
                    }
                };

                return SyncScheduler.Scheduler(RunNext);
            }

            return Task.CompletedTask;
        }

        private async Task<object?> Call_runing(SyncMessage sync)
        {
            switch (sync.RunType)
            {
                case 0:
                    {
                        var call = (Action)sync.Args;
                        call?.Invoke();
                        return default;
                    }
                case 1:
                    {
                        var call = (Action)sync.Args;
                        call?.Invoke();
                        return default;
                    }
                case 2:
                    {
                        var call = (Func<dynamic>)sync.Args;
                        return await Task.FromResult(call?.Invoke());
                    }

            }

            throw new NotSupportedException($"not find run type{sync.RunType}");
        }

    }
}

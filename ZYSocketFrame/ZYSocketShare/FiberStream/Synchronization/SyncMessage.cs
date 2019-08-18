using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources.Copy;

namespace ZYSocket.FiberStream.Synchronization
{
    public abstract class SyncMessage
    {
        public byte RunType { get; }
        public object Args { get; }

        public SyncMessage(byte runType,object args)
        {
            RunType = runType;
            Args = args;
        }

        public abstract void Completed(object result);
        public abstract void SetException(Exception error);

    }

    public class SyncMessage<T> : SyncMessage
    {
        internal ManualResetValueTaskSource<T> TaskSource { get; }
        internal ValueTask<T> Awaiter { get; }

        public SyncMessage(byte runType,object args)
            :base(runType,args)
        {
            TaskSource = new ManualResetValueTaskSource<T>();
            Awaiter = new ValueTask<T>(TaskSource, TaskSource.Version);
        }

        public override void Completed(object result)
        {
            TaskSource.SetResult((T)result);
        }

        public override void SetException(Exception error)
        {
            TaskSource.SetException(error);
        }
    }

   
}

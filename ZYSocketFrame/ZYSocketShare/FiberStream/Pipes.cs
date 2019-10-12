using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;


namespace ZYSocket.FiberStream
{

    public sealed class ManualResetValueTask<T> : IValueTaskSource<T>, IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<T> _core; // mutable struct; do not make this readonly

        public bool RunContinuationsAsynchronously { get => _core.RunContinuationsAsynchronously; set => _core.RunContinuationsAsynchronously = value; }
        public short Version => _core.Version;
        public void Reset() => _core.Reset();
        public void SetResult(T result) => _core.SetResult(result);
        public void SetException(Exception error) => _core.SetException(error);

        public T GetResult(short token) => _core.GetResult(token);
        void IValueTaskSource.GetResult(short token) => _core.GetResult(token);
        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);
        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
    }

    public class Pipes
    {

        //PipeFilberAwaiter read = new PipeFilberAwaiter();

        //public void Close()
        //{
        //    read.Close();
        //}



        //public void Advance(int len)
        //{
        //    if (!read.IsCompleted)
        //    {
        //        read.SetResult(len);
        //        read.Completed();
        //    }
        //}

        //public PipeFilberAwaiter Need()
        //{
        //    read.Reset();
        //    return read;
        //}


        readonly ManualResetValueTask<int> source_read = new ManualResetValueTask<int>();

        private int wl;

        public void Close()
        {            
            source_read.Reset();
            wl = 0;
        }

        public void Advance(int len)
        {
            wl = len;
            if (source_read.GetStatus(source_read.Version) == ValueTaskSourceStatus.Pending)
            {
                source_read.SetResult(wl);
            }
        }

        public ValueTask<int> Need()
        {
            source_read.Reset();
            return new ValueTask<int>(source_read, source_read.Version);
        }

    }
}

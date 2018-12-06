using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;

namespace ZYSocket.FiberStream
{

    public class PipeFilberAwaiter : ICriticalNotifyCompletion, INotifyCompletion
    {


        private Action Continuation;

        private PipeResult fiberResult;

        public bool IsNull => Continuation == null;

        public PipeFilberAwaiter(bool iscompleted=false)
        {
            this.iscompleted = iscompleted;
            Continuation = null;
            fiberResult = default;
        }

        internal void Completed()
        {
            iscompleted = true;
            Continuation?.Invoke();

        }


        internal void Reset()
        {
            this.iscompleted = false;
            Continuation = null;
            fiberResult = default;
        }

        internal void SetResult(PipeResult result)
        {
            fiberResult = result;
        }

        private bool iscompleted;

        public bool IsCompleted { get { return iscompleted; } }

        public void OnCompleted(Action continuation)
        {
            this.Continuation = continuation;
        }

        public void UnsafeOnCompleted(Action continuation)
        {

            this.Continuation = continuation;
        }

        public PipeFilberAwaiter GetAwaiter() => this;

        public PipeResult GetResult()
        {
            return fiberResult;
        }



    }

    public struct PipeResult
    {
        public PipeResult(bool isCanceled,int byteLength)
        {
            this.IsCanceled = isCanceled;
            this.ByteLength = byteLength;
        }

        public bool IsCanceled { get;  }

        public int ByteLength { get;  }

        public override bool Equals(object obj) =>
         obj is PipeResult && Equals((PipeResult)obj);
        public bool Equals(PipeResult other) => ByteLength == other.ByteLength && IsCanceled == other.IsCanceled;

        public override int GetHashCode() => ByteLength.GetHashCode() + IsCanceled.GetHashCode();

        public static bool operator == (PipeResult left, PipeResult right) =>left.Equals(right);

        public static bool operator !=(PipeResult left, PipeResult right) => !left.Equals(right);
    }


    public class ResultAwaiter<T> : ICriticalNotifyCompletion, INotifyCompletion
    {


        private Action Continuation;

        private T result;
        internal void Completed()
        {
            iscompleted = true;
            Continuation?.Invoke();
        }

        internal void Reset()
        {            
            iscompleted = false;
        }


        internal void SetResult(T res)
        {
            result = res;
        }



        private bool iscompleted = false;

        public bool IsCompleted { get { return iscompleted; } }

        public void OnCompleted(Action continuation)
        {
            this.Continuation = continuation;
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            this.Continuation = continuation;
        }

        public ResultAwaiter<T> GetAwaiter() => this;


        public T GetResult()
        {
            return result;
        }
    }


    public class StreamInitAwaiter : ResultAwaiter<bool>
    {

    }

}

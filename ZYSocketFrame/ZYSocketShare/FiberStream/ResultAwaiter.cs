using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ZYSocket.FiberStream
{

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

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;

namespace ZYSocket.FiberStream
{

    public class PipeFilberAwaiter : ICriticalNotifyCompletion, INotifyCompletion
    {
        private bool is_can_set = false;

        private Action Continuation;

        private int count;

        public bool IsNull => Continuation == null;

        public PipeFilberAwaiter(bool iscompleted=false)
        {
            this.iscompleted = iscompleted;
            Continuation = null;
            count = 0;
        }

        internal void Completed()
        {
            iscompleted = true;
            Continuation?.Invoke();

        }

        internal void Init()
        {
            is_can_set = true;
        }

        internal void Close()
        {
            is_can_set = false;
            this.iscompleted = false;
            Continuation = null;
            count = 0;
        }

      

        internal void Reset()
        {
            
            this.iscompleted = false;
            Continuation = null;
            count = 0;
        }

        internal void SetResult(int len)
        {
            count = len;
        }

        private bool iscompleted;

        public bool IsCompleted { get { return iscompleted; } }

        public void OnCompleted(Action continuation)
        {
            if (is_can_set)
                this.Continuation = continuation;
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (is_can_set)
                this.Continuation = continuation;
        }

        public PipeFilberAwaiter GetAwaiter() => this;

        public int GetResult()
        {
            return count;
        }



    }

  


    

}

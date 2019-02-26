using System.Threading;

namespace ZYSocket.FiberStream
{


    public class Pipes
    {
     
        PipeFilberAwaiter write = new PipeFilberAwaiter();
        PipeFilberAwaiter read = new PipeFilberAwaiter();

        private int wl;
        private int rl;   


        public void Close()
        {
            write.Close();
            read.Close();
          
            wl = 0;
            rl = 0;
        }

        public void Init()
        {
            write.Init();
            read.Init();
        }


        public PipeFilberAwaiter ReadCanceled()
        {
            write.Reset();         

            if (!read.IsCompleted)
            {              
                read.SetResult(new PipeResult(true, 0));
                read.Completed();
               
            }

            return write;
        }

        public PipeFilberAwaiter Advance(int len, CancellationToken cancellationTokenSource = default)
        {
          
            wl = len;
           // write.Reset();

            if (!read.IsCompleted)
            {
                read.SetResult(new PipeResult(cancellationTokenSource.IsCancellationRequested, wl));
                read.Completed();
            }

         
            return write;


        }

        public PipeFilberAwaiter Need(int len = 0, CancellationToken cancellationTokenSource = default)
        {
            
            rl = len;
            read.Reset();

            if (!write.IsCompleted)
            {
                write.SetResult(new PipeResult(cancellationTokenSource.IsCancellationRequested, rl));
                write.Completed();
            }

           
            return read;

        }

        public PipeFilberAwaiter RetBack()
        {
            read.Reset();

            if (!write.IsCompleted)
            {
                write.SetResult(new PipeResult(true, 0));
                write.Completed();
                
            }           

            return read;
        }
    }
}

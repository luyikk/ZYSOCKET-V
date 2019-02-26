using System.Threading;

namespace ZYSocket.FiberStream
{


    public class Pipes
    {
     
        PipeFilberAwaiter write = new PipeFilberAwaiter();
        PipeFilberAwaiter read = new PipeFilberAwaiter();

        private int wl;
    

        public void Close()
        {
            write.Close();
            read.Close();
          
            wl = 0;           
        }

        public void Init()
        {
            write.Init();
            read.Init();
        }
        
     

        public PipeFilberAwaiter Advance(int len)
        {
          
            wl = len;
            
            if (!read.IsCompleted)
            {
                read.SetResult(wl);
                read.Completed();
            }
         
            return write;


        }

        public PipeFilberAwaiter Need()
        {            
          
            read.Reset();

            if (!write.IsCompleted)
            {
                write.SetResult(0);
                write.Completed();
            }

           
            return read;

        }

     
    }
}

using System.Threading;

namespace ZYSocket.FiberStream
{


    public class Pipes
    {
          
        PipeFilberAwaiter read = new PipeFilberAwaiter();

        private int wl;
    

        public void Close()
        {          
            read.Close();          
            wl = 0;           
        }

        public void Init()
        {
            read.Init();
        }
        
     

        public void Advance(int len)
        {          
            wl = len;
            
            if (!read.IsCompleted)
            {
                read.SetResult(wl);
                read.Completed();
            }
        }

        public PipeFilberAwaiter Need()
        {            
            read.Reset();
            return read;
        }

     
    }
}

using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace ZYSocket.FiberStream
{


    public class Pipes
    {

        PipeFilberAwaiter read = new PipeFilberAwaiter();

        public void Close()
        {
            read.Close();
        }



        public void Advance(int len)
        {
            if (!read.IsCompleted)
            {
                read.SetResult(len);
                read.Completed();
            }
        }

        public PipeFilberAwaiter Need()
        {
            read.Reset();
            return read;
        }


        //readonly ManualResetValueTaskSource<int> source_read = new ManualResetValueTaskSource<int>();

        //private int wl;

        //public void Close()
        //{
        //    source_read.Reset();
        //    wl = 0;
        //}

        //public void Advance(int len)
        //{
        //    wl = len;
        //    if (source_read.GetStatus(source_read.Version) == ValueTaskSourceStatus.Pending)
        //    {
        //        source_read.SetResult(wl);
        //    }
        //}

        //public ValueTask<int> Need()
        //{
        //    source_read.Reset();
        //    return new ValueTask<int>(source_read, source_read.Version);
        //}

    }
}

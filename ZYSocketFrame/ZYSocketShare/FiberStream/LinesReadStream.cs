using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;


namespace ZYSocket.FiberStream
{
    public class LinesReadStream : Stream, IFiberReadStream
    {
        private readonly object lockobj = new object();

        private readonly PipeFilberAwaiter check_completed = new PipeFilberAwaiter(true);

        private readonly Pipes Pipes;

        private readonly byte[] data;

        private int offset;

        private readonly int len;

        private long wrlen;

        private long position;
    

        private StreamInitAwaiter InitAwaiter;    
        
        private readonly byte[] numericbytes = new byte[8];

        public Func<byte[], int, int, AsyncCallback,object, IAsyncResult> BeginReadFunc { get; set; }
        public Func<IAsyncResult,int> EndBeginReadFunc { get; set; }
        public Action ServerReceive { get; set; }

        public byte[]  Numericbytes { get => numericbytes; }

        public int Size => len;

        bool isBeginRw = true;
        /// <summary>
        /// 此参数只是为了对.NET FX SSL问题进行修复,因为.net fx sslstream调用的是Begin方法
        /// </summary>
        public bool IsBeginRaw { get=>IsBeginRaw; set { isBeginRw = value; } }

        public LinesReadStream(int length = 4096)
        {
            Pipes = new Pipes();
            data = new byte[length];
            len = length;          
        }


        public void Advance(int len)
        {
            wrlen = len;
            position = 0;
            Pipes.Advance(len);
        }

    


        public Memory<byte> GetMemory(int inithnit)
        {
            return new Memory<byte>(data, offset, get_have_length());
        }

        public ArraySegment<byte> GetArray(int inithnit)
        {
            return new ArraySegment<byte>(data, offset, get_have_length());
        }

        private int get_have_length()
        {
            return len - offset;
        }

        private long have_current_length()
        {
            return wrlen - position;
        }

        public bool HaveData()
        {
            if (position < wrlen)
                return true;
            else
                return false;
        }


        public  PipeFilberAwaiter Check()
        {
            if (position == wrlen)
                return Pipes.Need();
            else            
                return check_completed;            
        }

        public void Reset()
        {
            if (InitAwaiter != null)
                InitAwaiter.Reset();
            Pipes.Close();
            position = 0;
            wrlen = 0;
            offset = 0;
            isBeginRw=true;
          
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => wrlen;

        public override long Position { get => position; set => position = value; }



        public override void Flush()
        {

        }



        public StreamInitAwaiter WaitStreamInit()
        {
          

            if (InitAwaiter != null)
                return InitAwaiter;
            else
            {
                InitAwaiter = new StreamInitAwaiter();
                return InitAwaiter;
            }
        }

        public void StreamInit()
        {
            if (InitAwaiter != null)
            {
                Pipes.Init();
                InitAwaiter.SetResult(true);
                InitAwaiter.Completed();             
            }
            else
                throw new Exception("Please call first  WaitStreamInit");
        }


        public void UnStreamInit()
        {
            if (InitAwaiter != null)
            {
                InitAwaiter.SetResult(false);
                InitAwaiter.Completed();
            }
            else
                throw new Exception("Please call first  WaitStreamInit");
        }


     

        public override int Read(byte[] buffer, int offset, int count)
        {
            unsafe
            {              

                var cpbytes = wrlen - position;
                if (cpbytes == 0)
                    return 0;

                fixed (byte* source = &data[this.offset + position])
                fixed (byte* target = &buffer[offset])
                {
                   
                    if (cpbytes > count)                    
                        cpbytes = count;                    

                    Buffer.MemoryCopy(source, target, count, cpbytes);
                    position += cpbytes;

                    return (int)cpbytes;
                }
            }
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            ServerReceive();
            await Check();
            return  Read(buffer, offset, count);
        }


        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (isBeginRw)
            {
                return BeginReadFunc.Invoke(buffer, offset, count, callback, state);
            }
            else
            {
                var task = ReadAsync(buffer, offset, count);
                return TaskToApm.Begin(task, callback, state);
            }
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (isBeginRw)
            {
                return EndBeginReadFunc.Invoke(asyncResult);
            }
            else
            {
                return TaskToApm.End<int>(asyncResult);
            }
        }



        public  Memory<byte> ReadToBlockEnd()
        {
          
            int count = (int)have_current_length();
            position = wrlen;
            return new Memory<byte>(data, offset, count);

        }

        public ArraySegment<byte> ReadToBlockArrayEnd()
        {
            int count = (int)have_current_length();
            position = wrlen;
            return new ArraySegment<byte>(data, offset, count);
        }


        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}

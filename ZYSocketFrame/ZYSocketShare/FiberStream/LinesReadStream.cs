﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources.Copy;
using System.Runtime.InteropServices;

namespace ZYSocket.FiberStream
{
    public class LinesReadStream : Stream, IFiberReadStream
    {      
        private readonly Pipes Pipes;

        private readonly byte[] data;

        private int offset;

        private readonly int len;

        private long wrlen;

        private long position;    

        private ManualResetValueTaskSource<bool> InitAwaiter;    
        
        private readonly byte[] numericbytes = new byte[8];

        public byte[]  Numericbytes { get => numericbytes; }

        public int Size => len;
        public bool NeedRead => position == wrlen;


        public LinesReadStream(int length = 4096)
        {
           
            Pipes = new Pipes();
            data = new byte[length];
            len = length;
            InitAwaiter = new ManualResetValueTaskSource<bool>();
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


        public ValueTask<int> Check()
        {
            return Pipes.Need();
        }

        public void Reset()
        {           
            InitAwaiter.Reset();
            Pipes.Close();
            position = 0;
            wrlen = 0;
            offset = 0;  
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => wrlen;

        public override long Position { get => position; set => position = value; }



        public override void Flush()
        {

        }



        public ValueTask<bool> WaitStreamInit()
        {
            return new ValueTask<bool>(InitAwaiter, InitAwaiter.Version);
        }

        public void StreamInit()
        {
            InitAwaiter.SetResult(true);
        }


        public void UnStreamInit()
        {
            InitAwaiter.SetResult(false);
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

#if !NETSTANDARD2_0
        public override int Read(Span<byte> buffer)
        {

            int _postion = (int)position;
            int n = Math.Min((int)wrlen - _postion, buffer.Length);
            if (n <= 0)
                return 0;

            new Span<byte>(data, _postion, n).CopyTo(buffer);
            position += n;
            return n;
        }
#endif

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return 0;

            if (NeedRead)
                await Check();

            return Read(buffer, offset, count);
        }


        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {

            var task = ReadAsync(buffer, offset, count, CancellationToken.None);
            return TaskToApm.Begin(task, callback, state);

        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return TaskToApm.End<int>(asyncResult);
        }

#if !NETSTANDARD2_0
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return 0;

            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> destinationArray))
                return await ReadAsync(destinationArray.Array!, destinationArray.Offset, destinationArray.Count);
            else
            {

                if (NeedRead)
                    await Check();

                return Read(buffer.Span);
            }
        }
#endif

        public Memory<byte> ReadToBlockEnd()
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

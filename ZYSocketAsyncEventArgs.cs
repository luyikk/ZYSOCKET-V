using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ZYSocket.FiberStream;

namespace ZYSocket.Server
{
    public class ZYSocketAsyncEventArgs : SocketAsyncEventArgs, ICriticalNotifyCompletion
    {
        private static readonly Action s_completedSentinel = () => { };
        private Action _continuation;

        private readonly IFiberStream Stream;
        private ReadAwaiter readAwaiter;

        public ZYSocketAsyncEventArgs(IFiberStream stream)
        {
            readAwaiter = new ReadAwaiter();
            this.Stream = stream;
            base.Completed += SocketAsyncEventArgsWR_Completed;
        }

        private void SocketAsyncEventArgsWR_Completed(object sender, SocketAsyncEventArgs e)
        {
            Action callback = _continuation;

            if (callback != null)
                callback();           
            else
                Interlocked.CompareExchange(ref _continuation, s_completedSentinel, null)?.Invoke();
        }


        public ZYSocketAsyncEventArgs GetAwaiter() => this;

        public bool IsCompleted => _continuation != null;

        public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);


        public void OnCompleted(Action continuation)
        {

            if (ReferenceEquals(_continuation, s_completedSentinel) ||
                ReferenceEquals(Interlocked.CompareExchange(ref _continuation, continuation, null), s_completedSentinel))
            {
                Task.Run(continuation);
            }
        }

        public int GetResult()
        {
            _continuation = null;
            if (SocketError != SocketError.Success)            
                throw new SocketException((int)SocketError);
            return BytesTransferred;
        }


        public async ValueTask<bool> AcceptAsync(Socket socket)
        {
            Reset();

            if (socket.AcceptAsync(this))
            {
                await this;

                return true;
            }

            return false;
        }


        //public async ValueTask<int> ReceiveAsync()
        //{
        //    if(this.AcceptSocket.ReceiveAsync(this))
        //    {
        //       return await this;
        //    }

        //    return BytesTransferred;
        //}




        public async ValueTask<IFiberStream> GetFiblerStream()
        {
            await Stream.WaitStreamInit();
            return Stream;
        }


        public void StreamInit()
        {
            Stream.StreamInit();
        }


        public async ValueTask<Stream> GetStream()
        {
            var stream = await GetFiblerStream();

            return stream as Stream;
        }

        public void SetBuffer(int inthint)
        {           
            var mem = Stream.GetArray(inthint);
            base.SetBuffer(mem.Array, mem.Offset, mem.Count);
        }

        public void Reset()
        {
            Stream.Reset();
            this.AcceptSocket = null;
            base.SetBuffer(null, 0, 0);
        }

        public PipeFilberAwaiter Advance(int bytesTransferred)
        {
            return Stream.Advance(bytesTransferred);
        }

        public PipeFilberAwaiter ReadCanceled()
        {
            return Stream.ReadCanceled();
        }

     
    }

    public static class BufferExtensions
    {
        public static ArraySegment<byte> GetArray(this Memory<byte> memory)
        {
            return ((ReadOnlyMemory<byte>)memory).GetArray();
        }

        public static ArraySegment<byte> GetArray(this ReadOnlyMemory<byte> memory)
        {
            if (!MemoryMarshal.TryGetArray(memory, out var result))
            {
                throw new InvalidOperationException("Buffer backed by array was expected");
            }
            return result;
        }
    }
}

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ZYSocket.FiberStream
{
    public interface IFiberReadStream
    {
        int Size { get; }
        bool HaveData(); 
        byte[] Numericbytes { get; }
        void Advance(int len);
        ArraySegment<byte> GetArray(int inithnit);
        Memory<byte> GetMemory(int inithnit);
        int Read(byte[] buffer, int offset, int count);
        Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);
        IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state);
        int EndRead(IAsyncResult asyncResult);      
        ArraySegment<byte> ReadToBlockArrayEnd();
        Memory<byte> ReadToBlockEnd();
        void Reset();
        long Seek(long offset, SeekOrigin origin);
        void SetLength(long value);
        void StreamInit();
        ValueTask<bool> WaitStreamInit();
        PipeFilberAwaiter Check();
        bool NeedRead { get; }


    }
}
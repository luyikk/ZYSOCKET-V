using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ZYSocket.FiberStream
{
    public interface IFiberReadStream
    {
        bool IsCanceled { get; }
        bool IsSync { get; set; }
        byte[] Numericbytes { get; }
        PipeFilberAwaiter Advance(int len, CancellationToken cancellationTokenSource = default);
        ArraySegment<byte> GetArray(int inithnit);
        Memory<byte> GetMemory(int inithnit);
        int Read(byte[] buffer, int offset, int count);
        Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);
        IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state);
        int EndRead(IAsyncResult asyncResult);
        PipeFilberAwaiter ReadCanceled();
        ArraySegment<byte> ReadToBlockArrayEnd();
        Memory<byte> ReadToBlockEnd();
        void Reset();
        long Seek(long offset, SeekOrigin origin);
        void SetLength(long value);
        void StreamInit();
        StreamInitAwaiter WaitStreamInit();
        Task Check();
    }
}
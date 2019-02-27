using System;
using System.Threading;
using System.Threading.Tasks;

namespace ZYSocket.FiberStream
{
    public interface IFiberWriteStream
    {

        byte[] Numericbytes { get; }

        long Length { get; }
        long Position { get; set; }
        void Flush();
        Task<int> AwaitFlush();      
        void SetLength(long value);
        void Write(byte[] buffer, int offset, int count);
        Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);
        IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state);
        void EndWrite(IAsyncResult asyncResult);
        void Close();

    }
}
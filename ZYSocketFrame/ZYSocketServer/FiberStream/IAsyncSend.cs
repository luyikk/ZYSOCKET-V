using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZYSocket.Server
{
    public interface IAsyncSend
    {
        ValueTask<int> SendAsync(ArraySegment<byte> data);
        ValueTask<int> SendAsync(byte[] data);
        ValueTask<int> SendAsync(IList<ArraySegment<byte>> data);
        ValueTask<int> SendAsync(ReadOnlyMemory<byte> data);
    }
}
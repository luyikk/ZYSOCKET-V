using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ZYSocket.Share
{
    public interface IAsyncSend
    {
        void SetAccpet(SocketAsyncEventArgs accpet);
        void SetConnect(SocketAsyncEventArgs accpet);
        ValueTask<int> SendAsync(ArraySegment<byte> data);
        ValueTask<int> SendAsync(byte[] data);
        ValueTask<int> SendAsync(IList<ArraySegment<byte>> data);
        ValueTask<int> SendAsync(ReadOnlyMemory<byte> data);
    }
}
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ZYSocket.Share
{
    public interface IAsyncSend
    {
        bool TheSocketExceptionThrow(SocketException er);
        void SetAccpet(SocketAsyncEventArgs accpet);
        void SetConnect(SocketAsyncEventArgs accpet);
        Task<int> SendAsync(ArraySegment<byte> data);
        Task<int> SendAsync(byte[] data);
        Task<int> SendAsync(IList<ArraySegment<byte>> data);
        ValueTask<int> SendAsync(ReadOnlyMemory<byte> data);

    }
}
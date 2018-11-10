using System;
using System.Collections.Generic;

namespace ZYSocket.Server
{
    public interface ISend
    {
        void Send(ArraySegment<byte> data);
        void Send(byte[] data);
        void Send(IList<ArraySegment<byte>> data);
        void Send(ReadOnlyMemory<byte> data);
    }
}
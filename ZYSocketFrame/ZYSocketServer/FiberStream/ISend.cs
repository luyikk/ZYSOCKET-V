﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace ZYSocket.Server
{
    public interface ISend
    {
        void SetAccpet(SocketAsyncEventArgs accpet);
        void Send(ArraySegment<byte> data);
        void Send(byte[] data);
        void Send(IList<ArraySegment<byte>> data);
        void Send(ReadOnlyMemory<byte> data);
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ZYSocket.FiberStream;

namespace ZYSocket.Interface
{
    public interface IObjFormat
    {
        byte[] Serialize(object obj);

        T Read<T>(byte[] data, int offset, int count);
    }
}

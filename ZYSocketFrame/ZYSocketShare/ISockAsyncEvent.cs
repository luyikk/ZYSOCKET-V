using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ZYSocket.FiberStream;

namespace ZYSocket
{

  

    public interface ISockAsyncEvent
    {
        Encoding Encoding { get; }
        bool IsLittleEndian { get; }
        object UserToken { get; set; }
        Socket AcceptSocket { get; }
        ValueTask<IFiberRw> GetFiberRw(System.Func<Stream, Stream, (Stream, Stream)> init = null);
        ValueTask<IFiberRw<T>> GetFiberRw<T>(System.Func<Stream, Stream, (Stream, Stream)> init = null) where T : class;
    }
}

using System;
using System.Buffers;
using System.Text;
using ZYSocket.Share;
using ZYSocket.Interface;

namespace ZYSocket.Server.Builder
{
    public interface ISockServBuilder:IDisposable
    {
        IServiceProvider? ContainerBuilder { get;  }

        ISocketServer Bulid();
        ISockServBuilder ConfigEncode(Func<Encoding>? func = null);
        ISockServBuilder ConfigIAsyncSend(Func<IAsyncSend>? func = null);
        ISockServBuilder ConfigISend(Func<ISend>? func = null);
        ISockServBuilder ConfigMemoryPool(Func<MemoryPool<byte>>? func = null);
        ISockServBuilder ConfigServer(Action<SocketServerOptions>? config = null);
        ISockServBuilder ConfigObjFormat(Func<ISerialization>? func = null);    
        ISockServBuilder ConfigureDefaults();
    }
}
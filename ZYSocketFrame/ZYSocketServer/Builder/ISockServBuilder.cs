using System;
using System.Buffers;
using System.Text;

namespace ZYSocket.Server.Builder
{
    public interface ISockServBuilder
    {
        ISocketServer Bulid();
        ISockServBuilder ConfigEncode(Func<Encoding> func = null);
        ISockServBuilder ConfigIAsyncSend(Func<IAsyncSend> func = null);
        ISockServBuilder ConfigISend(Func<ISend> func = null);
        ISockServBuilder ConfigMemoryPool(Func<MemoryPool<byte>> func = null);
        ISockServBuilder ConfigServer(Action<SocketServerOptions> config = null);
        ISockServBuilder ConfigureDefaults();
    }
}
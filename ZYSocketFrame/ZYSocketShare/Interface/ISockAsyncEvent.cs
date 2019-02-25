using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using ZYSocket.FiberStream;

namespace ZYSocket
{



    public interface ISockAsyncEvent
    {
        Socket ConnectSocket { get; }
        Socket AcceptSocket { get; }
        Encoding Encoding { get; }
        bool IsLittleEndian { get; }
        object UserToken { get; set; }
        void Disconnect();

        ValueTask<IFiberRw> GetFiberRw(System.Func<Stream, Stream, GetFiberRwResult> init = null);
        ValueTask<IFiberRw<T>> GetFiberRw<T>(System.Func<Stream, Stream, GetFiberRwResult> init = null) where T : class;     
       
    }

    public interface ISockAsyncEventAsClient : ISockAsyncEvent
    {      

        ValueTask<GetFiberRwSSLResult> GetFiberRwSSL(X509Certificate certificate, string targethost, Func<Stream, Stream, GetFiberRwResult> init = null);

        ValueTask<GetFiberRwSSLResult<T>> GetFiberRwSSL<T>(X509Certificate certificate_client, string targethost, Func<Stream, Stream, GetFiberRwResult> init = null) where T : class;
    }

    public interface ISockAsyncEventAsServer : ISockAsyncEvent
    {
       
        ValueTask<(IFiberRw,string)> GetFiberRwSSL(X509Certificate certificate, Func<Stream, Stream, GetFiberRwResult> init = null);

        ValueTask<(IFiberRw<T>,string)> GetFiberRwSSL<T>(X509Certificate certificate, Func<Stream, Stream, GetFiberRwResult> init = null) where T : class;
    }
}

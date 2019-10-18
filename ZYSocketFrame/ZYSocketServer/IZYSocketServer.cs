using System.Net.Sockets;

namespace ZYSocket.Server
{
    public interface ISocketServer
    {
        BinaryInputHandler? BinaryInput { get; set; }
        ConnectionFilter? Connetions { get; set; }
        DisconnectHandler? MessageInput { get; set; }
        int GetMaxBufferSize { get; }
        int GetMaxUserConnect { get; }      
        bool NoDelay { get; set; }
        int ReceiveTimeout { get; set; }
        int SendTimeout { get; set; }
        Socket? Sock { get; }
        void Disconnect(Socket socks);
        void Dispose();
        void Start();
        void Stop();
    }
}
using System.Net.Sockets;

namespace ZYSocket.Client
{
    public interface ISocketClient
    {
        ZYSocketAsyncEventArgs AsynEvent { get; }
        string ErrorMsg { get; set; }
        bool IsConnect { get; }
        Socket Sock { get; }

        event BinaryInputHandler BinaryInput;
        event DisconnectHandler Disconnect;

        System.Threading.Tasks.Task<(bool IsSuccess, string Msg)> ConnectAsync(string host, int port, int connectTimeout = 6000);
        void ShutdownBoth();
        void Dispose();
    }
}
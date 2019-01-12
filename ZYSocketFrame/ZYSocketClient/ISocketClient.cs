using System.Net.Sockets;
using System.Threading.Tasks;
using ZYSocket.FiberStream;

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

        void SetConnect();

        Task<(bool IsSuccess, string Msg)> ConnectAsync(string host, int port, int connectTimeout = 6000);
        Task<IFiberRw> GetFiberRw();
        void ShutdownBoth(bool events=false);
        void Dispose();
    }
}
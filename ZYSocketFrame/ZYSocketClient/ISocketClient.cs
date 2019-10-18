using System.Net.Sockets;
using System.Threading.Tasks;
using ZYSocket.FiberStream;

namespace ZYSocket.Client
{
    public interface ISocketClient
    {
        ZYSocketAsyncEventArgs? CurrentSocketAsyncEventArgs { get; }
        string ErrorMsg { get; set; }
        bool IsConnect { get; }
        Socket? Sock { get; }

        event BinaryInputHandler? BinaryInput;
        event DisconnectHandler? Disconnect;

        void SetConnected(bool isSuccess = true, string? err = null);
        Task<ConnectResult> ConnectAsync(string host, int port, int connectTimeout = 6000);
        ConnectResult Connect(string host, int port, int connectTimeout = 6000);
        Task<IFiberRw?> GetFiberRw();
        void ShutdownBoth(bool events=false, string? errorMsg = null);
        void Dispose();
    }
}
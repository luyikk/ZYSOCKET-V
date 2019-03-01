using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZYSocket.FiberStream;
using ZYSocket.Share;

namespace ZYSocket.Client
{

    /// <summary>
    /// 数据包输入代理
    /// </summary>
    /// <param name="data">输入包</param>
    /// <param name="socketAsync"></param>
    public delegate void BinaryInputHandler(ISocketClient client,ISockAsyncEventAsClient socketAsync);


    /// <summary>
    /// 异常错误通常是用户断开的代理
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="socketAsync"></param>
    /// <param name="erorr">错误代码</param>
    public delegate void DisconnectHandler(ISocketClient client, ISockAsyncEventAsClient socketAsync, string msg);


    public class SocketClient : IDisposable, ISocketClient
    {

        private readonly Encoding encoding;

        private readonly MemoryPool<byte> memoryPool;

        private readonly ISend syncsend;

        private readonly IAsyncSend asyncsend;

        private readonly int bufferSize;

   

        public Socket Sock { get; private set; }     

        public bool IsConnect { get; private set; }

        public ZYSocketAsyncEventArgs AsynEvent { get; private set; }
      

        private System.Threading.AutoResetEvent wait = new System.Threading.AutoResetEvent(false);

        private TaskCompletionSource<IFiberRw> completionSource;

        private string errorMsg;
        public string ErrorMsg { get => errorMsg; set => errorMsg = value; }

        public event BinaryInputHandler BinaryInput;

        public event DisconnectHandler Disconnect;

        public SocketClient(int buffer_size=4096,MemoryPool<byte> memPool =null,ISend sync_send=null,IAsyncSend async_send=null, Encoding encode =null)
        {
            if (encode is null)
                this.encoding = Encoding.UTF8;
            else
                this.encoding = encode;

            if (memPool is null)
                memoryPool = new Thruster.FastMemoryPool<byte>();
            else
                memoryPool = memPool;
           
            if (sync_send is null)
                sync_send = new PoolSend();

            if (async_send is null)
                async_send = new PoolSend();


            syncsend = sync_send;
            asyncsend = async_send;
            bufferSize = buffer_size;
        }



        public async Task<ConnectResult> ConnectAsync(string host, int port,int connectTimeout=6000)
        {

            return await Task.Run<ConnectResult>(() =>
                {
                    return Connect(host, port, connectTimeout);
                });
        }

        public ConnectResult Connect(string host, int port, int connectTimeout = 6000)
        {
            if (IsConnect)
                throw new System.IO.IOException("the socket status is connect already,please Dispose it.");

            completionSource = new TaskCompletionSource<IFiberRw>(TaskCreationOptions.RunContinuationsAsynchronously);
          
            errorMsg = null;
            IPEndPoint myEnd = null;

            #region ipformat
            try
            {
                myEnd = new IPEndPoint(IPAddress.Parse(host), port);
            }
            catch (FormatException)
            {
                IPHostEntry p = Dns.GetHostEntry(host);

                foreach (IPAddress s in p.AddressList)
                {
                    myEnd = new IPEndPoint(s, port);
                    break;
                }
            }

            #endregion

            Sock = new Socket(myEnd.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            ZYSocketAsyncEventArgs e = new ZYSocketAsyncEventArgs(
                    completionSource,
                    new LinesReadStream(),
                    new BufferWriteStream(memoryPool, syncsend, asyncsend),
                    syncsend,
                    asyncsend,
                    memoryPool,
                    encoding)
            {
                RemoteEndPoint = myEnd
            };
            e.DisconnectIt = Diconnect_It;
            e.Receive = StartReceive;
            e.Completed += E_Completed;
            AsynEvent = e;

            if (!Sock.ConnectAsync(AsynEvent))
            {
                Connect(AsynEvent);
            }

            if (wait.WaitOne(connectTimeout))
            {
                wait.Reset();

                if (!IsConnect)
                {
                    this.Dispose();
                }

                return  new ConnectResult(IsConnect, errorMsg);
            }
            else
            {
                wait.Reset();
                this.Dispose();
                return new ConnectResult(false, "connect time out");
            }
        }

        public async Task<IFiberRw> GetFiberRw()
        {
            return await completionSource.Task; 
        }

        private void E_Completed(object sender, ZYSocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    Connect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    BeginReceive(e);
                    break;
            }
        }


        private void Connect(ZYSocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {              
                BinaryInput?.Invoke(this,e);
                syncsend.SetConnect(e);
                asyncsend.SetConnect(e);
                e.SetBuffer(bufferSize);
                e.StreamInit();
            }
            else
            {
                IsConnect = false;               
                errorMsg = new SocketException((int)e.SocketError).Message;
                wait?.Set();
            }
        }

        private void StartReceive()
        {
            if (!AsynEvent.IsStartReceive)
            {
                try
                {
                    if (!Sock.ReceiveAsync(AsynEvent))
                        BeginReceive(AsynEvent);

                    AsynEvent.IsStartReceive = true;
                    IsConnect = true;
                    errorMsg = "connect success";
                }
                catch (ObjectDisposedException)
                {
                    Diconnect_It(AsynEvent);
                }
            }
        }

        public void SetConnect(bool isSuccess=true,string err=null)
        {
            if (isSuccess)
            {
                StartReceive();               
            }
            else
            {
                Diconnect_It(AsynEvent);

                IsConnect = false;
                if (string.IsNullOrEmpty(err))
                    errorMsg = "set connect faill";
                else
                    ErrorMsg = err;
            }

            wait?.Set();
        }



        void BeginReceive(ZYSocketAsyncEventArgs e)
        {


            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {

                e.Advance();

                try
                {
                    
                    if (!Sock.ReceiveAsync(e))
                    {
                        if (e.Add_check() > 512)
                        {
                            e.Reset_check();
                            ThreadPool.QueueUserWorkItem(obj =>
                            {
                                BeginReceive(obj as ZYSocketAsyncEventArgs);
                            }, e);
                        }
                        else
                            BeginReceive(e);
                    }

                    e.Reset_check();
                }
                catch (ObjectDisposedException)
                {
                    Diconnect_It(e);
                }

            }
            else
            {
                Diconnect_It(e);
            }

        }

        private void Diconnect_It(ZYSocketAsyncEventArgs e)
        {

            errorMsg = "Disconnect";
            Disconnect?.Invoke(this, e, errorMsg);

            if(IsConnect)
                this.Dispose();

        }



        public void ShutdownBoth(bool events=false)
        {
            if (IsConnect)
            {
                Sock?.Shutdown(SocketShutdown.Both);
                this.Dispose();
            }

            if (events)
            {
                errorMsg = "Disconnect";
                Disconnect?.Invoke(this, AsynEvent, errorMsg);
            }
        }



        public void Dispose()
        {
            IsConnect = false;
            try
            {
                Sock?.Close();
                Sock?.Dispose();
            }
            catch { }
        }
    }
}

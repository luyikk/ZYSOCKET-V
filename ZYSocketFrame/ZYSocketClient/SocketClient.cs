using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZYSocket.FiberStream;
using ZYSocket.Interface;
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

        private readonly ISerialization objFormat;

        private readonly int bufferSize;   

        public Socket? Sock { get; private set; }     

        public bool IsConnect { get; private set; }

        public ZYSocketAsyncEventArgs? CurrentSocketAsyncEventArgs { get; private set; }
      

        private readonly System.Threading.AutoResetEvent wait = new System.Threading.AutoResetEvent(false);

        private readonly System.Threading.SemaphoreSlim semaphore = new System.Threading.SemaphoreSlim(1,1);

        private TaskCompletionSource<IFiberRw>? completionSource;

        private string? errorMsg;
        public string ErrorMsg { get => errorMsg??""; set => errorMsg = value; }

        public event BinaryInputHandler? BinaryInput;

        public event DisconnectHandler? Disconnect;

        public SocketClient(int buffer_size=4096,int maxPackerSize = 128*1024, MemoryPool<byte>? memPool =null,ISend? sync_send=null,IAsyncSend? async_send=null, ISerialization? obj_Format = null, Encoding? encode =null)
        {
            if (encode is null)
                this.encoding = Encoding.UTF8;
            else
                this.encoding = encode;

            if (memPool is null)
                memoryPool = new Thruster.FastMemoryPool<byte>(maxPackerSize);
            else
                memoryPool = memPool;
           
            if (sync_send is null)
                sync_send = new NetSend();


            if (async_send is null)
                async_send = new NetSend();

            if (obj_Format is null)
                obj_Format = new ProtobuffObjFormat();

            ReadBytes.MaxPackerSize = maxPackerSize;
            syncsend = sync_send;
            asyncsend = async_send;
            bufferSize = buffer_size;
            objFormat = obj_Format;
        }



        public  Task<ConnectResult> ConnectAsync(string host, int port,int connectTimeout=6000)
        {
            return Task.Run<ConnectResult>(() =>
            {
                return Connect(host, port, connectTimeout);
            });
        }

        public ConnectResult Connect(string host, int port, int connectTimeout = 6000)
        {
           

            if (semaphore.Wait(60000))
            {
              
                try
                {
                    if (IsConnect)
                        return new ConnectResult(false, "the socket status is connect already,please Dispose it.");


                    completionSource = new TaskCompletionSource<IFiberRw>(TaskCreationOptions.RunContinuationsAsynchronously);

                    errorMsg = null;
                    IPEndPoint? myEnd;

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

                        myEnd = null;
                    }

                    if (myEnd is null)
                        throw new NullReferenceException("IPEndPoint is null");

                    #endregion

                    Sock = new Socket(myEnd.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    ZYSocketAsyncEventArgs e = new ZYSocketAsyncEventArgs(
                            completionSource,
                            new LinesReadStream(),
                            new BufferWriteStream(memoryPool, syncsend, asyncsend),
                            syncsend,
                            asyncsend,
                            memoryPool,
                            encoding,
                            objFormat,
                            false)
                    {
                        RemoteEndPoint = myEnd
                    };
                    e.DisconnectIt = Diconnect_It;
                    e.Completed += E_Completed;
                    CurrentSocketAsyncEventArgs = e;

                    if (!Sock.ConnectAsync(CurrentSocketAsyncEventArgs))
                    {
                        Connect(CurrentSocketAsyncEventArgs);
                    }

                    if (wait.WaitOne(connectTimeout))
                    {
                        wait.Reset();

                        if (!IsConnect)
                        {
                            this.Dispose();
                        }

                        return new ConnectResult(IsConnect, errorMsg);
                    }
                    else
                    {
                        wait.Reset();
                        this.Dispose();
                        return new ConnectResult(false, "connect time out");
                    }

                }
                catch (Exception er)
                {
                    return new ConnectResult(false, er.ToString());
                }
                finally
                {
                    semaphore.Release();
                }

            }
            else
                return new ConnectResult(false, "connect time out");
        }

        public async Task<IFiberRw?> GetFiberRw()
        {
            if (completionSource is null)
                return null;

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
                StartReceive();
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
            if (!CurrentSocketAsyncEventArgs!.IsStartReceive)
            {
                CurrentSocketAsyncEventArgs.IsStartReceive = true;

                try
                {
                    if (!Sock!.ReceiveAsync(CurrentSocketAsyncEventArgs))
                        BeginReceive(CurrentSocketAsyncEventArgs);
                }
                catch (ObjectDisposedException)
                {

                }
            }
        }

        public void SetConnected(bool isSuccess=true,string? err=null)
        {
            if (isSuccess)
            {
                IsConnect = true;
                errorMsg = "connect success";
            }
            else
            {               
                if (string.IsNullOrEmpty(err))
                    errorMsg = "set connect faill";
                else
                    errorMsg = err;

                Diconnect_It(CurrentSocketAsyncEventArgs!,errorMsg);
                IsConnect = false;
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
                    
                    if (!Sock!.ReceiveAsync(e))
                    {
                        //if (e.Add_check() > 512)
                        //{
                        //    e.Reset_check();
                        //    ThreadPool.QueueUserWorkItem(obj => BeginReceive((obj as ZYSocketAsyncEventArgs)!), e);
                        //}
                        //else
                            BeginReceive(e);
                    }

                   // e.Reset_check();
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

        private void Diconnect_It(ZYSocketAsyncEventArgs e) => Diconnect_It(e,null);


        private void Diconnect_It(ZYSocketAsyncEventArgs e, string? errorMsg=null)
        {                       
            Disconnect?.Invoke(this, e, errorMsg?? "Disconnect");
            if(IsConnect)
                this.Dispose();
            e?.Reset();
        }



        public void ShutdownBoth(bool events=false, string? errorMsg =null)
        {
            if (IsConnect)
            {
                try
                {
                    Sock?.Shutdown(SocketShutdown.Both);
                }
                catch (SocketException)
                {
                }
            }


            if (events)
                Disconnect?.Invoke(this, CurrentSocketAsyncEventArgs!, errorMsg?? "Disconnect");
            
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

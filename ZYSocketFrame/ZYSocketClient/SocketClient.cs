﻿using System;
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



        public async Task<(bool IsSuccess, string Msg)> ConnectAsync(string host, int port,int connectTimeout=6000)
        {

            return await Task.Run<(bool, string)>(() =>
                {
                    return Connect(host, port, connectTimeout);
                });
        }

        public (bool IsSuccess, string Msg) Connect(string host, int port, int connectTimeout = 6000)
        {
            if (IsConnect)
                throw new System.IO.IOException("the socket status is connect already,please Dispose it.");

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
                    new LinesReadStream(),
                    new BufferWriteStream(memoryPool, syncsend, asyncsend),
                    syncsend,
                    asyncsend,
                    memoryPool,
                    encoding)
            {
                RemoteEndPoint = myEnd
            };


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

                return (IsConnect, errorMsg);
            }
            else
            {
                this.Dispose();
                return (false, "connect time out");
            }
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
                IsConnect = true;
                wait?.Set();
                errorMsg = "connect success";
                BinaryInput?.Invoke(this,e);

                syncsend.SetConnect(e);
                asyncsend.SetConnect(e);

                e.SetBuffer(bufferSize);

                try
                {
                    if (!Sock.ReceiveAsync(e))
                    {
                        BeginReceive(e);

                    }
                }
                catch (ObjectDisposedException)
                {
                    Diconnect_It(e);
                }

                e.StreamInit();

            }
            else
            {
                IsConnect = false;
                wait?.Set();
                errorMsg = new SocketException((int)e.SocketError).Message;
            }
        }



        async void BeginReceive(ZYSocketAsyncEventArgs e)
        {


            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {

                await e.Advance();

                try
                {
                    if (!Sock.ReceiveAsync(e))
                    {
                        if (e.Add_check() > 512)
                            ThreadPool.QueueUserWorkItem(obj =>
                            {
                                BeginReceive(obj as ZYSocketAsyncEventArgs);
                            }, e);
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



        public void ShutdownBoth()
        {
            if (IsConnect)
            {
                Sock?.Shutdown(SocketShutdown.Both);
                this.Dispose();
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
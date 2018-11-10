/*
 * 北风之神SOCKET框架(ZYSocket)
 *  Borey Socket Frame(ZYSocket)
 *  by luyikk@126.com
 *  Updated 2014-7-21  此类已支持MONO
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using ZYSocket.FiberStream;
using System.Buffers;

namespace ZYSocket.Server
{

    /// <summary>
    /// 连接的代理
    /// </summary>
    /// <param name="socketAsync"></param>
    public delegate bool ConnectionFilter(ZYSocketAsyncEventArgs socketAsync);

    /// <summary>
    /// 数据包输入代理
    /// </summary>
    /// <param name="data">输入包</param>
    /// <param name="socketAsync"></param>
    public delegate void BinaryInputHandler(ZYSocketAsyncEventArgs socketAsync);


    /// <summary>
    /// 异常错误通常是用户断开的代理
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="socketAsync"></param>
    /// <param name="erorr">错误代码</param>
    public delegate void MessageInputHandler(string message, ZYSocketAsyncEventArgs socketAsync, int erorr);

    /// <summary>
    /// ZYSOCKET框架 服务器端
    ///（通过6W个连接测试。理论上支持10W个连接，可谓.NET最强SOCKET模型）
    /// </summary>
    public class ZYSocketSuper : IDisposable
    {

        #region 释放
        /// <summary>
        /// 用来确定是否以释放
        /// </summary>
        private bool isDisposed;


        ~ZYSocketSuper()
        {
            this.Dispose(false);

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed||disposing)
            {
                try
                {
                    sock.Shutdown(SocketShutdown.Both);
                    sock.Close();
                    sock.Dispose();
                  
                }
                catch
                {
                }

                isDisposed = true;
            }
        }
#endregion

     
    
        /// <summary>
        /// SOCK对象
        /// </summary>
        private Socket sock;

        /// <summary>
        /// Socket对象
        /// </summary>
        public Socket Sock { get { return sock; } }


        /// <summary>
        /// 连接传入处理
        /// </summary>
        public ConnectionFilter Connetions { get; set; }

        /// <summary>
        /// 数据输入处理
        /// </summary>
        public BinaryInputHandler BinaryInput { get; set; }

  
        /// <summary>
        /// 异常错误通常是用户断开处理
        /// </summary>
        public MessageInputHandler MessageInput { get; set; }


        private readonly System.Threading.AutoResetEvent[] reset;

        /// <summary>
        /// 是否关闭SOCKET Delay算法
        /// </summary>
        public bool NoDelay
        {
            get
            {
                return sock.NoDelay;
            }

            set
            {
                sock.NoDelay = value;
            }

        }

        /// <summary>
        /// SOCKET 的  ReceiveTimeout属性
        /// </summary>
        public int ReceiveTimeout
        {
            get
            {
                return sock.ReceiveTimeout;
            }

            set
            {
                sock.ReceiveTimeout = value;

            }


        }

        /// <summary>
        /// SOCKET 的 SendTimeout
        /// </summary>
        public int SendTimeout
        {
            get
            {
                return sock.SendTimeout;
            }

            set
            {
                sock.SendTimeout = value;
            }

        }


        /// <summary>
        /// 接收包大小
        /// </summary>
        private readonly int MaxBufferSize;

        public int GetMaxBufferSize
        {
            get
            {
                return MaxBufferSize;
            }
        }

        /// <summary>
        /// 最大用户连接
        /// </summary>
        private readonly int MaxConnectCout;

        /// <summary>
        /// 最大用户连接数
        /// </summary>
        public int GetMaxUserConnect
        {
            get
            {
                return MaxConnectCout;
            }
        }

   


        /// <summary>
        /// IP
        /// </summary>
        private string Host;

        /// <summary>
        /// 端口
        /// </summary>
        private readonly int Port;




       

        public ZYSocketSuper(string host, int port, int maxconnectcout, int maxbuffersize)
        {
            this.Port = port;
            this.Host = host;
            this.MaxBufferSize = maxbuffersize;
            this.MaxConnectCout = maxconnectcout;

           
            this.reset = new System.Threading.AutoResetEvent[1];
            reset[0] = new System.Threading.AutoResetEvent(false);

            Run();

        }


        /// <summary>
        /// 启动
        /// </summary>
        private void Run()
        {
            if (isDisposed == true)
            {
                throw new ObjectDisposedException("ZYServer is Disposed");
            }


            IPEndPoint myEnd = new IPEndPoint(IPAddress.Any, Port);

            if (!Host.Equals("any",StringComparison.CurrentCultureIgnoreCase))
            {
                if (String.IsNullOrEmpty(Host))
                {

                    IPHostEntry p = Dns.GetHostEntry(Dns.GetHostName());

                    foreach (IPAddress s in p.AddressList)
                    {
                        if (!s.IsIPv6LinkLocal && s.AddressFamily != AddressFamily.InterNetworkV6)
                        {
                            myEnd = new IPEndPoint(s, Port);
                            break;
                        }
                    }
                  
                }
                else
                {
                    try
                    {
                        myEnd = new IPEndPoint(IPAddress.Parse(Host), Port);
                    }
                    catch (FormatException)
                    {

                        IPHostEntry p = Dns.GetHostEntry(Dns.GetHostName());

                        foreach (IPAddress s in p.AddressList)
                        {
                            if (!s.IsIPv6LinkLocal)
                                myEnd = new IPEndPoint(s, Port);
                        }
                    }

                }
            
            
            }

            sock = new Socket(myEnd.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);


            sock.Bind(myEnd);
            sock.Listen(512);           
            ReceiveTimeout = 1000;

            var memoryPool = new Thruster.FastMemoryPool<byte>();

            for (int i = 0; i < MaxConnectCout; i++)
            {

                PoolSend poolSend = new PoolSend();
                // var memoryPool = new SlabMemoryPool();
                //  var memoryPool = new MemoryPool.BufferMemoryPool();
               

                ZYSocketAsyncEventArgs socketasyn = new ZYSocketAsyncEventArgs(
                    new LinesReadStream(MaxBufferSize),
                    new BufferWriteStream(memoryPool,poolSend,poolSend),
                    memoryPool, 
                    Encoding.UTF8, 
                    poolSend, 
                    poolSend);

                poolSend.SetAccpet(socketasyn);
                socketasyn.Completed += new EventHandler<ZYSocketAsyncEventArgs>(Asyn_Completed);
                Accept(socketasyn);
            }


        }

        public void Start()
        {
            reset[0].Set();
           
        }

        public void Stop()
        {
            reset[0].Reset();
        }

        void Accept(ZYSocketAsyncEventArgs sockasyn)
        {

            sockasyn.Reset();

            if (!Sock.AcceptAsync(sockasyn))
            {
                BeginAccep(sockasyn);
            }

        }

        void BeginAccep(ZYSocketAsyncEventArgs e)
        {


            if (e.SocketError == SocketError.Success)
            {

                System.Threading.WaitHandle.WaitAll(reset);
                reset[0].Set();

                if (this.Connetions != null)
                    if (!this.Connetions(e))
                    {
                        e.AcceptSocket = null;
                        Accept(e);
                        return;
                    }


                e.SetBuffer(MaxBufferSize);

                BinaryInput?.Invoke(e);

                if (!e.AcceptSocket.ReceiveAsync(e))
                {
                    BeginReceive(e);
                   
                }

                e.StreamInit();


            }
            else
            {               
                Accept(e);
            }


        }



        async void BeginReceive(ZYSocketAsyncEventArgs e)
        {
          

            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {

                await e.Advance();

                if (!e.AcceptSocket.ReceiveAsync(e))
                {
                    if (e.Add_check() > 512)
                        ThreadPool.QueueUserWorkItem(obj => {                            
                                BeginReceive(obj as ZYSocketAsyncEventArgs);                         
                        }, e);
                    else
                        BeginReceive(e);
                }

                e.Reset_check();

            }
            else
            {              
                if (MessageInput != null && e.AcceptSocket != null && e.AcceptSocket.RemoteEndPoint != null)
                {

                    string message;

                    try
                    {
                        message = string.Format("User Disconnect :{0}", e.AcceptSocket.RemoteEndPoint.ToString());
                    }
                    catch (System.NullReferenceException)
                    {
                        message = "User Disconect";
                    }


                    MessageInput.Invoke(message, e, 0);

                }
                else
                {
                    MessageInput?.Invoke("User disconnect but cannot get Ipaddress", e, 0);
                }

                e.AcceptSocket = null;
                Accept(e);
            }

        }





        void Asyn_Completed(object sender, ZYSocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    BeginAccep(e);
                    break;
                case SocketAsyncOperation.Receive:
                    BeginReceive(e);
                    break;

            }
        }


        /// <summary>
        /// 断开此SOCKET
        /// </summary>
        /// <param name="sock"></param>
        public void Disconnect(Socket socks)
        {
            try
            {
                if (sock != null)
                {
                    socks.Shutdown(SocketShutdown.Both);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception)
            {

            }

        }


    }

    public enum LogType
    {
        Error,
    }


    public class LogOutEventArgs : EventArgs
    {

        /// <summary>
        /// 消息类型
        /// </summary>     
        private readonly LogType messClass;

        /// <summary>
        /// 消息类型
        /// </summary>  
        public LogType MessClass
        {
            get { return messClass; }
        }



        /// <summary>
        /// 消息
        /// </summary>
        private readonly string mess;

        public string Mess
        {
            get { return mess; }
        }

        public LogOutEventArgs(LogType messclass, string str)
        {
            messClass = messclass;
            mess = str;

        }


    }
}

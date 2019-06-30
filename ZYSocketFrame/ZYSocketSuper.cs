/*
 *  ZY Socket Frame
 *  by luyikk@126.com
 *  Start 2007-12-3
 *  Updated 2019-3-3 
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
using ZYSocket.Server.Builder;
using ZYSocket.Share;
using ZYSocket.Interface;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace ZYSocket.Server
{

    /// <summary>
    /// 连接的代理
    /// </summary>
    /// <param name="socketAsync"></param>
    public delegate bool ConnectionFilter(ISockAsyncEventAsServer socketAsync);

    /// <summary>
    /// 数据包输入代理
    /// </summary>
    /// <param name="data">输入包</param>
    /// <param name="socketAsync"></param>
    public delegate void BinaryInputHandler(ISockAsyncEventAsServer socketAsync);


    /// <summary>
    /// 异常错误通常是用户断开的代理
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="socketAsync"></param>
    /// <param name="erorr">错误代码</param>
    public delegate void DisconnectHandler(string message, ISockAsyncEventAsServer socketAsync, int erorr);

    /// <summary>
    /// ZYSOCKET框架 服务器端
    ///（通过6W个连接测试。理论上支持10W个连接，可谓.NET最强SOCKET模型）
    /// </summary>
    public class ZYSocketSuper : IDisposable, ISocketServer
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
                   // sock.Shutdown(SocketShutdown.Both);
                    sock.Close();
                    sock.Dispose();
                    reset[0].Dispose();

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
        public DisconnectHandler MessageInput { get; set; }


        private readonly System.Threading.ManualResetEvent[] reset;

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
        /// SEAE队列
        /// </summary>
        public readonly ConcurrentQueue<ZYSocketAsyncEventArgs> SAEAQueue = new ConcurrentQueue<ZYSocketAsyncEventArgs>();

   


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
           
            this.reset = new System.Threading.ManualResetEvent[1];
            reset[0] = new System.Threading.ManualResetEvent(false);          
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

            if (Host.Equals("ipv6any", StringComparison.CurrentCultureIgnoreCase))
            {
                myEnd = new IPEndPoint(IPAddress.IPv6Any, Port);
            }
            else if (!Host.Equals("any",StringComparison.CurrentCultureIgnoreCase))
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

                PoolSend poolSend = new PoolSend(true);                  

                ZYSocketAsyncEventArgs socketasyn = new ZYSocketAsyncEventArgs(
                    new LinesReadStream(MaxBufferSize),
                    new BufferWriteStream(memoryPool, poolSend, poolSend),
                    poolSend,
                    poolSend,
                    memoryPool,                    
                    Encoding.UTF8
                   );
                               
             
                socketasyn.DisconnectIt = Disconnect_It;
                poolSend.SetAccpet(socketasyn);
                socketasyn.Completed += new EventHandler<ZYSocketAsyncEventArgs>(Asyn_Completed);
                SAEAQueue.Enqueue(socketasyn);
            }

            Task.Factory.StartNew(StartAccept);
        }


        
        public ZYSocketSuper(IServiceProvider component)
        {
            var config = component.GetRequiredService<SocketServerOptions>();


            this.Port = config.Port;
            this.Host = config.Host;
            this.MaxBufferSize = config.MaxBufferSize;
            this.MaxConnectCout = config.MaxConnectCout;

            this.reset = new System.Threading.ManualResetEvent[1];
            reset[0] = new System.Threading.ManualResetEvent(false);

            Run(component);
        }


        /// <summary>
        /// 启动
        /// </summary>
        private void Run(IServiceProvider component)
        {
            if (isDisposed == true)
            {
                throw new ObjectDisposedException("ZYServer is Disposed");
            }

            var config = component.GetService<SocketServerOptions>();


            IPEndPoint myEnd = new IPEndPoint(IPAddress.Any, Port);

            if (Host.Equals("ipv6any", StringComparison.CurrentCultureIgnoreCase))
            {
                myEnd = new IPEndPoint(IPAddress.IPv6Any, Port);
            }
            else if (!Host.Equals("any", StringComparison.CurrentCultureIgnoreCase))
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
            sock.Listen(config.BackLog);

            if(config.ReceiveTimeout>0)
                ReceiveTimeout = config.ReceiveTimeout;
            if (config.SendTimeout > 0)
                SendTimeout = config.SendTimeout;




            var memoryPool = component.GetRequiredService<MemoryPool<byte>>();
            var encode = component.GetRequiredService<Encoding>();
            var objFormat = component.GetRequiredService<ISerialization>();

            for (int i = 0; i < MaxConnectCout; i++)
            {
              
                var poolSend = component.GetRequiredService<ISend>();
                var poolAsyncSend = component.GetRequiredService<IAsyncSend>();

                ZYSocketAsyncEventArgs socketasyn = new ZYSocketAsyncEventArgs(
                    new LinesReadStream(MaxBufferSize),
                    new BufferWriteStream(memoryPool, poolSend, poolAsyncSend),
                    poolSend,
                    poolAsyncSend,
                    memoryPool,
                    encode,
                    objFormat,
                    config.IsLittleEndian
                   );
                socketasyn.SetBuffer(MaxBufferSize);
                socketasyn.DisconnectIt = Disconnect_It;
                poolSend.SetAccpet(socketasyn);
                poolAsyncSend.SetAccpet(socketasyn);
                socketasyn.Completed += new EventHandler<ZYSocketAsyncEventArgs>(Asyn_Completed);
                SAEAQueue.Enqueue(socketasyn);
            }

            Task.Factory.StartNew(StartAccept);

        }




        public void Start()
        {
            if (BinaryInput is null)
                throw new IOException("BinaryInput is null");

            reset[0].Set();
           
        }

        public void Stop()
        {
            reset[0].Reset();
        }



        void StartAccept()
        {
            for(; ; )
            {
                var target=  Sock.Accept();

                if(SAEAQueue.TryDequeue(out ZYSocketAsyncEventArgs saea))
                {
                    BeginReceive(target, saea);
                }
                else
                {
                    Disconnect(target);
                }
            }
        }


        void PushSaea(ZYSocketAsyncEventArgs sockasyn)
        {
            sockasyn.Reset();
            SAEAQueue.Enqueue(sockasyn);
        }


        void BeginReceive(Socket socket, ZYSocketAsyncEventArgs e)
        {
            System.Threading.WaitHandle.WaitAll(reset);
            reset[0].Set();

            e.AcceptSocket = socket;
            if (this.Connetions != null)
                if (!this.Connetions(e))
                {
                    try
                    {
                        e.AcceptSocket?.Shutdown(SocketShutdown.Both);
                    }
                    catch { }

                    e.AcceptSocket = null;
                    PushSaea(e);
                    return;
                }
          
            BinaryInput(e);
            e.StreamInit();
            StartReceive(e);

        }

        private void StartReceive(ZYSocketAsyncEventArgs e)
        {          
            if (!e.AcceptSocket.ReceiveAsync(e))
                BeginReceive(e);
        }



        void BeginReceive(ZYSocketAsyncEventArgs e)
        {


            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
               
                e.Advance();

                if (!e.AcceptSocket.ReceiveAsync(e))
                {
                    if (e.Add_check() > 512)
                    {
                        e.Reset_check();
                        ThreadPool.QueueUserWorkItem(obj => BeginReceive(obj as ZYSocketAsyncEventArgs),e);
                    }
                    else
                        BeginReceive(e);

                }

                e.Reset_check();
            }
            else
            {             
                Disconnect_It(e);
            }

        }


        void Disconnect_It(ZYSocketAsyncEventArgs e)
        {

            if (MessageInput != null && e.AcceptSocket != null)
            {

                string message;

                try
                {
                    message = string.Format("User Disconnect :{0}", e.AcceptSocket.RemoteEndPoint.ToString());
                }
                catch (System.ObjectDisposedException)
                {
                    message = "User Disconect";
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
            
            if(e.IsInit)
                PushSaea(e);
        }




        void Asyn_Completed(object sender, ZYSocketAsyncEventArgs e)=> BeginReceive(e);



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
   
}

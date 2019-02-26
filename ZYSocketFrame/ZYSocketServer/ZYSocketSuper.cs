﻿/*
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
using Autofac;
using ZYSocket.Server.Builder;
using ZYSocket.Share;

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

                PoolSend poolSend = new PoolSend();
                 //var memoryPool = new SlabMemoryPool();
                 // var memoryPool = new MemoryPool.BufferMemoryPool();
             

                ZYSocketAsyncEventArgs socketasyn = new ZYSocketAsyncEventArgs(
                    new LinesReadStream(MaxBufferSize),
                    new BufferWriteStream(memoryPool, poolSend, poolSend),
                    poolSend,
                    poolSend,
                    memoryPool,
                    Encoding.UTF8
                   );

               
                socketasyn.StartReceiveAction = StartReceive;
                socketasyn.DisconnectIt = Disconnect_It;
                poolSend.SetAccpet(socketasyn);
                socketasyn.Completed += new EventHandler<ZYSocketAsyncEventArgs>(Asyn_Completed);
                Accept(socketasyn);
            }


        }


        
        public ZYSocketSuper(IComponentContext component)
        {
            var config = component.Resolve<SocketServerOptions>();


            this.Port = config.Port;
            this.Host = config.Host;
            this.MaxBufferSize = config.MaxBufferSize;
            this.MaxConnectCout = config.MaxConnectCout;

            this.reset = new System.Threading.AutoResetEvent[1];
            reset[0] = new System.Threading.AutoResetEvent(false);

            Run(component);
        }


        /// <summary>
        /// 启动
        /// </summary>
        private void Run(IComponentContext component)
        {
            if (isDisposed == true)
            {
                throw new ObjectDisposedException("ZYServer is Disposed");
            }

            var config = component.Resolve<SocketServerOptions>();


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




            var memoryPool = component.Resolve<MemoryPool<byte>>();
            var encode = component.Resolve<Encoding>();

            for (int i = 0; i < MaxConnectCout; i++)
            {
              
                var poolSend = component.Resolve<ISend>();
                var poolAsyncSend = component.Resolve<IAsyncSend>();

                ZYSocketAsyncEventArgs socketasyn = new ZYSocketAsyncEventArgs(
                    new LinesReadStream(MaxBufferSize),
                    new BufferWriteStream(memoryPool, poolSend, poolAsyncSend),
                    poolSend,
                    poolAsyncSend,
                    memoryPool,
                    encode,
                    config.IsLittleEndian
                   );

                socketasyn.StartReceiveAction= StartReceive;
                socketasyn.DisconnectIt = Disconnect_It;
                poolSend.SetAccpet(socketasyn);
                poolAsyncSend.SetAccpet(socketasyn);

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

            try
            {
                if (!Sock.AcceptAsync(sockasyn))
                {
                    BeginAccep(sockasyn);
                }
            }
            catch (ObjectDisposedException) { }
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
                        try
                        {
                            e.AcceptSocket?.Shutdown(SocketShutdown.Both);
                        }
                        catch { }

                        e.AcceptSocket = null;
                        Accept(e);
                        return;
                    }

                e.SetBuffer(MaxBufferSize);
                BinaryInput?.Invoke(e);
                e.StreamInit();

            }
            else
            {               
                Accept(e);
            }


        }

        private void StartReceive(ZYSocketAsyncEventArgs e)
        {          
            if (!e.AcceptSocket.ReceiveAsync(e))
                BeginReceive(e);
        }



        async void BeginReceive(ZYSocketAsyncEventArgs e)
        {


            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
               
                await e.Advance();
                
                if (!e.AcceptSocket.ReceiveAsync(e))                                
                        BeginReceive(e);               

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
                Accept(e);
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
   
}

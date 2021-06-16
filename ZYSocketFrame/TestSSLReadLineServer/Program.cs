﻿using System;
using ZYSocket.Server;
using System.Collections.Generic;
using ZYSocket.FiberStream;
using System.Threading.Tasks;
using ZYSocket.Server.Builder;
using ZYSocket;
using System.IO.Compression;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace TestServer
{
    class Program
    {


        static X509Certificate certificate = new X509Certificate2(Environment.CurrentDirectory + "/service.pfx", "testPassword");

        //程序入口
        static void Main(string[] args)
        {

            //var server = new SockServBuilder(p =>
            //{
            //    return new ZYSocketSuper(p)
            //    {
            //        BinaryInput = new BinaryInputHandler(BinaryInputHandler),
            //        Connetions = new ConnectionFilter(ConnectionFilter),
            //        MessageInput = new DisconnectHandler(DisconnectHandler)
            //    };

            //}).Bulid();
            //server.Start(); //启动服务器 1000端口


            //var server2 = new SockServBuilder(p =>
            //{
            //    return new ZYSocketSuper(p)
            //    {
            //        BinaryInput = new BinaryInputHandler(BinaryInputHandler),
            //        Connetions = new ConnectionFilter(ConnectionFilter),
            //        MessageInput = new DisconnectHandler(DisconnectHandler)
            //    };

            //})

            //.ConfigServer(p =>
            //{
            //    p.Host = "ipv6any";
            //    p.Port = 1001;
            //})
            //.Bulid();
            //server2.Start(); //启动服务器 所有IPV6 1001端口



            var containerBuilder = new ServiceCollection();
            new SockServBuilder(containerBuilder, p =>
            {
                return new ZYSocketSuper(p)
                {
                    BinaryInput = new BinaryInputHandler(BinaryInputHandler),
                    Connetions = new ConnectionFilter(ConnectionFilter),
                    MessageInput = new DisconnectHandler(DisconnectHandler)
                };
            })
             .ConfigServer(p => {
                 p.Port = 1002;
                 p.MaxBufferSize = 8192;
             });

            var build = containerBuilder.BuildServiceProvider();

            var server3 = build.GetRequiredService<ISocketServer>();
            server3.Start(); //启动服务器 1002端口 缓冲区为8KB 


            Console.ReadLine();
            build.Dispose();
            Console.ReadLine();
        }




        /// <summary>
        /// 用户断开代理（你可以根据socketAsync 读取到断开的
        /// </summary>
        /// <param name="message">断开消息</param>
        /// <param name="socketAsync">断开的SOCKET</param>
        /// <param name="erorr">错误的ID</param>
        static void DisconnectHandler(string message, ISockAsyncEvent socketAsync, int erorr)
        {
            Console.WriteLine(message);
            socketAsync.UserToken = null;
            socketAsync.AcceptSocket?.Dispose();
        }
        /// <summary>
        /// 用户连接的代理
        /// </summary>
        /// <param name="socketAsync">连接的SOCKET</param>
        /// <returns>如果返回FALSE 则断开连接,这里注意下 可以用来封IP</returns>
        static bool ConnectionFilter(ISockAsyncEvent socketAsync)
        {
            Console.WriteLine("UserConn {0}", socketAsync.AcceptSocket.RemoteEndPoint.ToString());

            return true;
        }




        /// <summary>
        /// 数据包输入
        /// </summary>
        /// <param name="data">输入数据</param>
        /// <param name="socketAsync">该数据包的通讯SOCKET</param>
        static async void BinaryInputHandler(ISockAsyncEventAsServer socketAsync)
        {


            //USE SSL
            var (fiberRw, errMsg) = await socketAsync.GetFiberRwSSL<string>(certificate);

           // var (fiberRw, errMsg) = await socketAsync.GetFiberRwSSL<string>(certificate);

            if (fiberRw is null)
            {
                Console.WriteLine(errMsg);
                socketAsync.Disconnect();
                return;
            }

           // var fiberRw = await socketAsync.GetFiberRw<string>();

            fiberRw.UserToken = "my is ttk";

            byte[] data = new byte[4096];

            for (; ; )
            {
                try
                {
                    var x= System.Diagnostics.Stopwatch.StartNew();

                    var memory= await fiberRw.ReadLine(data);
                    x.Stop();                  
                    var str = System.Text.Encoding.Default.GetString(memory.Span);
                    Console.WriteLine(x.ElapsedTicks+":"+str);

                    fiberRw.Write("200\r\n",false);
                    await fiberRw.FlushAsync();


                }
                catch (System.Net.Sockets.SocketException)
                {
                    break;
                }
                catch (Exception er)
                {
                    Console.WriteLine(er.ToString());
                    break;
                }

            }

            socketAsync.Disconnect();

        }



    }
}

using System;
using ZYSocket.Server;
using System.Collections.Generic;
using ZYSocket.FiberStream;
using System.Threading.Tasks;
using ZYSocket.Server.Builder;
using ZYSocket;
using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;

namespace TestServer
{
    class Program
    {


        static X509Certificate certificate = new X509Certificate2(Environment.CurrentDirectory + "/server.pfx", "testPassword");

        //程序入口
        static void Main(string[] args)
        {



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
             .ConfigServer(p =>
             {
                 p.Port = 1002;
                 p.MaxBufferSize = 4096;
                 p.MaxConnectCout = 1;
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

            var (fiberRw, errMsg) = await socketAsync.GetFiberRwSSL<string>(certificate);

            Console.WriteLine("SSL OK");

            if (fiberRw is null)
            {
                Console.WriteLine(errMsg);
                socketAsync.Disconnect();
                return;
            }

            fiberRw.UserToken = "my is ttk";
                     

            for (; ; )
            {


                try
                {
                    using (var data = await fiberRw.ReadMemory())
                    {
                        int? x = await fiberRw.ReadInt32();

                        Console.WriteLine(data.Value.Length);
                        Console.WriteLine(x);

                        fiberRw.Write("ok");
                        await fiberRw.Flush();
                    }

                    break;

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

            socketAsync.AcceptSocket.Shutdown(System.Net.Sockets.SocketShutdown.Both);

        }

    }
      
}

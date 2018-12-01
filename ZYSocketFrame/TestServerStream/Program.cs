using System;
using ZYSocket.Server;
using System.Collections.Generic;
using ZYSocket.FiberStream;
using System.Threading.Tasks;
using ZYSocket.Server.Builder;
using Autofac;
using ZYSocket;
using System.IO.Compression;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;

namespace TestServer
{
    class Program
    {


        static X509Certificate certificate = new X509Certificate2(Environment.CurrentDirectory + "/server.pfx", "testPassword");

        //程序入口
        static void Main(string[] args)
        {

            var server = new SockServBuilder(p =>
            {
                return new ZYSocketSuper(p)
                {
                    BinaryInput = new BinaryInputHandler(BinaryInputHandler),
                    Connetions = new ConnectionFilter(ConnectionFilter),
                    MessageInput = new DisconnectHandler(DisconnectHandler)
                };

            }).Bulid();
            server.Start(); //启动服务器 1000端口


            var server2 = new SockServBuilder(p =>
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
                p.Host = "ipv6any";
                p.Port = 1001;
            })
            .Bulid();
            server2.Start(); //启动服务器 所有IPV6 1001端口



            ContainerBuilder containerBuilder = new ContainerBuilder();
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

            var build = containerBuilder.Build();

            var server3 = build.Resolve<ISocketServer>();
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
            socketAsync.AcceptSocket.Dispose();
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


            //USE SSL+GZIP
            var fiberRw = await socketAsync.GetFiberRwSSL<string>(certificate,(input, output) =>
            {
                var gzip_input = new GZipStream(input, CompressionMode.Decompress, true);
                var gzip_output = new GZipStream(output, CompressionMode.Compress, true);
                return (gzip_input, gzip_output); //return gzip mode
            });

            if (fiberRw is null)
            {
                socketAsync?.AcceptSocket?.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                return;
            }

            fiberRw.UserToken = "my is ttk";

            for (; ; )
            {                
                try
                {
                    //提供2种数据 读取写入方式
                    ReadBytes readBytes = await new ReadBytes(fiberRw).Init();
                    DataOn(ref readBytes, fiberRw);

                    await DataOnByLine(fiberRw);

                   
                }
                catch (Exception er)
                {
                    Console.WriteLine(er.ToString());
                    break;
                }          

            }

           fiberRw.Disconnect();

        }

        static async ValueTask DataOnByLine(IFiberRw<string> fiberRw)
        {
            var len = await fiberRw.ReadInt32();
            var cmd = await fiberRw.ReadInt32();
            var p1 = await fiberRw.ReadInt32();
            var p2 = await fiberRw.ReadInt64();
            var p3 = await fiberRw.ReadDouble();
            var p4 = await fiberRw.ReadSingle();
            var p5 = await fiberRw.ReadBoolean();
            var p6 = await fiberRw.ReadBoolean();
            var p7 = await fiberRw.ReadString();

            using (var p8 = await fiberRw.ReadMemory())
            {

                var p9 = await fiberRw.ReadInt16();
                var p10 = await fiberRw.ReadObject<List<Guid>>();

                fiberRw.Write(len.Value);
                fiberRw.Write(cmd.Value);
                fiberRw.Write(p1.Value);
                fiberRw.Write(p2.Value);
                fiberRw.Write(p3.Value);
                fiberRw.Write(p4.Value);
                fiberRw.Write(p5.Value);
                fiberRw.Write(p6.Value);
                fiberRw.Write(p7);
                fiberRw.Write(p8.Value);
                fiberRw.Write(p9.Value);
                fiberRw.Write(p10);
                await fiberRw.Flush();
            }


        }

        static void DataOn(ref ReadBytes read, IFiberRw<string> fiberRw)
        {

            var cmd = read.ReadInt32();
            var p1 = read.ReadInt32();
            var p2 = read.ReadInt64();
            var p3 = read.ReadDouble();
            var p4 = read.ReadSingle();
            var p5 = read.ReadBoolean();
            var p6 = read.ReadBoolean();
            var p7 = read.ReadString();
            var p8 = read.ReadMemory();
            var p9 = read.ReadInt16();
            var p10 = read.ReadObject<List<Guid>>();
            read.Dispose();

            using (WriteBytes writeBytes = new WriteBytes(fiberRw))
            {
                writeBytes.WriteLen();
                writeBytes.Cmd(cmd.Value);
                writeBytes.Write(p1.Value);
                writeBytes.Write(p2.Value);
                writeBytes.Write(p3.Value);
                writeBytes.Write(p4.Value);
                writeBytes.Write(p5.Value);
                writeBytes.Write(p6.Value);
                writeBytes.Write(p7);
                writeBytes.Write(p8);
                writeBytes.Write(p9.Value);
                writeBytes.Write(p10);
                writeBytes.Flush();
            }


        }

    }
}

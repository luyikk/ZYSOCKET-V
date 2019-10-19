using System;
using ZYSocket.Server;
using System.Collections.Generic;
using ZYSocket.FiberStream;
using System.Threading.Tasks;
using ZYSocket.Server.Builder;
using ZYSocket;
using ZYSocket.Share;
using Microsoft.Extensions.DependencyInjection;

namespace TestServer
{
    class Program
    {


        // static byte[] httpRespone;

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
             .ConfigISend(()=>new NetSend())
             .ConfigIAsyncSend(() => new NetSend())
             .ConfigServer(p => {
                 p.Port = 1002;
                 p.MaxBufferSize = 8;                
                 });

            var build = containerBuilder.BuildServiceProvider();

               var server3 = build.GetRequiredService<ISocketServer>();
            server3.Start(); //启动服务器 1002端口 缓冲区为4KB 


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
        static async void BinaryInputHandler(ISockAsyncEvent socketAsync)
        {

            var fiberRw = await socketAsync.GetFiberRw<string>();

            fiberRw.UserToken = "my is ttk";


            for (; ; )
            {

                //读取 发送 测试
                //var data = await fiberRw.ReadToBlockArrayEnd();
                //fiberRw.Write(data);
                //await fiberRw.Flush();

                try
                {
                    //提供2种数据 读取写入方式
                    //using (ReadBytes readBytes = new ReadBytes(fiberRw))
                    //{
                    //    await readBytes.Init();
                    //    DataOn(readBytes, fiberRw);
                    //}


                    await DataOnByLine(fiberRw);



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

            using var p8 = await fiberRw.ReadMemory();

            var p9 = await fiberRw.ReadInt16();

            // var p10 = await fiberRw.ReadObject<List<Guid>>();

            fiberRw.Write(len);
            fiberRw.Write(cmd);
            fiberRw.Write(p1);
            fiberRw.Write(p2);
            fiberRw.Write(p3);
            fiberRw.Write(p4);
            fiberRw.Write(p5);
            fiberRw.Write(p6);
            fiberRw.Write(p7);
            fiberRw.Write(p8);
            fiberRw.Write(p9);
            //fiberRw.Write(p10);
            await fiberRw.Flush();



        }

        static void DataOn(ReadBytes read, IFiberRw<string> fiberRw)
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


            // var p10 = read.ReadObject<List<Guid>>();
            // read.Dispose();


            using var writeBytes = new WriteBytes(fiberRw);

            writeBytes.WriteLen();
            writeBytes.Cmd(cmd);
            writeBytes.Write(p1);
            writeBytes.Write(p2);
            writeBytes.Write(p3);
            writeBytes.Write(p4);
            writeBytes.Write(p5);
            writeBytes.Write(p6);
            writeBytes.Write(p7);
            writeBytes.Write(p8);
            writeBytes.Write(p9);
            //writeBytes.Write(p10);
            writeBytes.Flush();



        }

    }
}

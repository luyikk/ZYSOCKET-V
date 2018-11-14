using System;
using ZYSocket.Server;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using ZYSocket.FiberStream;
using System.Threading.Tasks;
using Thruster;
using ZYSocket.Server.MemoryPool;

namespace TestServer
{
    class Program
    {
        //建立一个ZYSOCKETSERVER 对象 注意启动前应该先设置 App.Config 文件,
        //如果你不想设置App.Config文件 那么可以在构造方法里面传入相关的设置
        static ZYSocketSuper server;

       
        // static byte[] httpRespone;

        //程序入口
        static void Main(string[] args)
        {
           

            //httpRespone = File.ReadAllBytes("http.txt");
            server = new ZYSocketSuper("any", 1000, 1000, 1024 * 4)
            {               
                BinaryInput = new BinaryInputHandler(BinaryInputHandler), //设置输入代理
                Connetions = new ConnectionFilter(ConnectionFilter), //设置连接代理
                MessageInput = new MessageInputHandler(MessageInputHandler) //设置 客户端断开
            };

            server.Start(); //启动服务器

            Console.ReadLine();
        }




        /// <summary>
        /// 用户断开代理（你可以根据socketAsync 读取到断开的
        /// </summary>
        /// <param name="message">断开消息</param>
        /// <param name="socketAsync">断开的SOCKET</param>
        /// <param name="erorr">错误的ID</param>
        static void MessageInputHandler(string message, ZYSocketAsyncEventArgs socketAsync, int erorr)
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
        static bool ConnectionFilter(ZYSocketAsyncEventArgs socketAsync)
        {
            // Console.WriteLine("UserConn {0}", socketAsync.AcceptSocket.RemoteEndPoint.ToString());
            //socketAsync.UserToken = new AsyncSend(socketAsync.AcceptSocket,-1);

            return true;
        }


       

        /// <summary>
        /// 数据包输入
        /// </summary>
        /// <param name="data">输入数据</param>
        /// <param name="socketAsync">该数据包的通讯SOCKET</param>
        static async void BinaryInputHandler(ZYSocketAsyncEventArgs socketAsync)
        {          

            var fiberRw = await socketAsync.GetFiberRw<string>();

            fiberRw.UserToken = "my is ttk";

            for (; ; )
            {
                //读取 发送 测试
                //var data = await fiberRw.ReadToBlockArrayEnd();
                //WriteBytes writeBytes = new WriteBytes(fiberRw);
                //writeBytes.Write(data);
                //await writeBytes.AwaitFlush();

                try
                {
                    //提供2种数据 读取写入方式
                    ReadBytes readBytes = await new ReadBytes(fiberRw).Init();
                    DataOn(ref readBytes, fiberRw);

                    //  await DataOnByLine(fiberRw);

                }
                catch (Exception er)
                {
                    Console.WriteLine(er.ToString());
                    break;
                }

              

            }

            server.Disconnect(socketAsync.AcceptSocket);

        }

        static async ValueTask DataOnByLine(FiberRw<string> fiberRw)
        {
            var len = await fiberRw.ReadInt32();
            var cmd = await fiberRw.ReadInt32();
            var p1 = await fiberRw.ReadInt32();
            var p2 = await fiberRw.ReadInt64();
            var p3 = await fiberRw.ReadDouble();
            var p4 = await fiberRw.ReadSingle();
            var p5 = await fiberRw.ReadBoolean();
            var p6 = await fiberRw.ReadBoolean();
           // var p7 = await fiberRw.ReadString();

           // using (var p8 = await fiberRw.ReadMemory())
           // {

                var p9 = await fiberRw.ReadInt16();
                //var p10 = await fiberRw.ReadObject<List<Guid>>();

                await fiberRw.Write(len.Value);
                await fiberRw.Write(cmd.Value);
                await fiberRw.Write(p1.Value);
                await fiberRw.Write(p2.Value);
                await fiberRw.Write(p3.Value);
                await fiberRw.Write(p4.Value);
                await fiberRw.Write(p5.Value);
                await fiberRw.Write(p6.Value);
               // await fiberRw.Write(p7);
               // await fiberRw.Write(p8.Value);
                await fiberRw.Write(p9.Value);
                //await fiberRw.Write(p10);
           // }


        }

        static void  DataOn(ref ReadBytes read, FiberRw<string> fiberRw)
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

            WriteBytes writeBytes = new WriteBytes(fiberRw);
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

using AWaitServer;
using Microsoft.Extensions.DependencyInjection;
using Netx.Actor;
using Netx.Actor.Builder;
using System;
using System.Threading.Tasks;
using ZYSocket;
using ZYSocket.FiberStream;
using ZYSocket.Server;
using ZYSocket.Server.Builder;

namespace TestServer
{
    class Program
    {
        static IActorRun actor;

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
             });

            var build = containerBuilder.BuildServiceProvider();

            var server3 = build.GetRequiredService<ISocketServer>();
            server3.Start(); //启动服务器 1002端口 缓冲区为4KB 

            actor = new ActorBuilder()
             .UseActorLambda()
             .RegisterService<TestActorController>()
             .Build();

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


                try
                {                 
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

            var id = await fiberRw.ReadString();
            int waittime = await fiberRw.ReadInt32();
            var p = await fiberRw.ReadString();
            Console.WriteLine($"{id} start1");
            RunIng(fiberRw,id, waittime);
        }

        static async void RunIng(IFiberRw<string> fiberRw, string id, int time)
        {
            Console.WriteLine($"{id} start2");
            //await Task.Delay(time);         

            await actor.Get<ITestActorController>().Run();

            Console.WriteLine($"{id} close");

            fiberRw.Write(id);
            await fiberRw.Flush();
        }

    }
}

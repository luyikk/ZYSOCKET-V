using System;
using System.Threading.Tasks;
using ZYSocket;
using ZYSocket.Client;
using ZYSocket.FiberStream;

namespace Client
{
    class Program
    {
        static SocketClient client;

       

        static void Main(string[] args)
        {
            client = new SocketClient();
            client.BinaryInput += Client_BinaryInput;
            client.Disconnect += Client_Disconnect;

            while (true)
            {
                connect();

                Console.ReadLine();

                client.ShutdownBoth();

                Console.ReadLine();
            }
        }
     

        static void connect()
        {
            var (IsSuccess, Msg) =  client.Connect("127.0.0.1", 3000);
            Console.WriteLine(IsSuccess + ":" + Msg);

        }

        private static void Client_Disconnect(ISocketClient client, ISockAsyncEvent socketAsync, string msg)
        {
            Console.WriteLine(msg);
        }

        private static async void Client_BinaryInput(ISocketClient client, ISockAsyncEventAsClient socketAsync)
        {
            var fiberRw = await socketAsync.GetFiberRw();

            if(fiberRw==null)
            {
                client.ShutdownBoth();
                return;
            }

            fiberRw.Write(1000); //登入
            fiberRw.Write("test");
            fiberRw.Write("password");
            await fiberRw.Flush();

            for (; ; )
            {
                try
                {
                    await ReadCommand(fiberRw);
                }
                catch (Exception er)
                {
                    Console.WriteLine(er);
                    break;
                }
            }

            fiberRw.Disconnect();

        }

        static async Task ReadCommand(IFiberRw fiberRw)
        {
            var cmd = await fiberRw.ReadInt32();

            switch(cmd)
            {
                case 1001:
                    {
                        var isSuccess = await fiberRw.ReadBoolean();

                        Console.WriteLine(await fiberRw.ReadString());

                        if (isSuccess.Value)
                        {
                            TestLib.Data data = new TestLib.Data()
                            {
                                Id = Guid.NewGuid(),
                                Time = DateTime.Now
                            };

                            fiberRw.Write(2000); //发送数据
                            fiberRw.Write(data);
                            await fiberRw.Flush();

                            fiberRw.Write(3000); //发送消息
                            fiberRw.Write("EMMMMMMMMMMMMMMMMMMMMM...");
                            await fiberRw.Flush();
                        }
                      
                    }
                    break;
                case 3001:
                    {
                        Console.WriteLine(await fiberRw.ReadString());
                    }
                    break;
            }
        }
    }
}

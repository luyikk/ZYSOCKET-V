using System;
using System.Threading.Tasks;
using ZYSocket;
using ZYSocket.Client;
using ZYSocket.FiberStream;

namespace TestClient
{
    class Program
    {
        static SocketClient client;

        static int id = 0;

        static async Task Main(string[] args)
        {
            client = new SocketClient();
            client.BinaryInput += Client_BinaryInput;
            client.Disconnect += Client_Disconnect;

            while (true)
            {
                await connect();

                var fiberRw = await client.GetFiberRw();

                while (true)
                {
                    Console.ReadLine();
                    await  SendTest(fiberRw);
                }

            }
        }

        static async Task connect()
        {
            var result = await client.ConnectAsync("127.0.0.1", 1002, 60000);
            Console.WriteLine(result);
        }

        private static void Client_Disconnect(ISocketClient client, ISockAsyncEvent socketAsync, string msg)
        {
            Console.WriteLine(msg);
        }


        private static async Task SendTest(IFiberRw fiberRw)
        {
            fiberRw.Write((++id).ToString());
            fiberRw.Write(new Random().Next(10, 10000));
            fiberRw.Write(new Random().Next(10, 10000).ToString());
            await fiberRw.FlushAsync();
        }


        private static async void Client_BinaryInput(ISocketClient client, ISockAsyncEvent socketAsync)
        {
            var fiberRw = await socketAsync.GetFiberRw();

            client.SetConnected();

            while (true)
            {
                try
                {
                    await DataOnByLine(fiberRw);                  
                }
                catch
                {
                    break;
                }
            }


            client.ShutdownBoth(true);
        }



        static async ValueTask DataOnByLine(IFiberRw fiberRw)
        {
            var id = await fiberRw.ReadString();
            Console.WriteLine(id+" Close");
        }


    }
}

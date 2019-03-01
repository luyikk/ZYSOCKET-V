using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Threading.Tasks;
using ZYSocket;
using ZYSocket.Client;
using ZYSocket.FiberStream;

namespace TestClient
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

        static async void connect()
        {
            var result = await client.ConnectAsync("127.0.0.1", 1002, 60000);
            Console.WriteLine(result);

            var fiberRw = await client.GetFiberRw();

            for (; ; )
            {
               
                byte[] data = new byte[102400];
                fiberRw.Write(data);
                fiberRw.Write(1);

                await fiberRw.Flush();
            }
        }

        private static void Client_Disconnect(ISocketClient client, ISockAsyncEvent socketAsync, string msg)
        {
            Console.WriteLine(msg);
        }


     


        private static async void Client_BinaryInput(ISocketClient client, ISockAsyncEventAsClient socketAsync)
        {
            var res = await socketAsync.GetFiberRwSSL(null,"localhost");

            var fiberRw = res.FiberRw;

            client.SetConnect();

            while (true)
            {
                try
                {
                  

                    string a = await fiberRw.ReadString();

                    Console.WriteLine(a);
                }
                catch
                {
                    break;
                }
            }


            client.ShutdownBoth(true);
        }



     
    }
}

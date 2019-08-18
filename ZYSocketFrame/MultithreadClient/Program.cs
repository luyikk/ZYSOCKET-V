using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ZYSocket;
using ZYSocket.Client;
using ZYSocket.FiberStream;
using ZYSocket.FiberStream.Synchronization;

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

                fiberRw.Sync = new SyncRun();


                var task1 = Task.Factory.StartNew(async () =>
                   {
                       while (true)
                       {
                           try
                           {
                               await SendTest(fiberRw);
                           }
                           catch
                           {
                               break;
                           }
                       }
                   });

                var task2 = Task.Factory.StartNew(async () =>
                {
                    while (true)
                    {

                        try
                        {
                            await SendTest(fiberRw);
                        }
                        catch
                        {
                            break;
                        }
                    }
                });

                var task3 = Task.Factory.StartNew(async () =>
                {
                    while (true)
                    {

                        try
                        {
                            await SendTest(fiberRw);
                        }
                        catch
                        {
                            break;
                        }
                    }
                });

                var task4 = Task.Factory.StartNew(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            await SendTest(fiberRw);
                        }
                        catch
                        {
                            break;
                        }
                    }
                });



                await Task.WhenAll(task1, task2,task3,task4);
            }

        }

        private static async Task SendTest(IFiberRw fiberRw)
        {
            await await fiberRw.Sync.Ask(() =>
            {
                fiberRw.Write(System.Threading.Interlocked.Increment(ref id).ToString());
                return fiberRw.Flush();
            });
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



        static System.Diagnostics.Stopwatch stopwatch = new Stopwatch();

        static int count = 0;


        private static async void Client_BinaryInput(ISocketClient client, ISockAsyncEvent socketAsync)
        {
            var fiberRw = await socketAsync.GetFiberRw();

            client.SetConnected();

            stopwatch.Start();

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
     

            if (System.Threading.Interlocked.Increment(ref count) >1000000)
            {
                System.Threading.Interlocked.Exchange(ref count, 0);

                stopwatch.Stop();
                Console.WriteLine(stopwatch.ElapsedMilliseconds);
              
                stopwatch.Restart();
            }

          //  sendp(fiberRw);

        }

        static async void sendp(IFiberRw fiberRw)
        {
            await SendTest(fiberRw);
        }

    }
}

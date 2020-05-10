﻿using System;
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
                    SendTest(fiberRw);
                }
            
            }
        }

        static async Task connect()
        { 
            var result = await client.ConnectAsync("127.0.0.1", 1002,60000);
            Console.WriteLine(result);
        }

        private static void Client_Disconnect(ISocketClient client, ISockAsyncEvent socketAsync, string msg)
        {
            Console.WriteLine(msg);
        }


        private static void SendTest(IFiberRw fiberRw)
        {
            using (WriteBytes writeBytes = new WriteBytes(fiberRw))
            {
                writeBytes.WriteLen();
                writeBytes.Cmd(1001);
                writeBytes.Write(2);
                writeBytes.Write(5L);
                writeBytes.Write(5.5);
                writeBytes.Write(4.3f);
                writeBytes.Write(true);
                writeBytes.Write(false);
                writeBytes.Write("AAAAAAAAAAAAAA");
                writeBytes.Write("BBBBBBBBBBBBBBBB");
                writeBytes.Write((short)111);

                //List<Guid> guids = new List<Guid>();
                //for (int i = 0; i < 100; i++)
                //{
                //    guids.Add(Guid.NewGuid());
                //}
                //writeBytes.Write(guids);
                writeBytes.Flush();
            }
        }

     

        private static async void Client_BinaryInput(ISocketClient client, ISockAsyncEvent socketAsync)
        {
            var fiberRw = await socketAsync.GetFiberRw();

            client.SetConnected();

            long t = 0;
            var stop = System.Diagnostics.Stopwatch.StartNew();

            while (true)
            {
                try
                {

                    //var data = await fiberRw.ReadToBlockArrayEnd();
                    //WriteBytes writeBytes = new WriteBytes(fiberRw);
                    //writeBytes.Write(data);
                    //await writeBytes.AwaitFlush();

                    await DataOnByLine(fiberRw);

                    //Console.WriteLine("OK");

                    t++;

                    if (t > 10000)
                    {
                        t = 0;
                        stop.Stop();

                        Console.WriteLine(stop.ElapsedMilliseconds);

                        stop.Restart();
                    }
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
                // fiberRw.Write(p10);
                await fiberRw.Flush();
            }


           
        }


    }
}

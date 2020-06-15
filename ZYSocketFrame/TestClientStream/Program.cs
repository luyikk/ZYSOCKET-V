using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using ZYSocket;
using ZYSocket.Client;
using ZYSocket.FiberStream;

namespace TestClient
{
    class Program
    {
        static SocketClient client;

        static X509Certificate certificate = new X509Certificate2(Environment.CurrentDirectory + "/client.pfx", "testPassword");

        static async Task Main(string[] args)
        {
            client = new SocketClient();
            client.BinaryInput += Client_BinaryInput;
            client.Disconnect += Client_Disconnect;

            while (true)
            {
                await connect();

                Console.ReadLine();

                client.ShutdownBoth();

                Console.ReadLine();
            }
        }

        static async Task connect()
        {
            var result =await client.ConnectAsync("127.0.0.1", 1002,60000);
            Console.WriteLine(result);

            if (result.IsSuccess)
            {              

                var fiber = await client.GetFiberRw();

                SendTest(fiber);
            }

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
                writeBytes.Write("ssssssssssssssssssssssssssssssssssssss");
                writeBytes.Write("XXXXXXXXXXXXXXXXXXXXXXXXX");
                writeBytes.Write((short)111);

                List<Guid> guids = new List<Guid>();
                for (int i = 0; i < 1000; i++)
                {
                    guids.Add(Guid.NewGuid());
                }
                writeBytes.Write(guids);
                writeBytes.Flush();
            }
        }
                       

        private static async void Client_BinaryInput(ISocketClient client, ISockAsyncEventAsClient socketAsync)
        {

           // USE SSL+GZIP
            var res = await socketAsync.GetFiberRwSSL<string>(certificate, "localhost", (input, output) =>
               {
                   //var gzip_input = new GZipStream(input, CompressionMode.Decompress, true);
                   //var gzip_output = new GZipStream(output, CompressionMode.Compress, true);
                   //return new GetFiberRwResult(gzip_input, gzip_output); //return gzip mode          

                   var lz4_input = K4os.Compression.LZ4.Streams.LZ4Stream.Decode(input, leaveOpen: true);
                   var lz4_output = K4os.Compression.LZ4.Streams.LZ4Stream.Encode(output, leaveOpen: true);
                   return new GetFiberRwResult(lz4_input, lz4_output); //return lz4 mode
               });

            if (res.IsError)
            {
                Console.WriteLine(res.ErrMsg);
                client.ShutdownBoth(true);
                return;
            }

            var fiberRw = res.FiberRw;

            //var fiberRw = await socketAsync.GetFiberRw((input, output) =>
            //{
            //    var lz4_input = K4os.Compression.LZ4.Streams.LZ4Stream.Decode(input, leaveOpen: true);
            //    var lz4_output = K4os.Compression.LZ4.Streams.LZ4Stream.Encode(output, leaveOpen: true);
            //    return new GetFiberRwResult(lz4_input, lz4_output); //return lz4 mode
            //});

            // var fiberRw = await socketAsync.GetFiberRw();

            client.SetConnected();

            var testspeed = Stopwatch.StartNew();
            var i = 0L;
            while (true)
            {
                try
                {
                    i++;
                    await DataOnByLine(fiberRw);

                    if(i%10000==0)
                    {                      
                        Console.WriteLine(testspeed.ElapsedMilliseconds+" ms");
                        testspeed.Restart();
                    }

                    //Console.WriteLine("OK");

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
                var p10 = await fiberRw.ReadObject<List<Guid>>();

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
                fiberRw.Write(p10);
                await fiberRw.FlushAsync();
            }



        }


    }
}

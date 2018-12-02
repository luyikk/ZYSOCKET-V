using System;
using System.Collections.Generic;
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
            var (IsSuccess, Msg) = await client.ConnectAsync("127.0.0.1", 1002);         

            Console.WriteLine(IsSuccess + ":" + Msg);

            var fiber = await client.GetFiberRw();          

            SendTest(fiber);          

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
                for (int i = 0; i < 10; i++)
                {
                    guids.Add(Guid.NewGuid());
                }
                writeBytes.Write(guids);
                writeBytes.Flush();
            }
        }
                       

        private static async void Client_BinaryInput(ISocketClient client, ISockAsyncEventAsClient socketAsync)
        {

            //USE SSL+GZIP
            var fiberRw = await socketAsync.GetFiberRwSSL<string>(certificate,"localhost",(input, output) =>
            {
                var gzip_input = new GZipStream(input, CompressionMode.Decompress, true);
                var gzip_output = new GZipStream(output, CompressionMode.Compress, true);
                return (gzip_input, gzip_output); //return gzip mode          

            });

            if (fiberRw is null)
            {
                client.ShutdownBoth();
                return;
            }

            while (true)
            {
                try
                {

                    await DataOnByLine(fiberRw);

                    Console.WriteLine("OK");

                }
                catch
                {
                    break;
                }
            }

            fiberRw.Disconnect();

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


    }
}

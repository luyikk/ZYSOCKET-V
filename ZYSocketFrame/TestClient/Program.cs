using System;
using System.Collections.Generic;
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
            var (IsSuccess, Msg) = await client.ConnectAsync("127.0.0.1", 1002);
            Console.WriteLine(IsSuccess+":"+Msg);
        }

        private static void Client_Disconnect(ISocketClient client, ISockAsyncEvent socketAsync, string msg)
        {
            Console.WriteLine(msg);
        }


        private static async ValueTask SendTest(IFiberRw fiberRw)
        {
            WriteBytes writeBytes = new WriteBytes(fiberRw);
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
            await writeBytes.AwaitFlush();
        }


        private static async void Client_BinaryInput(ISocketClient client, ISockAsyncEvent socketAsync)
        {
            var fiberRw = await socketAsync.GetFiberRw((input,output)=>
            {
                return (input, output);
            });

            await SendTest(fiberRw);

            while (true)
            {
                try
                {

                    //var data = await fiberRw.ReadToBlockArrayEnd();
                    //WriteBytes writeBytes = new WriteBytes(fiberRw);
                    //writeBytes.Write(data);
                    //await writeBytes.AwaitFlush();

                    await DataOnByLine(fiberRw);
                  
                }
                catch
                {
                    break;
                }
            }


            client.ShutdownBoth();
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

                await fiberRw.Write(len.Value);
                await fiberRw.Write(cmd.Value);
                await fiberRw.Write(p1.Value);
                await fiberRw.Write(p2.Value);
                await fiberRw.Write(p3.Value);
                await fiberRw.Write(p4.Value);
                await fiberRw.Write(p5.Value);
                await fiberRw.Write(p6.Value);
                await fiberRw.Write(p7);
                await fiberRw.Write(p8.Value);
                await fiberRw.Write(p9.Value);
                await fiberRw.Write(p10);
            }


           
        }


    }
}

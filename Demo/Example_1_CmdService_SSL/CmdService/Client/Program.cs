﻿using System;
using System.IO.Compression;
using System.Threading.Tasks;
using TestLib;
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
            client = new SocketClient(); //创建client
            client.BinaryInput += Client_BinaryInput; //注册链接成功后数据报读取事件
            client.Disconnect += Client_Disconnect; //注册断开后如何处理

            while (true)
            {
                connect();

                Console.ReadLine();

                client.ShutdownBoth();

                Console.ReadLine();
            }
        }
     
        //链接服务器
        static async void connect()
        {
            var result =  client.Connect("127.0.0.1", 3000,60000); //同步链接
           // var (IsSuccess, Msg) = await client.ConnectAsync("127.0.0.1", 3000); //异步链接
            Console.WriteLine(result);

            var fiberRw = await client.GetFiberRw();

            await await fiberRw.Sync.Ask(() =>
            {
                fiberRw.Write(1000); //登入
                fiberRw.Write("test");
                fiberRw.Write("password");
                return fiberRw.FlushAsync();
            });

            //for (; ; ) //我们也可以在这里处理数据
            //{
            //    try
            //    {
            //        await ReadCommand(fiberRw);
            //    }
            //    catch (Exception er)
            //    {
            //        Console.WriteLine(er);
            //        break;
            //    }
            //}

            //fiberRw.Disconnect();

        }

        private static void Client_Disconnect(ISocketClient client, ISockAsyncEvent socketAsync, string msg)
        {
            Console.WriteLine(msg);
        }

        private static async void Client_BinaryInput(ISocketClient client, ISockAsyncEventAsClient socketAsync)
        {

            var res = await socketAsync.GetFiberRwSSL(null, "");  //我们在这地方使用SSL加密

            if (res.IsError)
            {
                Console.WriteLine(res.ErrMsg);
                client.ShutdownBoth();
                return;
            }

            client.SetConnected();

        
            for (; ; ) //我们可以在这里处理数据或者在上面
            {
                try
                {
                    await ReadCommand(res.FiberRw);
                }
                catch (Exception er)
                {
                    Console.WriteLine(er);
                    break;
                }
            }

            client.ShutdownBoth();

        }

        static async Task ReadCommand(IFiberRw fiberRw)
        {
            var cmd = await fiberRw.ReadInt32();

            switch (cmd)
            {
                case 1001:
                    {
                        var isSuccess = await fiberRw.ReadBoolean();

                        Console.WriteLine(await fiberRw.ReadString());

                        if (isSuccess)
                        {
                            Data data = new Data()
                            {
                                Id = Guid.NewGuid(),
                                Time = DateTime.Now
                            };

                            await await fiberRw.Sync.Ask(() =>
                            {
                                fiberRw.Write(2000); //发送数据
                                fiberRw.Write(data);

                                fiberRw.Write(3000); //发送消息                          
                                fiberRw.Write("EMMMMMMMMMMMMMMMMMMMMM...");
                                return fiberRw.FlushAsync();
                            });
                        }

                    }
                    break;
                case 3001:
                    {
                        using (var data = await fiberRw.ReadMemory())
                        { 
                            Console.WriteLine(data.Value.Length);

                            await await fiberRw.Sync.Ask(() =>
                            {
                                fiberRw.Write(3000); //发送消息                          
                                fiberRw.Write("EMMMMMMMMMMMMMMMMMMMMM...");
                                return fiberRw.FlushAsync();
                            });
                        }
                    }
                    break;
            }
        }
    }

}

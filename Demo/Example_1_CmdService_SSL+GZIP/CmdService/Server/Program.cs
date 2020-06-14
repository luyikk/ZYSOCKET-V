﻿using System;
using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using ZYSocket;
using ZYSocket.FiberStream;
using ZYSocket.Server.Builder;

namespace Server
{
    class Program
    {
        static X509Certificate certificate = new X509Certificate2("server.pfx", "testPassword");

        static void Main(string[] args)
        {
            using (var build = new SockServBuilder(p =>
             {
                   return new ZYSocket.Server.ZYSocketSuper(p)
                   {
                       BinaryInput = new ZYSocket.Server.BinaryInputHandler(BinaryInputHandler),
                       MessageInput = new ZYSocket.Server.DisconnectHandler(DisconnectHandler),
                       Connetions = new ZYSocket.Server.ConnectionFilter(ConnectionFilter)
                   };

             }).ConfigServer(p => p.Port = 3000))
            {
                build.Bulid().Start();
                Console.ReadLine();
            }

        }

        static bool ConnectionFilter(ISockAsyncEventAsServer socketAsync)
        {
            Console.WriteLine($"{socketAsync?.AcceptSocket?.RemoteEndPoint} connect"); //打印连接
            return true;
        }

        static void DisconnectHandler(string message, ISockAsyncEventAsServer socketAsync, int erorr)
        {
            Console.WriteLine($"{message}");
            socketAsync.UserToken = null; //在这里我们转换成userinfo 然后做一些用户断开后的操作
            socketAsync.AcceptSocket.Close();
            socketAsync.AcceptSocket.Dispose();
        }

        static async void BinaryInputHandler(ISockAsyncEventAsServer socketAsync)
        {
            var (fiberW,errMsg) = await socketAsync.GetFiberRwSSL<UserInfo>(certificate,(input,output)=> //在GZIP的基础上在通过SSL 加密
            {
                var gzip_input = new GZipStream(input, CompressionMode.Decompress,true); //注意第三个参数
                var gzip_output = new GZipStream(output, CompressionMode.Compress,true);
                return new GetFiberRwResult(gzip_input, gzip_output);

            });  //我们在这地方使用SSL加密
           

            if (fiberW is null) //如果获取失败 那么断开连接
            {
                Console.WriteLine(errMsg);
                socketAsync.Disconnect();
                return;
            }

            for (; ; ) //循环读取处理数据表 类似于 协程
            {
                try
                {
                    await ReadCommand(fiberW);
                }
                catch (System.Net.Sockets.SocketException)
                {
                    break;
                }
                catch (Exception er)
                {
                    Console.WriteLine(er.ToString()); //出现异常 打印，并且结束循环，断开连接
                    break;
                }
            }

            socketAsync.Disconnect();
        }

        static async Task ReadCommand(IFiberRw<UserInfo> fiberRw)
        {
            int? cmd = await fiberRw.ReadInt32();

            switch(cmd)
            {
                case 1000: //用户登入，我们需要读取一个用户名 一个密码 然后验证
                    {
                        string username = await fiberRw.ReadString();
                        string password = await fiberRw.ReadString();

                        if (string.Equals(username,"test",StringComparison.Ordinal) && string.Equals(password,"password",StringComparison.Ordinal))
                        {
                            fiberRw.UserToken = new UserInfo()
                            {
                                UserName=username,
                                Password=password
                            };

                            await await fiberRw.Sync.Ask(() =>
                            {
                                fiberRw.Write(1001);  //发送登入成功
                                fiberRw.Write(true);
                                fiberRw.Write("logon ok");
                                return fiberRw.FlushAsync();
                            });
                        }
                        else
                        {
                            await await fiberRw.Sync.Ask(() =>
                            {
                                fiberRw.Write(1001); //发送登入失败
                                fiberRw.Write(false);
                                fiberRw.Write("logon fail");
                                return fiberRw.FlushAsync();
                            });
                        }                      
                    }
                    break;
                case 2000: //读取一个数据 然后保存到当前用户对象中
                    {
                        if (fiberRw.UserToken != null)
                        {
                            fiberRw.UserToken.Data = await fiberRw.ReadObject<TestLib.Data>();
                        }
                        else
                            fiberRw.Async.Disconnect();
                    }
                    break;
                case 3000: //在屏幕上显示消息 然后告诉客户端显示成功
                    {
                        string msg = await fiberRw.ReadString();
                        Console.WriteLine(msg);

                        await await fiberRw.Sync.Ask(() =>
                        {
                            fiberRw.Write(3001);
                            fiberRw.Write("msg show");
                            return fiberRw.FlushAsync();
                        });
                    }
                    break;

            }
        }



    }


}

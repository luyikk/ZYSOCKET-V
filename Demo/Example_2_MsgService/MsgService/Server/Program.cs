﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ZYSocket;
using ZYSocket.FiberStream;
using ZYSocket.Server.Builder;
using System.Security.Cryptography.X509Certificates;

namespace Server
{
    class Program
    {
        static X509Certificate certificate = new X509Certificate2("server.pfx", "testPassword");

        static List<IFiberRw<UserInfo>> UserList = new List<IFiberRw<UserInfo>>();

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

            })
             .ConfigServer(p => p.Port = 3000)) //监听所有IPV4 的3000端口
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

        static async void DisconnectHandler(string message, ISockAsyncEventAsServer socketAsync, int erorr)
        {
            Console.WriteLine($"{message}");

            if(socketAsync.UserToken is IFiberRw<UserInfo> user)
            {
                UserList.Remove(user);

                foreach (var item in UserList.AsReadOnly())
                {
                    item.Write(4000);
                    item.Write(user.UserToken.UserName);
                    await item.Flush();
                }
            }

            socketAsync.UserToken = null; //在这里我们转换成userinfo 然后做一些用户断开后的操作
            socketAsync.AcceptSocket.Close();
            socketAsync.AcceptSocket.Dispose();
        }

        static async void BinaryInputHandler(ISockAsyncEventAsServer socketAsync)
        {
            var (fiberW,errMsg) = await socketAsync.GetFiberRwSSL<UserInfo>(certificate); //获取一个异步基础流

            if (fiberW is null) //如果获取失败 那么断开连接
            {
                Console.WriteLine(errMsg);
                socketAsync?.AcceptSocket?.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                return;
            }

            for (; ; ) //循环读取处理数据表 类似于 协程
            {
                try
                {
                    await ReadCommand(fiberW);
                }
                catch (Exception er)
                {
                    Console.WriteLine(er.ToString()); //出现异常 打印，并且结束循环，断开连接
                    break;
                }
            }

            fiberW.Disconnect();
        }

        static bool CheckLogOn(string username,string password)
        {
            if (UserList.SingleOrDefault(p => p.UserToken.UserName.Equals(username))==null)  //检查相同用户名 重复登入
            {
                return true;
            }

            return false;
        }

        static async Task ReadCommand(IFiberRw<UserInfo> fiberRw)
        {
            int? cmd = await fiberRw.ReadInt32();

            switch (cmd)
            {
                case 1000: //用户登入，我们需要读取一个用户名 一个密码 然后验证
                    {
                        string username = await fiberRw.ReadString();
                        string password = await fiberRw.ReadString();

                        if (CheckLogOn(username,password))
                        {
                            fiberRw.UserToken = new UserInfo()
                            {
                                UserName = username,
                            };

                            fiberRw.Async.UserToken = fiberRw; //我们可以断开后对userinfo做一些事情

                            UserList.Add(fiberRw);

                            fiberRw.Write(1001);  //发送登入成功
                            fiberRw.Write(true);
                            fiberRw.Write("logon ok");
                            await fiberRw.Flush();
                        }
                        else
                        {
                            fiberRw.Write(1001); //发送登入失败
                            fiberRw.Write(false);
                            fiberRw.Write("logon fail");
                            await fiberRw.Flush();
                        }
                    }
                    break;
                case 2000: //GET USERLIST
                    {
                        if (fiberRw.UserToken != null)
                        {
                            var x = from p in UserList
                                    where p != fiberRw
                                    select p.UserToken.UserName;

                            fiberRw.Write(2001);
                            fiberRw.Write(x.ToList());
                            await fiberRw.Flush();

                            foreach (var item in UserList.Where(p=>p!=fiberRw))
                            {
                                item.Write(2002);
                                item.Write(fiberRw.UserToken.UserName);
                                await item.Flush();
                            }
                        }
                    }
                    break;
                case 3000:
                    {
                        if (fiberRw.UserToken != null)
                        {
                            string targetuser = await fiberRw.ReadString();
                            string msg = await fiberRw.ReadString();

                            if (targetuser.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                            {                               

                                foreach (var item in UserList.AsReadOnly())
                                {
                                    if (item != fiberRw)
                                    {
                                        item.Write(3001);
                                        item.Write(fiberRw.UserToken.UserName);
                                        item.Write(msg);
                                        await item.Flush();
                                    }
                                }
                            }
                            else
                            {
                                var user = UserList.FirstOrDefault(p => p.UserToken.UserName == targetuser);

                                if(user!=null)
                                {
                                    user.Write(3002);
                                    user.Write(fiberRw.UserToken.UserName);
                                    user.Write(msg);
                                    await user.Flush();
                                }

                            }
                        }
                    }
                    break;

            }
        }



    }


}
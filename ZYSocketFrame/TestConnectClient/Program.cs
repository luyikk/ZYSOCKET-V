using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Net.Security;
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
            var result = await client.ConnectAsync("127.0.0.1", 1002, 60000);
            Console.WriteLine(result);

            var fiberRw = await client.GetFiberRw();

            //for (; ; )
            //{
               
                byte[] data = new byte[102400];
                fiberRw.Write(data);
                fiberRw.Write(1);

                await fiberRw.Flush();
            //}
        }

        private static void Client_Disconnect(ISocketClient client, ISockAsyncEvent socketAsync, string msg)
        {
            Console.WriteLine(msg);
        }


     


        private static async void Client_BinaryInput(ISocketClient client, ISockAsyncEventAsClient socketAsync)
        {
            var res = await socketAsync.GetFiberRwSSL(async stream=>
            {
                var sslstream = new SslStream(stream, false, (sender, certificate, chain, errors) => true,
                    (sender, host, certificates, certificate, issuers)
                    => certificate);

                try
                {
                    await sslstream.AuthenticateAsClientAsync("localhost");
                }
                catch (Exception er)
                {
                    Console.WriteLine(er.Message);
                    return null;
                }

                return sslstream;
            });

            var fiberRw = res.FiberRw;

            client.SetConnected();

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

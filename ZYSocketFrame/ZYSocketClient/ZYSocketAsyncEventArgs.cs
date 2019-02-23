﻿using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ZYSocket.FiberStream;
using ZYSocket.Share;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace ZYSocket.Client
{

    public class ZYSocketAsyncEventArgs : SocketAsyncEventArgs, ISockAsyncEventAsClient
    {
      

        private readonly IFiberReadStream RStream;

        private readonly IFiberWriteStream WStream;
      
        private bool IsInit = false;

        private readonly MemoryPool<byte> MemoryPool;

        public  bool IsLittleEndian { get; private set; }
        public  Encoding Encoding { get; private set; }
        public ISend SendImplemented { get;  private set; }
        public IAsyncSend AsyncSendImplemented { get; private set; }

        private TaskCompletionSource<IFiberRw> taskCompletionSource;
        private IDisposable fiberobj;
        private IDisposable fiberT;
        private IDisposable fibersslobj;
        private IDisposable fibersslT;


        private int _check_thread = 0;     

        public int Add_check()
        {
            _check_thread++;
            return _check_thread;
        }

        public void Reset_check()
        {
            _check_thread = 0;
        }

        public bool IsStartReceive { get; set; }

        public ZYSocketAsyncEventArgs(TaskCompletionSource<IFiberRw> completionSource ,IFiberReadStream r_stream, IFiberWriteStream w_stream, ISend send,IAsyncSend asyncsend, MemoryPool<byte> memoryPool, Encoding encoding, bool isLittleEndian=false)
        {
            this.taskCompletionSource = completionSource;
            this.MemoryPool = memoryPool;
            this.RStream = r_stream;
            this.WStream = w_stream;
            this.Encoding = encoding;
            base.Completed += ZYSocketAsyncEventArgs_Completed;
            IsLittleEndian = isLittleEndian;
            SendImplemented = send;
            AsyncSendImplemented = asyncsend;
            RStream.BeginReadFunc = BeginRead;
            RStream.EndBeginReadFunc = EndBeginRead;            
        }

        public Action Receive { get => RStream.Receive;set { RStream.Receive = value; } }
        


        private IAsyncResult BeginRead(byte[] data, int offset, int count, AsyncCallback callback, object state)
        {
            return this.ConnectSocket.BeginReceive(data, offset, count, SocketFlags.None, callback, state);

        }

        private int EndBeginRead(IAsyncResult asyncResult)
        {
            return this.ConnectSocket.EndReceive(asyncResult);
        }


        private void ZYSocketAsyncEventArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            if(Completed!=null)
                this.Completed(sender, this);
        }

        public new event EventHandler<ZYSocketAsyncEventArgs> Completed;

        public async ValueTask<IFiberRw> GetFiberRw(Func<Stream, Stream, GetFiberRwResult> init = null)
        {
            if (await RStream.WaitStreamInit())
            {
             

                var fiber= new FiberRw<object>(this, RStream, WStream, MemoryPool, Encoding, IsLittleEndian, init: init);
                fiberobj = fiber;
                taskCompletionSource?.TrySetResult(fiber);
                return fiber;
            }
            else
                return null;
        }

        public async ValueTask<IFiberRw<T>> GetFiberRw<T>(Func<Stream, Stream, GetFiberRwResult> init = null) where T:class
        {
            if (await RStream.WaitStreamInit())
            {
                var fiber= new FiberRw<T>(this, RStream, WStream, MemoryPool, Encoding, IsLittleEndian,init: init);
                fiberobj = fiber;
                taskCompletionSource?.TrySetResult(fiber);
                return fiber;
            }
            else
                return null;
        }

        public async ValueTask<GetFiberRwSSLResult> GetFiberRwSSL(X509Certificate certificate_client, string targethost="localhost", Func<Stream, Stream, GetFiberRwResult> init = null)
        {
            if (await RStream.WaitStreamInit())
            {

                var mergestream = new MergeStream(RStream as Stream, WStream as Stream);
                var sslstream = new SslStream(mergestream, false, (sender, certificate, chain, errors) => true,
                (sender, host, certificates, certificate, issuers) => certificate_client);

                try
                {
                    await sslstream.AuthenticateAsClientAsync(targethost);
                }
                catch (Exception er)
                {
                    return new GetFiberRwSSLResult { IsError = true, FiberRw = null, ErrMsg = er.Message };                  
                }

                var fiber = new FiberRw<object>(this, RStream, WStream, MemoryPool, Encoding, IsLittleEndian, sslstream, sslstream, init: init);
                fibersslobj = fiber;
                taskCompletionSource?.TrySetResult(fiber);
                return new GetFiberRwSSLResult { IsError = false, FiberRw = fiber, ErrMsg = null };

            }
            else
                return new GetFiberRwSSLResult { IsError = true, FiberRw = null, ErrMsg = "not install" };

        }

        public async ValueTask<GetFiberRwSSLResult<T>> GetFiberRwSSL<T>(X509Certificate certificate_client, string targethost = "localhost", Func<Stream, Stream, GetFiberRwResult> init = null) where T : class
        {
            if (await RStream.WaitStreamInit())
            {
                
                var mergestream = new MergeStream(RStream as Stream, WStream as Stream);
                var sslstream = new SslStream(mergestream, false, (sender, certificate, chain, errors) => true,
                (sender, host, certificates, certificate, issuers) => certificate_client);

                try
                {
                    await sslstream.AuthenticateAsClientAsync(targethost);
                }
                catch (Exception er)
                {
                    return new GetFiberRwSSLResult<T> { IsError = true, FiberRw = null, ErrMsg = er.Message };
                  
                }
               
                var fiber = new FiberRw<T>(this, RStream, WStream, MemoryPool, Encoding, IsLittleEndian, sslstream, sslstream, init: init);
                fibersslT = fiber;
                taskCompletionSource?.TrySetResult(fiber);
                return new GetFiberRwSSLResult<T> { IsError = false, FiberRw = fiber, ErrMsg = null };


            }
            else
                return new GetFiberRwSSLResult<T> { IsError= true, FiberRw= null, ErrMsg= "not install" };
        }


        public void StreamInit()
        {
            if (!IsInit)
            {
                IsInit = true;
                RStream.StreamInit();
            }
        }



        public void SetBuffer(int inthint)
        {           
            var mem = RStream.GetArray(inthint);
            base.SetBuffer(mem.Array, mem.Offset, mem.Count);
        }

        public void Reset()
        {
            fiberobj?.Dispose();
            fiberT?.Dispose();
            fibersslobj?.Dispose();
            fibersslT?.Dispose();

            fiberobj = null;
            fiberT = null;
            fibersslobj = null;
            fibersslT = null;

            base.SetBuffer(null, 0, 0);
            IsInit = false;
            RStream.Reset();
            WStream.Close();
            this.AcceptSocket = null;            
        }

        public PipeFilberAwaiter Advance(int bytesTransferred)
        {
            return RStream.Advance(bytesTransferred);
        }

        public PipeFilberAwaiter Advance()
        {
            return RStream.Advance(BytesTransferred);
        }

        public PipeFilberAwaiter ReadCanceled()
        {
            return RStream.ReadCanceled();
        }

     
    }

  
}

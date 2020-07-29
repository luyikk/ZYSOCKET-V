﻿using System;
using System.Buffers;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using ZYSocket.FiberStream;
using ZYSocket.Share;
using ZYSocket.Interface;

namespace ZYSocket.Server
{

    public class ZYSocketAsyncEventArgs : SocketAsyncEventArgs, ISockAsyncEventAsServer
    {      

        private readonly IFiberReadStream RStream;
        private readonly IFiberWriteStream WStream;      
        private bool isInit = false;
        public bool IsInit => isInit;

        private readonly MemoryPool<byte> MemoryPool;

        public  bool IsLittleEndian { get;  }
        public  Encoding Encoding { get;  }
        public ISerialization? ObjFormat { get;  }

        public ISend SendImplemented { get;   }
        public IAsyncSend AsyncSendImplemented { get;  } 

        private IDisposable? fiberobj;
        private IDisposable? fiberT;
        private IDisposable? fibersslobj;
        private IDisposable? fibersslT;       
        public new event EventHandler<ZYSocketAsyncEventArgs>? Completed;

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

      


        public ZYSocketAsyncEventArgs(IFiberReadStream r_stream, IFiberWriteStream w_stream, ISend send,IAsyncSend asyncsend, MemoryPool<byte> memoryPool, Encoding encoding, ISerialization? objFormat = null,bool isLittleEndian=false)
        {
            this.MemoryPool = memoryPool;
            this.RStream = r_stream;
            this.WStream = w_stream;
            this.Encoding = encoding;
            this.ObjFormat = objFormat;
            base.Completed += ZYSocketAsyncEventArgs_Completed;
            IsLittleEndian = isLittleEndian;
            SendImplemented = send;
            AsyncSendImplemented = asyncsend;               
        }

   

        private void ZYSocketAsyncEventArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            this.Completed!(sender, this);
        }



        public async ValueTask<IFiberRw?> GetFiberRw(Func<Stream, Stream, GetFiberRwResult>? init = null)
        {
            if (await RStream.WaitStreamInit())
            { 
                var fiber= new FiberRw<object>(this, RStream, WStream, MemoryPool, Encoding, ObjFormat,IsLittleEndian, init:init);
                fiberobj = fiber;
                return fiber;
            }
            else
                return default;
        }

        public async ValueTask<IFiberRw<T>?> GetFiberRw<T>(Func<Stream, Stream, GetFiberRwResult>? init = null) where T:class
        {
            if (await RStream.WaitStreamInit())
            {       
                var fiber= new FiberRw<T>(this, RStream, WStream, MemoryPool, Encoding, ObjFormat,IsLittleEndian, init:init);
                fiberT = fiber;
                return fiber;
            }
            else
                return default;
        }

        public async ValueTask<(IFiberRw?,string?)> GetFiberRwSSL(X509Certificate certificate, Func<Stream, Stream, GetFiberRwResult>? init = null)
        {
            if (await RStream.WaitStreamInit())
            {
                var mergestream = new MergeStream((RStream as Stream)!, (WStream as Stream)!);              
                var sslstream = new SslStream(mergestream, false);
                try
                {
                    await sslstream.AuthenticateAsServerAsync(certificate);
                }
                catch(Exception er)
                {
                    return (null,er.Message);
                }                   
                var fiber= new FiberRw<object>(this, RStream, WStream, MemoryPool, Encoding, ObjFormat, IsLittleEndian, sslstream, sslstream, init: init);
                fibersslobj = fiber;
                return (fiber,null);
            }
            else
                return (null,"not install");
        }

        public async ValueTask<(IFiberRw?, string?)> GetFiberRwSSL(Func<Stream,Task<SslStream>> sslstream_init,Func<Stream, Stream, GetFiberRwResult>? init = null)
        {
            if (await RStream.WaitStreamInit())
            {
                var mergestream = new MergeStream((RStream as Stream)!, (WStream as Stream)!);
                SslStream sslstream =await sslstream_init(mergestream);
                if (sslstream is null)
                    return (null,"sslstream init fail");
                var fiber = new FiberRw<object>(this, RStream, WStream, MemoryPool, Encoding, ObjFormat, IsLittleEndian, sslstream, sslstream, init: init);
                fibersslobj = fiber;
                return (fiber, null);
            }
            else
                return (null, "not install");
        }

        public async ValueTask<(IFiberRw<T>?,string?)> GetFiberRwSSL<T>(X509Certificate certificate, Func<Stream, Stream, GetFiberRwResult>? init = null) where T : class
        {
            if (await RStream.WaitStreamInit())
            {
                var mergestream = new MergeStream((RStream as Stream)!, (WStream as Stream)!);               
                var sslstream = new SslStream(mergestream, false);
                try
                {
                    await sslstream.AuthenticateAsServerAsync(certificate);
                }
                catch(Exception er)
                {
                    return (null,er.Message);
                }                    
                var fiber= new FiberRw<T>(this, RStream, WStream, MemoryPool, Encoding, ObjFormat, IsLittleEndian, sslstream, sslstream, init: init);
                fibersslT = fiber;
                return (fiber,null);
            }
            else
                return (null,"not install");
        }

        public async ValueTask<(IFiberRw<T>?, string?)> GetFiberRwSSL<T>(Func<Stream, Task<SslStream>> sslstream_init, Func<Stream, Stream, GetFiberRwResult>? init = null) where T : class
        {
            if (await RStream.WaitStreamInit())
            {
                var mergestream = new MergeStream((RStream as Stream)!, (WStream as Stream)!);              
                SslStream sslstream = await sslstream_init(mergestream);
                if (sslstream is null)
                    return (null, "sslstream init fail");
                var fiber = new FiberRw<T>(this, RStream, WStream, MemoryPool, Encoding, ObjFormat, IsLittleEndian, sslstream, sslstream, init: init);
                fibersslT = fiber;
                return (fiber, null);
            }
            else
                return (null, "not install");
        }


        public void StreamInit()
        {
            if (!isInit)
            {
                isInit = true;
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
            isInit = false;
            RStream.Reset();
            WStream.Close();
            this.AcceptSocket = null;         
            
        }

        public void Disconnect()
        {
            try
            {
                if (isInit)                
                    AcceptSocket?.Shutdown(System.Net.Sockets.SocketShutdown.Both);                  
                
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception)
            {

            }
        }

        public void Advance(int bytesTransferred)
        {
             RStream.Advance(bytesTransferred);
        }

        public void Advance()
        {
             RStream.Advance(BytesTransferred);
        }

   

     
    }

  
}

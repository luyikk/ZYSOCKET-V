using System;
using System.Buffers;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using ZYSocket.FiberStream;
using ZYSocket.Share;

namespace ZYSocket.Server
{

    public class ZYSocketAsyncEventArgs : SocketAsyncEventArgs, ISockAsyncEventAsServer
    {
      

        private readonly IFiberReadStream RStream;

        private readonly IFiberWriteStream WStream;
      
        private bool isInit = false;
        public bool IsInit => isInit;

        private readonly MemoryPool<byte> MemoryPool;

        public  bool IsLittleEndian { get; private set; }
        public  Encoding Encoding { get; private set; }
        public ISend SendImplemented { get;  private set; }
        public IAsyncSend AsyncSendImplemented { get; private set; } 

        private int _check_thread = 0;

        private IDisposable fiberobj;
        private IDisposable fiberT;
        private IDisposable fibersslobj;
        private IDisposable fibersslT;
        public Action<ZYSocketAsyncEventArgs> DisconnectIt { get; set; }

        private bool IsStartReceive = false;

        public Action<ZYSocketAsyncEventArgs> StartReceiveAction { get; set; }             
     

        public void StartReceive()
        {          
            if (!IsStartReceive)
            {
                IsStartReceive = true;
                StartReceiveAction?.Invoke(this);
            }          
        }

        public int Add_check()
        {
            _check_thread++;
            return _check_thread;
        }

        public void Reset_check()
        {
            _check_thread = 0;
        }

        public ZYSocketAsyncEventArgs(IFiberReadStream r_stream, IFiberWriteStream w_stream, ISend send,IAsyncSend asyncsend, MemoryPool<byte> memoryPool, Encoding encoding, bool isLittleEndian=false)
        {
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
            RStream.Receive = StartReceive;
        }

        private IAsyncResult BeginRead(byte[] data, int offset, int count, AsyncCallback callback, object state)
        {
            if (this.AcceptSocket != null)
                return this.AcceptSocket.BeginReceive(data, offset, count, SocketFlags.None, callback, state);
            else
                throw new ObjectDisposedException("AcceptSocket is null");

        }

        private int EndBeginRead(IAsyncResult asyncResult)
        {
            return this.AcceptSocket.EndReceive(asyncResult);
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
                StartReceive();
                var fiber= new FiberRw<object>(this, RStream, WStream, MemoryPool, Encoding, IsLittleEndian,init:init);
                fiberobj = fiber;
                return fiber;
            }
            else
                return null;
        }

        public async ValueTask<IFiberRw<T>> GetFiberRw<T>(Func<Stream, Stream, GetFiberRwResult> init = null) where T:class
        {
            if (await RStream.WaitStreamInit())
            {
                StartReceive();
                var fiber= new FiberRw<T>(this, RStream, WStream, MemoryPool, Encoding, IsLittleEndian, init:init);
                fiberT = fiber;
                return fiber;
            }
            else
                return null;
        }

        public async ValueTask<(IFiberRw,string)> GetFiberRwSSL(X509Certificate certificate, Func<Stream, Stream, GetFiberRwResult> init = null)
        {
            if (await RStream.WaitStreamInit())
            {
                var mergestream = new MergeStream(RStream as Stream, WStream as Stream);              
                var sslstream = new SslStream(mergestream, false);
                try
                {
                    await sslstream.AuthenticateAsServerAsync(certificate);
                }
                catch(Exception er)
                {
                    return (null,er.Message);
                }

                StartReceive();
                var fiber= new FiberRw<object>(this, RStream, WStream, MemoryPool, Encoding, IsLittleEndian, sslstream, sslstream, init: init);
                fibersslobj = fiber;
                return (fiber,null);
            }
            else
                return (null,"not install");
        }

        public async ValueTask<(IFiberRw<T>,string)> GetFiberRwSSL<T>(X509Certificate certificate, Func<Stream, Stream, GetFiberRwResult> init = null) where T : class
        {
            if (await RStream.WaitStreamInit())
            {
                var mergestream = new MergeStream(RStream as Stream, WStream as Stream);               
                var sslstream = new SslStream(mergestream, false);
                try
                {
                    await sslstream.AuthenticateAsServerAsync(certificate);
                }
                catch(Exception er)
                {
                    return (null,er.Message);
                }
                StartReceive();
                var fiber= new FiberRw<T>(this, RStream, WStream, MemoryPool, Encoding, IsLittleEndian, sslstream, sslstream, init: init);
                fibersslT = fiber;
                return (fiber,null);
            }
            else
                return (null,"not install");
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
            IsStartReceive = false;
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
            this.Reset_check();
            
        }

        public void Disconnect(bool dispose=false)
        {
            try
            {
                if (isInit)
                {
                    AcceptSocket?.Shutdown(System.Net.Sockets.SocketShutdown.Both);

                    if(dispose)
                        DisconnectIt?.Invoke(this);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception)
            {

            }
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

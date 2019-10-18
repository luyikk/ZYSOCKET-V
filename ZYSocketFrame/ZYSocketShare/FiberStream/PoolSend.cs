using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ZYSocket.Share
{
    public class PoolSend : ISend, IAsyncSend
    {
        private bool isAccpet = false;

        private SocketAsyncEventArgs? _accpet;

        private readonly SendSocketAsyncEventPool _sendPool;

        private bool IsThrowDisconnectException { get; }

        public PoolSend(bool isThrowSocketDisconnectException=false)
        {
            IsThrowDisconnectException = isThrowSocketDisconnectException;
            _sendPool = SendSocketAsyncEventPool.Shared;
        }

        public PoolSend(SocketAsyncEventArgs accpet)
        {
            _accpet = accpet;
            _sendPool = SendSocketAsyncEventPool.Shared;
            
        }

        public void SetAccpet(SocketAsyncEventArgs accpet)
        {
            isAccpet = true;
            _accpet = accpet;
        }

        public void SetConnect(SocketAsyncEventArgs connect)
        {
            isAccpet = false;
            _accpet = connect;
        }


        private bool TheSocketExceptionThrow(SocketException er)
        {
            if (IsThrowDisconnectException)
                return true;

            if (er.SocketErrorCode != SocketError.TimedOut &&
                    er.SocketErrorCode != SocketError.ConnectionReset &&
                    er.SocketErrorCode != SocketError.OperationAborted &&
                    er.SocketErrorCode != SocketError.ConnectionAborted &&
                    er.SocketErrorCode != SocketError.Shutdown &&
                    er.SocketErrorCode != SocketError.Interrupted &&
                    er.ErrorCode != 32)
                return true;

            return false;
        }
        

        public void Send(ArraySegment<byte> data)
        {
            Socket? socket;
            if (isAccpet)
                socket = _accpet?.AcceptSocket;
            else
                socket = _accpet?.ConnectSocket;
            

            if (socket != null)
            {               
                try
                {
                    socket.Send(data.Array, data.Offset, data.Count, SocketFlags.None);
                }
                catch (SocketException er)
                {
                    if(TheSocketExceptionThrow(er))
                            throw er;
                }
              

            }
        }

        public  void Send(byte[] data)
        {
            Socket? socket;
            if (isAccpet)
                socket = _accpet?.AcceptSocket;
            else
                socket = _accpet?.ConnectSocket;

            if (socket != null)
            {
                
                try
                {
                    socket.Send(data, 0, data.Length, SocketFlags.None);
                }
                catch (SocketException er)
                {
                    if (TheSocketExceptionThrow(er))
                        throw er;
                }
             

            }

    }

        public  void Send(IList<ArraySegment<byte>> data)
        {
            Socket? socket;
            if (isAccpet)
                socket = _accpet?.AcceptSocket;
            else
                socket = _accpet?.ConnectSocket;

            if (socket != null)
            {
               
                try
                {
                    socket.Send(data, SocketFlags.None);
                }
                catch (SocketException er)
                {
                    if (TheSocketExceptionThrow(er))
                        throw er;
                }
             


            }
        }

        public void Send(ReadOnlyMemory<byte> data)
        {

            Socket? socket;

            if (isAccpet)
                socket = _accpet?.AcceptSocket;
            else
                socket = _accpet?.ConnectSocket;

            if (socket != null)
            {

                try
                {
                    var array = data.GetArray();
                    socket.Send(array.Array, array.Offset, array.Count, SocketFlags.None);
                }
                catch (SocketException er)
                {
                    if (TheSocketExceptionThrow(er))
                        throw er;
                }

            }


        }


        public async Task<int> SendAsync(ArraySegment<byte> data)
        {
            Socket? socket;
            if (isAccpet)
                socket = _accpet?.AcceptSocket;
            else
                socket = _accpet?.ConnectSocket;

            if (socket != null)
            {
                var async = _sendPool.GetObject();
                async.SetBuffer(data.Array, data.Offset, data.Count);

                try
                {
                    var len = await async.SendSync(socket);
                    return len;
                }               
                finally
                {
                    _sendPool.ReleaseObject(async);
                }
               
            }
            else
                return 0;

        }

        public async Task<int> SendAsync(byte[] data)
        {
            Socket? socket;
            if (isAccpet)
                socket = _accpet?.AcceptSocket;
            else
                socket = _accpet?.ConnectSocket;

            if (socket != null)
            {
                var async = _sendPool.GetObject();
                async.SetBuffer(data, 0, data.Length);
                try
                {
                    var len = await async.SendSync(socket);
                    return len;
                }              
                finally
                {
                    _sendPool.ReleaseObject(async);
                }
              
            }
            else
                return 0;

        }

        public async Task<int> SendAsync(IList<ArraySegment<byte>> data)
        {
            Socket? socket;
            if (isAccpet)
                socket = _accpet?.AcceptSocket;
            else
                socket = _accpet?.ConnectSocket;

            if (socket != null)
            {
                var async = _sendPool.GetObject();
                async.BufferList = data;
                try
                {
                    var len = await async.SendSync(socket);
                    return len;
                }              
                finally
                {
                    _sendPool.ReleaseObject(async);
                }
              
            }
            else
                return 0;

        }

        public async Task<int> SendAsync(ReadOnlyMemory<byte> data)
        {
            Socket? socket;

            if (isAccpet)
                socket = _accpet?.AcceptSocket;
            else
                socket = _accpet?.ConnectSocket;

            if (socket != null)
            {
                var async = _sendPool.GetObject();

                var array = data.GetArray();
                async.SetBuffer(array.Array, array.Offset, array.Count);
                try
                {
                    var len = await async.SendSync(socket);
                    return len;
                }           
                finally
                {
                    _sendPool.ReleaseObject(async);
                }

              
            }
            else
                return 0;

        }

    }
}

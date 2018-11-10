using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ZYSocket.Server
{
    public class PoolSend : ISend, IAsyncSend
    {
        private SocketAsyncEventArgs _accpet;

        private readonly SendSocketAsyncEventPool _sendPool;

        public PoolSend()
        {
            _sendPool = SendSocketAsyncEventPool.Shared;
        }

        public PoolSend(SocketAsyncEventArgs accpet)
        {
            _accpet = accpet;
            _sendPool = SendSocketAsyncEventPool.Shared;
            
        }

        public void SetAccpet(SocketAsyncEventArgs accpet)
        {
            _accpet = accpet;
        }

        

        public void Send(ArraySegment<byte> data)
        {
            if (_accpet.AcceptSocket != null)
            {
               
                try
                {
                    _accpet.AcceptSocket.Send(data.Array, data.Offset, data.Count, SocketFlags.None);
                }
                catch (SocketException er)
                {
                    if (er.SocketErrorCode != SocketError.TimedOut && er.SocketErrorCode != SocketError.ConnectionReset && er.SocketErrorCode != SocketError.OperationAborted)
                        throw er;
                }

            }        
        }

        public  void Send(byte[] data)
        {
            if (_accpet.AcceptSocket != null)
            {
                
                try
                {
                    _accpet.AcceptSocket.Send(data, 0, data.Length, SocketFlags.None);
                }
                catch (SocketException er)
                {
                    if (er.SocketErrorCode != SocketError.TimedOut && er.SocketErrorCode != SocketError.ConnectionReset && er.SocketErrorCode != SocketError.OperationAborted)
                        throw er;
                }

            }

    }

        public  void Send(IList<ArraySegment<byte>> data)
        {
            if (_accpet.AcceptSocket != null)
            {
               
                try
                {
                    _accpet.AcceptSocket.Send(data, SocketFlags.None);
                }
                catch (SocketException er)
                {
                    if (er.SocketErrorCode != SocketError.TimedOut && er.SocketErrorCode != SocketError.ConnectionReset && er.SocketErrorCode != SocketError.OperationAborted)
                        throw er;
                }


            }
        }

        public  void Send(ReadOnlyMemory<byte> data)
        {
            if (_accpet.AcceptSocket != null)
            {
               
                try
                {
                    var array = data.GetArray();
                    _accpet.AcceptSocket.Send(array.Array, array.Offset, array.Count, SocketFlags.None);
                }
                catch (SocketException er)
                {
                    if (er.SocketErrorCode != SocketError.ConnectionReset && er.SocketErrorCode != SocketError.OperationAborted)
                        throw er;
                }

            }

    }


        public async ValueTask<int> SendAsync(ArraySegment<byte> data)
        {
            if (_accpet.AcceptSocket != null)
            {
                var async = _sendPool.GetObject();
                async.SetBuffer(data.Array, data.Offset, data.Count);

                try
                {
                    var len = await async.SendSync(_accpet.AcceptSocket);
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

        public async ValueTask<int> SendAsync(byte[] data)
        {
            if (_accpet.AcceptSocket != null)
            {
                var async = _sendPool.GetObject();
                async.SetBuffer(data, 0, data.Length);
                try
                {
                    var len = await async.SendSync(_accpet.AcceptSocket);
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

        public async ValueTask<int> SendAsync(IList<ArraySegment<byte>> data)
        {
            if (_accpet.AcceptSocket != null)
            {
                var async = _sendPool.GetObject();
                async.BufferList = data;
                try
                {
                    var len = await async.SendSync(_accpet.AcceptSocket);
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

        public async ValueTask<int> SendAsync(ReadOnlyMemory<byte> data)
        {
            if (_accpet.AcceptSocket != null)
            {
                var async = _sendPool.GetObject();

                var array = data.GetArray();
                async.SetBuffer(array.Array, array.Offset, array.Count);
                try
                {
                    var len = await async.SendSync(_accpet.AcceptSocket);
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

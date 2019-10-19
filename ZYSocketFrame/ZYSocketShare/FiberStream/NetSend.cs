using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ZYSocket.Share
{
    public class NetSend : ISend,IAsyncSend
    {
        private bool isAccpet = false;

        private SocketAsyncEventArgs? _accpet;

        private bool IsThrowDisconnectException { get; }

        public NetSend(bool isThrowSocketDisconnectException = false)
        {
            IsThrowDisconnectException = isThrowSocketDisconnectException;            
        }

        public NetSend(SocketAsyncEventArgs accpet)
        {
            _accpet = accpet;          
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

        public bool TheSocketExceptionThrow(SocketException er)
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


            if (socket != null && socket.Connected)
            {
                try
                {
                    socket.Send(data.Array, data.Offset, data.Count, SocketFlags.None);
                }
                catch (SocketException er)
                {
                    if (TheSocketExceptionThrow(er))
                        throw er;
                }


            }
        }

        public void Send(byte[] data)
        {
            Socket? socket;
            if (isAccpet)
                socket = _accpet?.AcceptSocket;
            else
                socket = _accpet?.ConnectSocket;

            if (socket != null && socket.Connected)
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

        public void Send(IList<ArraySegment<byte>> data)
        {
            Socket? socket;
            if (isAccpet)
                socket = _accpet?.AcceptSocket;
            else
                socket = _accpet?.ConnectSocket;

            if (socket != null && socket.Connected)
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

            if (socket != null && socket.Connected)
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

        public  Task<int> SendAsync(ArraySegment<byte> data)
        {
            Socket? socket;
            if (isAccpet)
                socket = _accpet?.AcceptSocket;
            else
                socket = _accpet?.ConnectSocket;

            if (socket != null && socket.Connected)            
                 return  socket.SendAsync(data,SocketFlags.None);            
            else
                return Task.FromResult(0);
        }

        public Task<int> SendAsync(byte[] data)
        {
            Socket? socket;
            if (isAccpet)
                socket = _accpet?.AcceptSocket;
            else
                socket = _accpet?.ConnectSocket;

            if (socket != null && socket.Connected)
                return socket.SendAsync(data, SocketFlags.None);
            else
                return Task.FromResult(0);
        }

        public Task<int> SendAsync(IList<ArraySegment<byte>> data)
        {
            Socket? socket;
            if (isAccpet)
                socket = _accpet?.AcceptSocket;
            else
                socket = _accpet?.ConnectSocket;

            if (socket != null && socket.Connected)
                return socket.SendAsync(data, SocketFlags.None);
            else
                return Task.FromResult(0);
        }

        public ValueTask<int> SendAsync(ReadOnlyMemory<byte> data)
        {
            Socket? socket;
            if (isAccpet)
                socket = _accpet?.AcceptSocket;
            else
                socket = _accpet?.ConnectSocket;

            if (socket != null && socket.Connected)
                return socket.SendAsync(data, SocketFlags.None);
            else
                return new ValueTask<int>(0);
        }
    }
}

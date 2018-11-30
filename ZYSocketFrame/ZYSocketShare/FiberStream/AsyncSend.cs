using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ZYSocket.Share
{
    public class AsyncSend 
    {
        private readonly int bufferlength = 4096;

        private readonly SocketAsyncEventArgs _send;

        private readonly ConcurrentQueue<byte[]> BufferQueue;

        private readonly List<ArraySegment<byte>> send_buffer;

        private readonly SocketAsyncEventArgs _accept;
        private Socket _sock { get => _accept?.AcceptSocket; }

        protected int BufferLenght { get; set; } = -1;

        private int SendIng;

        public AsyncSend(SocketAsyncEventArgs accept, int bufferLength=0)
        {
            _accept = accept;
            this.BufferLenght = bufferLength;
            SendIng = 0;
            BufferQueue = new ConcurrentQueue<byte[]>();
            _send = new SocketAsyncEventArgs();
            _send.Completed += Completed;
            send_buffer = new List<ArraySegment<byte>>();
        }

        private void Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Send:
                    {
                        BeginSend(e);
                    }
                    break;

            }
        }

        private void BeginSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Free();
                return;
            }
            else
            {
                send_buffer.Clear();
                Interlocked.Exchange(ref SendIng, 0);
                if (BufferQueue.Count > 0)
                    SendComputer();
            }

        }

        private void Free()
        {
            _send.BufferList = null;
            for (int i = 0; i < BufferQueue.Count; i++)
                BufferQueue.TryDequeue(out byte[] tmp);

            send_buffer.Clear();
        }

        private bool InitData()
        {
            var list = PopBufferList();

            if (list.Count > 0)
            {
                _send.BufferList = list;

                return true;
            }
            else
                return false;

        }



        private List<ArraySegment<byte>> PopBufferList()
        {
            List<ArraySegment<byte>> buffer = send_buffer;


            int length = 0;
            int offset = 0;

            for (int c=0 ;c<bufferlength ; )
            {
                if (BufferQueue.TryDequeue(out byte[] data))
                {
                    if (BufferLenght <= 0)
                    {
                        ArraySegment<byte> vs = new ArraySegment<byte>(data, 0, data.Length);
                        buffer.Add(vs);
                    }
                    else
                    {
                        do
                        {
                            var have = data.Length - offset;
                            if (BufferLenght > have)
                                length = have;
                            else
                                length = BufferLenght;

                            ArraySegment<byte> vs = new ArraySegment<byte>(data, offset, length);
                            buffer.Add(vs);
                            offset += length;

                        } while (offset < data.Length);

                    }

                    c += data.Length;                 
                }
                else
                    break;
            }

            return buffer;
        }



        public bool Send(byte[] data)
        {

            if (_sock == null)
                return false;
            if (data == null)
                return false;

            BufferQueue.Enqueue(data);

            return SendComputer();

        }



        private bool SendComputer()
        {
            if (Interlocked.CompareExchange(ref SendIng, 1, 0) == 0)
                if (InitData())
                {
                    SendAsync();
                    return true;
                }
                else
                    Interlocked.Exchange(ref SendIng, 0);

            return false;
        }

        private void SendAsync()
        {
            try
            {

                if (!_sock.SendAsync(_send))
                {
                    BeginSend(_send);
                }
            }
            catch (ObjectDisposedException)
            {
                Free();               
            }

        }

    }
}

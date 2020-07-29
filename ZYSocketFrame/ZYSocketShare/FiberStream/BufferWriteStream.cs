﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ZYSocket.Share;

namespace ZYSocket.FiberStream
{
    public class BufferWriteStream : Stream, IFiberWriteStream
    {

        public  const int BufferBlockSize = 4096;

        public const int checknum = 12;

        private long _len = 0;

        private long _postion = 0;

        private int _index = 0;

        private int _tmp_postion = 0;

        public static LengthSize LenType { get; set; } = LengthSize.Int32;

        private readonly List<ArraySegment<byte>> DataSegment;
        private readonly List<IMemoryOwner<byte>> MemoryOwners;

        private readonly MemoryPool<byte> MemoryPool;

        private readonly ISend Send;

        private readonly IAsyncSend AsyncSend;

        private readonly byte[] numbytes = new byte[8];
        public  byte[] Numericbytes { get => numbytes; }


        public long CurrentSegmentLen
        {
            get {

                long len = 0;

                foreach (var item in DataSegment)
                {
                    len += item.Count;
                }

                return len;
            }
        }


        public BufferWriteStream(MemoryPool<byte> memoryPool,ISend send,IAsyncSend asyncSend)
        {
            this.MemoryPool = memoryPool;
            Send = send;
            AsyncSend = asyncSend;
            DataSegment = new List<ArraySegment<byte>>();
            MemoryOwners = new List<IMemoryOwner<byte>>();
        }



        public override void Close()
        {
            Reset();
            DataSegment.Clear();           
            for (int i = 0; i < MemoryOwners.Count; i++)
            {
                var item = MemoryOwners[i];
                item?.Dispose();
            }
            MemoryOwners.Clear();
            base.Close();
        }

        public override bool CanRead => false;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _len;

        public override long Position { get => _postion; set => set_current_tmp_postion(value); }

    

        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();       

        public override long Seek(long offset, SeekOrigin origin)=> 
            throw new NotImplementedException();


        public override void Flush()
        {
            if (_len == 0)
                return;

            int isfull = (int)(_len % BufferBlockSize);

            int segment_ptr = (int)(_len >> checknum);

            if (isfull == 0)
                if (segment_ptr > 0)
                    segment_ptr -= 1;


            if (isfull == 0)
            {
                Send.Send(DataSegment.GetRange(0, segment_ptr + 1));
            }
            else
            {
                if (segment_ptr > 0)
                {
                    var list = DataSegment.GetRange(0, segment_ptr);
                    list.Add(DataSegment[segment_ptr].AsMemory().Slice(0, isfull).GetArray());
                    Send.Send(list);
                }
                else
                {
                    var array = DataSegment[segment_ptr].AsMemory().Slice(0, isfull);
                    Send.Send(array);
                }
            }


            Reset();

        }

        
        
      

        public override async Task FlushAsync(CancellationToken cancellationToken=default)
        {
            if (_len == 0)
                return;

            int isfull = (int)(_len % BufferBlockSize);

            int segment_ptr = (int)(_len >> checknum);

            if (isfull == 0)
                if (segment_ptr > 0)
                    segment_ptr -= 1;

         
            try
            {
                if (isfull == 0)
                {
                    await AsyncSend.SendAsync(DataSegment.GetRange(0, segment_ptr + 1));
                }
                else
                {
                    if (segment_ptr > 0)
                    {
                        var list = DataSegment.GetRange(0, segment_ptr);
                        list.Add(DataSegment[segment_ptr].AsMemory().Slice(0, isfull).GetArray());
                        await AsyncSend.SendAsync(list);
                    }
                    else
                    {
                        var array = DataSegment[segment_ptr].AsMemory().Slice(0, isfull);
                        await AsyncSend.SendAsync(array);
                    }
                }
            }
            catch (System.Net.Sockets.SocketException er)
            {
                if (AsyncSend.TheSocketExceptionThrow(er))
                    throw er;
            }

            Reset();

            return;
        }

        public override void SetLength(long value)
        {
            long needs = value - CurrentSegmentLen;
            if (needs > 0)
            {
                InitSegment(needs);
                _len = value;
                set_current_tmp_postion(_len);
            }
            else if(needs<0)
            {
                var x = Math.Abs(needs);
                int v =(int) (x >>checknum);
                
                if(v>0)
                {
                    var start = DataSegment.Count - v;
                    DataSegment.RemoveRange(start, v);
                    var remove= MemoryOwners.GetRange(start, v);
                    MemoryOwners.RemoveRange(start, v);
                    foreach (var item in remove)
                    {
                        item.Dispose();
                    }
                    remove.Clear();
                    set_current_tmp_postion(value);
                    _len = value;
                }
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int countp = count;
            int offsetp = offset;

            while (countp > 0)
            {
                int w = copy_to_block(buffer, offsetp, countp);

                countp -= w;
                offsetp += w;
            }         
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken=default)
        {           
            int countp = count;
            int offsetp = offset;

            while (countp > 0)
            {
                int w = copy_to_block(buffer, offsetp, countp);
                countp -= w;
                offsetp += w;

                if (cancellationToken.IsCancellationRequested)
                    break;
            }
          
            await FlushAsync();
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var task = WriteAsync(buffer, offset, count);
            return  TaskToApm.Begin(task, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        => TaskToApm.End(asyncResult);

        private unsafe int copy_to_block(byte[] buffer,int offset,int count)
        {

            //check do you need add segment
            check_datasegment();

            //get current segment
            var array = get_array_by_postion();
            
            //get the segment have size,will copy it to len
            var len = array.Count - _tmp_postion;
            if (len > count)
                len = count;

            if (len > 16)
            {
                var copy_postion = _tmp_postion + array.Offset;

                fixed (byte* target = &array.Array[copy_postion])
                fixed (byte* source = &buffer[offset])
                {
                    Buffer.MemoryCopy(source, target, len, len);                    

                    _postion = _postion + len;
                    _index = (int)(_postion >> checknum);
                    _tmp_postion = (int)(_postion % BufferBlockSize);

                    if(_postion>_len)
                        _len = _postion;
                }

                return len;
            }
            else
            {
                int tmp_tmp_index = _tmp_postion + array.Offset;
                for (int si = offset; si < len + offset; si++)
                {
                    array.Array[tmp_tmp_index] = buffer[si];
                    tmp_tmp_index++;
                }

                _postion = _postion + len;
                _index = (int)(_postion>>checknum);
                _tmp_postion = (int)(_postion % BufferBlockSize);

                if (_postion > _len)
                    _len = _postion;

                return len;
            }

        }

        private void check_datasegment()
        {
            var current_segment_count = DataSegment.Count;

            if (current_segment_count == 0)
                InitSegment();
            else
            {
                
                if(_tmp_postion== BufferBlockSize)
                    InitSegment();
                else if(_index== current_segment_count)
                    InitSegment();
            }
        }

        private ArraySegment<byte> get_array_by_postion()
            => DataSegment[_index];

        private void set_current_tmp_postion(long postion)
        {         
            _postion = postion = postion > _len ? _len : postion;
            _index =(int)(postion >>checknum);
            _tmp_postion = (int)(postion % BufferBlockSize);
        }

        private void InitSegment(long need = BufferBlockSize)
        {
            long p = need;

            while (p > 0)
            {
                var memoryOwner = MemoryPool.Rent(BufferBlockSize);
                DataSegment.Add(memoryOwner.Memory.GetArray());
                MemoryOwners.Add(memoryOwner);
                p -= 4096;
            }
        }

        private void Reset()
        => _len = _postion = _index = _tmp_postion = 0;
        
    }
}

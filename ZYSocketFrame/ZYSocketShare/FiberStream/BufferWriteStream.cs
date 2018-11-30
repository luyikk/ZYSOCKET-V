using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ZYSocket.Share;

namespace ZYSocket.FiberStream
{
    public class BufferWriteStream : Stream, IFiberWriteStream
    {

        public  const int BufferBlockSize = 4096;

        private long _len = 0;

        private long _postion = 0;

        private int _index = 0;

        private int _tmp_postion = 0;

        public static LengthLen LenType { get; set; } = LengthLen.Int32;

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
            foreach (var item in MemoryOwners)
            {
                item.Dispose();
            }
            MemoryOwners.Clear();
            base.Close();
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _len;

        public override long Position { get => _postion; set => set_current_tmp_postion(value); }

    

        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();       

        public override long Seek(long offset, SeekOrigin origin)=> throw new NotImplementedException();


        public override void Flush()
        {
            int isfull =(int)(_len % BufferBlockSize);

            int segment_ptr = (int)_len / BufferBlockSize;

            if (isfull == 0)
                if (segment_ptr > 0)
                    segment_ptr -= 1;

            for (int i = 0; i < segment_ptr; i++)
            {
                var data = DataSegment[i];
                Send.Send(data);
            }

            if (isfull != 0)
            {
                var array = DataSegment[segment_ptr].AsMemory().Slice(0, isfull).GetArray();
                Send.Send(array);
            }
            else
            {
                var data = DataSegment[segment_ptr];
                Send.Send(data);
            }          


            Reset();
        }

        public async ValueTask<int> AwaitFlush()
        {
            int isfull = (int)(_len % BufferBlockSize);

            int segment_ptr = (int)_len / BufferBlockSize;

            if (isfull == 0)
                if (segment_ptr > 0)
                    segment_ptr -= 1;

            int sendlen = 0;

            for (int i = 0; i < segment_ptr; i++)
            {
                var data = DataSegment[i];
                sendlen+=await AsyncSend.SendAsync(data);
            }

            if (isfull != 0)
            {
                var array = DataSegment[segment_ptr].AsMemory().Slice(0, isfull).GetArray();
                sendlen += await AsyncSend.SendAsync(array);
            }
            else
            {
                var data = DataSegment[segment_ptr];
                sendlen += await AsyncSend.SendAsync(data);
            }


            Reset();

            return sendlen;
        }


        public void Reset()
        {           
            _len = 0;
            set_current_tmp_postion(0);
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
                int v =(int) x / BufferBlockSize;
                
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
                    _index = (int)(_postion / BufferBlockSize);
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
                _index = (int)(_postion / BufferBlockSize);
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
        {
            return DataSegment[_index];
        }


        private void set_current_tmp_postion(long postion)
        {          
            if (postion > _len)
                postion = _len;
            _postion = postion;          
            _index =(int)(postion / BufferBlockSize);
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

      
    }
}

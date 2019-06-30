using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ZYSocket.FiberStream;

namespace ZYSocket
{



    public struct WriteBytes:IDisposable,IBufferWrite
    {

        public IFiberRw FiberRw { get;  }

        public LengthLen LenType { get; set; }

        private readonly bool IsLittleEndian;

        private readonly byte[] Numericbytes;

        private readonly Stream StreamWriteFormat;
        private readonly IFiberWriteStream FiberWriteStream;
        private readonly MemoryStream StreamWrite;
        private readonly IMemoryOwner<byte> memory;

       

        public WriteBytes(IFiberRw fiberRw)
        {
          

            FiberRw = fiberRw;
            LenType = LengthLen.None;
            IsLittleEndian = FiberRw.IsLittleEndian;
            Numericbytes = FiberRw.FiberWriteStream.Numericbytes;
            StreamWriteFormat = FiberRw.StreamWriteFormat;
            FiberWriteStream = FiberRw.FiberWriteStream;
            StreamWrite = new MemoryStream(256);
            memory = null;
        }

        public WriteBytes(IFiberRw fiberRw,int capacity)
        {
           
            FiberRw = fiberRw;
            LenType = LengthLen.None;
            IsLittleEndian = FiberRw.IsLittleEndian;
            Numericbytes = FiberRw.FiberWriteStream.Numericbytes;
            StreamWriteFormat = FiberRw.StreamWriteFormat;
            FiberWriteStream = FiberRw.FiberWriteStream;           
            memory=FiberRw.GetMemory(capacity);
            var buffer = memory.Memory.GetArray();
            StreamWrite = new MemoryStream(buffer.Array,buffer.Offset,buffer.Count, true, true);
            StreamWrite.SetLength(0);
        }

        public WriteBytes(IFiberRw fiberRw, ref Memory<byte> memory)
        {

            FiberRw = fiberRw;
            LenType = LengthLen.None;
            IsLittleEndian = FiberRw.IsLittleEndian;
            Numericbytes = FiberRw.FiberWriteStream.Numericbytes;
            StreamWriteFormat = FiberRw.StreamWriteFormat;
            FiberWriteStream = FiberRw.FiberWriteStream;         
            var buffer = memory.GetArray();
            StreamWrite = new MemoryStream(buffer.Array, buffer.Offset, buffer.Count, true, true);
            StreamWrite.SetLength(0);
            this.memory = null;
        }

        public Stream Stream => StreamWrite;

        public void Dispose()
        {
            StreamWrite.Dispose();
            memory?.Dispose();
        }

        public void Reset()
        {
            StreamWrite.SetLength(0);
            StreamWrite.Position = 0;
        }


        public void WriteLen(LengthLen headLenType = LengthLen.Int32)
        {

            LenType = headLenType;

            switch (LenType)
            {
                case LengthLen.Byte:
                    {
                        StreamWrite.Write(Numericbytes, 0, 1);
                       
                    }
                    break;
                case LengthLen.Int16:
                    {
                        StreamWrite.Write(Numericbytes, 0, 2);
                    }
                    break;
                case LengthLen.Int32:
                    {
                        StreamWrite.Write(Numericbytes, 0, 4);
                    }
                    break;
                case LengthLen.Int64:
                    {
                        StreamWrite.Write(Numericbytes, 0, 8);
                    }
                    break;
            }
        }

        public void Cmd(int cmd)
        {
            Write(cmd);
        }
        
        public void Write(ArraySegment<byte> data)
        {
            StreamWrite.Write(data.Array, data.Offset, data.Count);
        }

        public void Write(byte[] data, bool wlen = true)
        {
            if (wlen)
                Write(data.Length);

            StreamWrite.Write(data, 0, data.Length);
        }

        public void Write(Memory<byte> data, int offset, int count)
        {
            var array = data.GetArray();

            StreamWrite.Write(array.Array, array.Offset + offset, count);
        }

        public void Write(byte[] data, int offset, int count)
        {
            StreamWrite.Write(data, offset, count);           
        }


        public void Write(Memory<byte> data, bool wlen = true)
        {
            var array = data.GetArray();

            if (wlen)
                Write(array.Count);

            StreamWrite.Write(array.Array, array.Offset, array.Count);
        }

        public void Write(ResultByMemoryOwner<Memory<byte>> data, bool wlen = true)
        {
            if (!data.IsInit)
                throw new ObjectDisposedException("data is not init");

            Write(data.Value, wlen);
        }

        public void Write(ResultByMemoryOwner<Memory<byte>> data, int offset, int count)
        {
            if (!data.IsInit)
                throw new ObjectDisposedException("data is not init");

            Write(data.Value, offset, count);
        }

        public void Write(string data,bool wlen=true)
        {
            byte[] bytes = FiberRw.Encoding.GetBytes(data);
            Write(bytes,wlen);
        }

        public void Write(object obj) => Write(FiberRw.ObjFormat.Serialize(obj));     


        public void Write(byte data)
        {
            StreamWrite.WriteByte(data);
        }


        public unsafe void Write(short data)
        {
            if (IsLittleEndian)
                data = BinaryPrimitives.ReverseEndianness(data);

            fixed (byte* numRef = &Numericbytes[0])
            {
                *((short*)numRef) = data;
                Write(Numericbytes, 0, 2);
            }
        }


        public unsafe void Write(int data)
        {
            if (IsLittleEndian)
                data = BinaryPrimitives.ReverseEndianness(data);        

            fixed (byte* numRef = &Numericbytes[0])
            {
                *((int*)numRef) = data;
                Write(Numericbytes, 0, 4);
            }
        }

        public unsafe void Write(long data)
        {
            if (IsLittleEndian)
                data = BinaryPrimitives.ReverseEndianness(data);

            fixed (byte* numRef = &Numericbytes[0])
            {
                *((long*)numRef) = data;
                Write(Numericbytes, 0, 8);
            }
        }

        public unsafe void Write(ushort data)
        {
            if (IsLittleEndian)
                data = BinaryPrimitives.ReverseEndianness(data);

            fixed (byte* numRef = &Numericbytes[0])
            {
                *((ushort*)numRef) = data;
                Write(Numericbytes, 0, 2);
            }
        }
        public unsafe void Write(uint data)
        {
            if (IsLittleEndian)
                data = BinaryPrimitives.ReverseEndianness(data);

            fixed (byte* numRef = &Numericbytes[0])
            {
                *((uint*)numRef) = data;
                Write(Numericbytes, 0, 4);
            }
        }

        public unsafe void Write(ulong data)
        {
            if (IsLittleEndian)
                data = BinaryPrimitives.ReverseEndianness(data);


            fixed (byte* numRef = &Numericbytes[0])
            {
                *((ulong*)numRef) = data;
                Write(Numericbytes, 0, 8);
            }
        }


        public void Write(double data)
        {
            unsafe
            {
                ulong format = *(((ulong*)&data));
                Write(format);
            }
        }

        public void Write(float data)
        {
            unsafe
            {
                uint format = *(((uint*)&data));
                Write(format);
            };
        }


        public void Write(bool data)
        {
            Write(data ? ((byte)1) : ((byte)0));
        }

        public void Write(bool? data)
        {
            if (!data.HasValue)
                throw new ArgumentNullException("data");

            Write(data.Value);
        }

        public void Write(byte? data)
        {
            if (!data.HasValue)
                throw new ArgumentNullException("data");

            Write(data.Value);
        }

        public void Write(short? data)
        {
            if (!data.HasValue)
                throw new ArgumentNullException("data");

            Write(data.Value);
        }

        public void Write(int? data)
        {
            if (!data.HasValue)
                throw new ArgumentNullException("data");

            Write(data.Value);
        }

        public void Write(long? data)
        {
            if (!data.HasValue)
                throw new ArgumentNullException("data");

            Write(data.Value);
        }

        public void Write(ushort? data)
        {
            if (!data.HasValue)
                throw new ArgumentNullException("data");

            Write(data.Value);
        }

        public void Write(uint? data)
        {
            if (!data.HasValue)
                throw new ArgumentNullException("data");

            Write(data.Value);
        }

        public void Write(ulong? data)
        {
            if (!data.HasValue)
                throw new ArgumentNullException("data");

            Write(data.Value);
        }

        public void Write(double? data)
        {
            if (!data.HasValue)
                throw new ArgumentNullException("data");

            Write(data.Value);
        }

        public void Write(float? data)
        {
            if (!data.HasValue)
                throw new ArgumentNullException("data");
            Write(data.Value);
        }


        public (int, int) Allocate(int size, byte data = 0)
        {
            var postion =(int) StreamWrite.Position;

            for (int i = 0; i < size; i++)
                Write(data);

            return (postion, size);
        }

        public Task<int> Flush()
        {
           
            switch (LenType)
            {
                case LengthLen.Byte:
                    {
                        StreamWrite.Position = 0;
                        var lenbytes = (byte)StreamWrite.Length;
                        Write(lenbytes);
                    }
                    break;
                case LengthLen.Int16:
                    {
                        StreamWrite.Position = 0;
                        Write((ushort)StreamWrite.Length);
                    }
                    break;
                case LengthLen.Int32:
                    {
                        StreamWrite.Position = 0;
                        Write((uint)StreamWrite.Length);
                    }
                    break;
                case LengthLen.Int64:
                    {
                        StreamWrite.Position = 0;
                        Write((ulong)StreamWrite.Length);
                    }
                    break;

            }

            if (StreamWrite.TryGetBuffer(out ArraySegment<byte> buffer))
            {
                StreamWriteFormat.Write(buffer.Array, buffer.Offset, buffer.Count);
                StreamWriteFormat.Flush();
            }

            if (FiberWriteStream.Length > 0)
                return FiberWriteStream.AwaitFlush();
            else
                return Task.FromResult(0);
        }

      
    }
}

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ZYSocket.FiberStream;

namespace ZYSocket
{



    public struct WriteBytes:IDisposable
    {

        public IFiberRw FiberRw { get; private set; }

        public LengthLen LenType { get; set; }

        private readonly bool IsLittleEndian;

        private readonly byte[] Numericbytes;

        private readonly Stream StreamWriteFormat;
        private readonly IFiberWriteStream FiberWriteStream;

        private readonly MemoryStream StreamWrite;

        public WriteBytes(IFiberRw fiberRw)
        {
            FiberRw = fiberRw;
            LenType = LengthLen.None;
            IsLittleEndian = FiberRw.IsLittleEndian;
            Numericbytes = FiberRw.FiberWriteStream.Numericbytes;
            StreamWriteFormat = FiberRw.StreamWriteFormat;
            FiberWriteStream = FiberRw.FiberWriteStream;
            StreamWrite = new MemoryStream();
        }

        public void Dispose()
        {
            StreamWrite.Dispose();
        }

        public void Reset()
        {
            StreamWrite.SetLength(0);
            StreamWrite.Position = 0;
        }


        public void WriteLen(LengthLen headLenType = LengthLen.Int32)
        {

            if (StreamWrite.Length > 0)
                throw new System.IO.InvalidDataException("the stream not null");


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

        public void Write(Memory<byte> data, bool wlen = true)
        {
            var array = data.GetArray();

            if (wlen)
                Write(array.Count);

            StreamWrite.Write(array.Array, array.Offset, array.Count);
        }


        public void Write(string data)
        {
            byte[] bytes = FiberRw.Encoding.GetBytes(data);
            Write(bytes);
        }


        public void Write(object obj)
        {

            var bkpostion = StreamWrite.Position;
            Write(0);
            ProtoBuf.Meta.RuntimeTypeModel.Default.Serialize(StreamWrite, obj);
            var lastpostion = StreamWrite.Position;
            var len = lastpostion - bkpostion - 4;
            StreamWrite.Position = bkpostion;
            Write((int)len);
            StreamWrite.Position = lastpostion;

        }



        public void Write(byte data)
        {
            StreamWrite.WriteByte(data);
        }

        public void Write(short data)
        {
            if (IsLittleEndian)
                data = BinaryPrimitives.ReverseEndianness(data);

            Numericbytes[0] = (byte)data;
            Numericbytes[1] = (byte)(data >> 8);

            Write(Numericbytes, 0, 2);
        }
        public void Write(int data)
        {
            if (IsLittleEndian)
                data = BinaryPrimitives.ReverseEndianness(data);

            Numericbytes[0] = (byte)data;
            Numericbytes[1] = (byte)(data >> 8);
            Numericbytes[2] = (byte)(data >> 0x10);
            Numericbytes[3] = (byte)(data >> 0x18);

            Write(Numericbytes, 0, 4);
        }
        public void Write(long data)
        {
            if (IsLittleEndian)
                data = BinaryPrimitives.ReverseEndianness(data);


            Numericbytes[0] = (byte)data;
            Numericbytes[1] = (byte)(data >> 8);
            Numericbytes[2] = (byte)(data >> 0x10);
            Numericbytes[3] = (byte)(data >> 0x18);
            Numericbytes[4] = (byte)(data >> 0x20);
            Numericbytes[5] = (byte)(data >> 0x28);
            Numericbytes[6] = (byte)(data >> 0x30);
            Numericbytes[7] = (byte)(data >> 0x38);

            Write(Numericbytes, 0, 8);
        }

        public void Write(ushort data)
        {
            if (IsLittleEndian)
                data = BinaryPrimitives.ReverseEndianness(data);

            Numericbytes[0] = (byte)data;
            Numericbytes[1] = (byte)(data >> 8);
            Write(Numericbytes, 0, 2);
        }
        public void Write(uint data)
        {
            if (IsLittleEndian)
                data = BinaryPrimitives.ReverseEndianness(data);

            Numericbytes[0] = (byte)data;
            Numericbytes[1] = (byte)(data >> 8);
            Numericbytes[2] = (byte)(data >> 0x10);
            Numericbytes[3] = (byte)(data >> 0x18);

            Write(Numericbytes, 0, 4);
        }
        public void Write(ulong data)
        {
            if (IsLittleEndian)
                data = BinaryPrimitives.ReverseEndianness(data);


            Numericbytes[0] = (byte)data;
            Numericbytes[1] = (byte)(data >> 8);
            Numericbytes[2] = (byte)(data >> 0x10);
            Numericbytes[3] = (byte)(data >> 0x18);
            Numericbytes[4] = (byte)(data >> 0x20);
            Numericbytes[5] = (byte)(data >> 0x28);
            Numericbytes[6] = (byte)(data >> 0x30);
            Numericbytes[7] = (byte)(data >> 0x38);

            Write(Numericbytes, 0, 8);
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



            byte[] data = StreamWrite.ToArray();
            StreamWriteFormat.Write(data, 0, data.Length);
            StreamWriteFormat.Flush();
            return  FiberWriteStream.AwaitFlush();
        }


        public async ValueTask<int> AwaitFlush()
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


            byte[] data = StreamWrite.ToArray();
            StreamWriteFormat.Write(data, 0, data.Length);
            StreamWriteFormat.Flush();
            if (FiberWriteStream.Length > 0)
                return await FiberWriteStream.AwaitFlush();
            else
                return 0;
        }

       
    }
}

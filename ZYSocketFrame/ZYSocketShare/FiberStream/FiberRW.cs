using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ZYSocket.FiberStream
{
    public class FiberRw<T> : IDisposable,IFiberRw<T> where T:class
    {
        private readonly MemoryPool<byte> memoryPool;
        public  MemoryPool<byte> MemoryPool { get => memoryPool; }

        private readonly bool isinit;
        public bool IsInit { get => isinit; }

        public Encoding Encoding { get; private set; }

        private readonly bool isLittleEndian;
        public bool IsLittleEndian { get => isLittleEndian; }

        private readonly IFiberReadStream fiberReadStream;
        public IFiberReadStream FiberReadStream { get => fiberReadStream; }


        private readonly Stream streamReadFormat;
        public Stream StreamReadFormat { get => streamReadFormat; }

        private readonly IFiberWriteStream fiberWriteStream;

        public IFiberWriteStream FiberWriteStream { get => fiberWriteStream; }

        private readonly Stream streamWriteFormat;
        public Stream StreamWriteFormat { get => streamWriteFormat; }
        public ISockAsyncEvent Async { get; private set; }       
        public T UserToken { get; set; }

        private readonly byte[] read_Numericbytes;
        private readonly byte[] write_Numericbytes;

        public FiberRw(ISockAsyncEvent async,IFiberReadStream fiberRStream, IFiberWriteStream fiberWStream,  MemoryPool<byte> memoryPool, Encoding encoding,bool isLittleEndian=false, Stream inputStream=null,Stream outputStream=null, Func<Stream,Stream,GetFiberRwResult> init=null)
        {
           
            if (init != null)
            {
                var result = init(inputStream==null?fiberRStream as Stream:inputStream, outputStream==null? fiberWStream as Stream:outputStream);

                if(result!= null)
                {
                    streamReadFormat = result.Input;
                    streamWriteFormat = result.Output;

                }
                else
                {
                    streamReadFormat = inputStream == null ? fiberRStream as Stream : inputStream;
                    streamWriteFormat = outputStream == null ? fiberWStream as Stream : outputStream;
                }
            }
            else
            {
                streamReadFormat = inputStream == null ? fiberRStream as Stream : inputStream;
                streamWriteFormat = outputStream == null ? fiberWStream as Stream : outputStream;
            }

            UserToken = null;
            this.Async = async;
            fiberReadStream = fiberRStream;
            fiberWriteStream = fiberWStream;
            this.Encoding = encoding;
            this.memoryPool = memoryPool;
            this.isLittleEndian = isLittleEndian;           
            read_Numericbytes = fiberReadStream.Numericbytes;
            write_Numericbytes = fiberWriteStream.Numericbytes;
            isinit = true;
        }

        public void Dispose()
        {
            try
            {
                streamReadFormat?.Dispose();
            }
            catch (ObjectDisposedException) { }
            
            try
            {
                streamWriteFormat?.Dispose();
            }
            catch (ObjectDisposedException) { }
        }


       


        public async Task<long> NextMove(long offset)
        {

            if (!streamReadFormat.CanSeek)
                return 0;


            long offset_next = offset;

            do
            {
                await fiberReadStream.Check();

                var x = streamReadFormat.Length - streamReadFormat.Position;

                if (offset_next > x)
                {
                    streamReadFormat.Position = streamReadFormat.Length;
                    offset_next -= x;
                }
                else
                {
                    streamReadFormat.Position += offset_next;
                    offset_next = 0;
                }

            } while (offset_next > 0);

            return offset - offset_next;
        }

        public async Task<Memory<byte>> ReadToBlockEnd()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");

            await fiberReadStream.Check();

            return fiberReadStream.ReadToBlockEnd();
        }

        public async Task<ArraySegment<byte>> ReadToBlockArrayEnd()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");

            await fiberReadStream.Check();
            return fiberReadStream.ReadToBlockArrayEnd();
        }

        public async Task<int> Read(byte[] data,int offset,int count)
        {
            if (!isinit)
                throw new NotSupportedException("not init it");

            int needcount = count;
            int offset_next = offset;
            do
            {
               
                var res = await streamReadFormat.ReadAsync(data, offset, needcount, CancellationToken.None).ConfigureAwait(false);

                if (res == 0)
                    return count - needcount;

                needcount -= res;
                offset_next += res;



            } while (needcount > 0);

            return count;
        }
     
     

        public IMemoryOwner<byte> GetMemory(int inithint)
        {
           return memoryPool.Rent(inithint);
        }


        #region read integer
        public async Task<byte?> ReadByte()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");


            int count = await this.Read(read_Numericbytes, 0, 1);

            if (count == 1)
                return read_Numericbytes[0];
            else
                return null;

        }

        public async Task<bool?> ReadBoolean()
        {
            var b = await ReadByte();

            return b == 1 ? true : false;
        }

        public async Task<ushort?> ReadUInt16()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");

            int count = await this.Read(read_Numericbytes, 0, 2);

            if (count == 2)
            {
                return (ushort?)ReadInt16(read_Numericbytes);
            }
            else
                return null;
        }

        public async Task<short?> ReadInt16()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");

            int count = await this.Read(read_Numericbytes, 0, 2);

            if (count == 2)
            {
                return ReadInt16(read_Numericbytes);
            }
            else
                return null;
        }

        private unsafe short? ReadInt16(byte[] value)
        {
            if (value == null)            
                return null; 

            fixed (byte* numRef = &(value[0]))
            {
                var x = *(short*)numRef;
                if (isLittleEndian)
                    return BinaryPrimitives.ReverseEndianness(x);
                else
                    return x;
            }
        }
                
        public async Task<uint?> ReadUInt32()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");

            int count = await this.Read(read_Numericbytes, 0, 4);

            if (count == 4)
            {
                
                return (uint?)ReadInt32(read_Numericbytes);           
            }
            else
                return null;
        }

        public async Task<int?> ReadInt32()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");

            int count = await this.Read(read_Numericbytes, 0, 4);

            if (count == 4)
            {
                return ReadInt32(read_Numericbytes);
            }
            else
                return null;
        }

        private unsafe int? ReadInt32(byte[] value)
        {
            if (value == null)
            {
                return null;
            }
            fixed (byte* numRef = &(value[0]))
            {
                var x = *(((int*)numRef));
                if (isLittleEndian)
                    return BinaryPrimitives.ReverseEndianness(x);
                else
                    return x;
            }
        }
        
        public async Task<ulong?> ReadUInt64()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");

            int count = await this.Read(read_Numericbytes, 0, 8);

            if (count == 8)
            {
                return (ulong?)ReadInt64(read_Numericbytes);
            }
            else
                return null;
        }

        public async Task<long?> ReadInt64()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");

            int count = await this.Read(read_Numericbytes, 0, 8);

            if (count == 8)
            {
                return ReadInt64(read_Numericbytes);
            }
            else
                return null;
        }

        private unsafe long? ReadInt64(byte[] value)
        {
            if (value == null)
            {
                return null;
            }
            fixed (byte* numRef = &(value[0]))
            {
                var x = *(((long*)numRef));
                if (isLittleEndian)
                    return BinaryPrimitives.ReverseEndianness(x);
                else
                    return x;
            }
        }


        public async Task<double?> ReadDouble()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");

            
            int count = await this.Read(read_Numericbytes, 0, 8);

            if (count == 8)
            {
                return ReadDouble(read_Numericbytes);
            }
            else
                return null;
        }

        private unsafe double? ReadDouble(byte[] value)
        {
            if (value == null)
            {
                return null;
            }

            fixed (byte* numRef = &(value[0]))
            {
                var x = *(ulong*)numRef;
                ulong p = IsLittleEndian ? BinaryPrimitives.ReverseEndianness(x) : x;
                return *(double*)&p;
            }
        }

        public async Task<float?> ReadSingle()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");


            int count = await this.Read(read_Numericbytes, 0, 4);

            if (count == 4)
            {
                return ReadSingle(read_Numericbytes);
            }
            else
                return null;
        }

        private unsafe float? ReadSingle(byte[] value)
        {
            if (value == null)
            {
                return null;
            }

            fixed (byte* numRef = &(value[0]))
            {
                var x = *(((uint*)numRef));
                uint p = IsLittleEndian ? BinaryPrimitives.ReverseEndianness(x) : x;
                return *(((float*)&p));
            }
        }


        #endregion

        #region read memory block

        public async Task<ResultByMemoryOwner<Memory<byte>>> ReadMemory(int size)
        {
            var imo = GetMemory(size);

            var memory = imo.Memory;
            var array = memory.GetArray();

            int len = await Read(array.Array, array.Offset, size);

            if (len != size)
                throw new System.IO.IOException($"not read data");

            var slice_mem= memory.Slice(0, len);

            return new ResultByMemoryOwner<Memory<byte>>(imo, slice_mem);

        }

        public async Task<ResultByMemoryOwner<Memory<byte>>> ReadMemory()
        {
            int? len = await ReadInt32();

            if (len == null)
                return default;
            else
            {
                return await ReadMemory(len.Value);
            }
        }

        public async Task<byte[]> ReadArray(int size)
        {            

            byte[] array = new byte[size];

            int len = await Read(array, 0, size);

            if (len != size)
                throw new System.IO.IOException($"not read data");

            return array;
        }
   


        public async Task<byte[]> ReadArray()
        {
            int? len = await ReadInt32();

            if (len == null)
                return null;
            else
            {
                return await ReadArray(len.Value);
            }
        }


        public async Task<string> ReadString()
        {
            int? len = await ReadInt32();

            if (len == null)
                return default;
            else
            {

                using (var imo = GetMemory(len.Value))
                {

                    var array = imo.Memory.GetArray();

                    int rlen = await Read(array.Array, array.Offset, len.Value);

                    if (rlen != len.Value)
                        throw new System.IO.IOException($"not read data");

                    return Encoding.GetString(array.Array, array.Offset, rlen);


                }
            }
        }


        #endregion

        #region read obj
        public async Task<S> ReadObject<S>()
        {
            using (var mem = await ReadMemory())
            {
                var array = mem.Value.GetArray();

                using (System.IO.MemoryStream stream = new System.IO.MemoryStream(array.Array, array.Offset, array.Count))
                {
                    return ProtoBuf.Serializer.Deserialize<S>(stream);
                }
            }
        }
        #endregion

        #region write buf

        public void Write(ArraySegment<byte> data)
        {
            streamWriteFormat.Write(data.Array, data.Offset, data.Count);            
        }

        public void Write(byte[] data, int offset, int count)
        {
            streamWriteFormat.Write(data, offset, count);          
        }

        public void Write(byte[] data, bool wlen = true)
        {
            if (wlen)
                Write(data.Length);
            streamWriteFormat.Write(data, 0, data.Length);           
        }

        public void Write(Memory<byte> data, int offset, int count)
        {
            var array = data.GetArray();
            streamWriteFormat.Write(array.Array, array.Offset+offset, count);           
        }


        public void Write(Memory<byte> data, bool wlen = true)
        {
            var array = data.GetArray();

            if (wlen)
                Write(array.Count);

            streamWriteFormat.Write(array.Array, array.Offset, array.Count);           
        }

   

        public void Write(string data)
        {
            byte[] bytes = Encoding.GetBytes(data);
            Write(bytes);
        }

      

        #endregion

        #region integer

        public void Write(byte data)
        {
            streamWriteFormat.WriteByte(data);          
        }

        public unsafe void Write(short data)
        {
            if (IsLittleEndian)
                data = BinaryPrimitives.ReverseEndianness(data);
       
            fixed (byte* numRef = &write_Numericbytes[0])
            {
                *((short*)numRef) = data;
                Write(write_Numericbytes, 0, 2);
            }

           
        }
        public unsafe void Write(int data)
        {
            if (IsLittleEndian)
                data = BinaryPrimitives.ReverseEndianness(data);

            fixed (byte* numRef = &write_Numericbytes[0])
            {
                *((int*)numRef) = data;
                Write(write_Numericbytes, 0, 4);
            }
        }

        public unsafe  void Write(long data)
        {
            if (IsLittleEndian)
                data = BinaryPrimitives.ReverseEndianness(data);

            fixed (byte* numRef = &write_Numericbytes[0])
            {
                *((long*)numRef) = data;
                Write(write_Numericbytes, 0, 8);
            }
        }

        public unsafe void Write(ushort data)
        {
            if (IsLittleEndian)
                data = BinaryPrimitives.ReverseEndianness(data);

            fixed (byte* numRef = &write_Numericbytes[0])
            {
                *((ushort*)numRef) = data;
                Write(write_Numericbytes, 0, 2);
            }
        }


        public unsafe void Write(uint data)
        {
            if (IsLittleEndian)
                data = BinaryPrimitives.ReverseEndianness(data);

            fixed (byte* numRef = &write_Numericbytes[0])
            {
                *((uint*)numRef) = data;
                Write(write_Numericbytes, 0, 4);
            }
        }
        public unsafe void Write(ulong data)
        {
            if (IsLittleEndian)
                data = BinaryPrimitives.ReverseEndianness(data);

            fixed (byte* numRef = &write_Numericbytes[0])
            {
                *((ulong*)numRef) = data;
                 Write(write_Numericbytes, 0, 8);
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
            }
        }


        public void Write(bool data)
        {
            Write(data ? ((byte)1) : ((byte)0));
        }
                    

        public void Write(bool? data)
        {
            Write(data.GetValueOrDefault());
        }

        public void Write(byte? data)
        {
            Write(data.GetValueOrDefault());
        }

        public void Write(short? data)
        {
            Write(data.GetValueOrDefault());
        }

        public void Write(int? data)
        {
            Write(data.GetValueOrDefault());
        }

        public void Write(long? data)
        {
            Write(data.GetValueOrDefault());
        }

        public void Write(ushort? data)
        {
            Write(data.GetValueOrDefault());
        }

        public void Write(uint? data)
        {
            Write(data.GetValueOrDefault());
        }

        public void Write(ulong? data)
        {
            Write(data.GetValueOrDefault());
        }

        public void Write(double? data)
        {
            Write(data.GetValueOrDefault());
        }

        public void Write(float? data)
        {
            Write(data.GetValueOrDefault());
        }


        #endregion

        #region wr obj

        public void Write(object obj)
        {
            if (StreamWriteFormat.CanSeek)
            {
                var bkpostion = StreamWriteFormat.Position;
                Write(0);
                ProtoBuf.Meta.RuntimeTypeModel.Default.Serialize(StreamWriteFormat, obj);
                var lastpostion = StreamWriteFormat.Position;
                var len = lastpostion - bkpostion - 4;
                StreamWriteFormat.Position = bkpostion;
                Write((int)len);
                StreamWriteFormat.Position = lastpostion;
            }
            else
            {
                using (var stream = new MemoryStream())
                {
                    ProtoBuf.Meta.RuntimeTypeModel.Default.Serialize(stream, obj);
                    byte[] data = stream.ToArray();
                    Write(data);
                }

            }
        }




        #endregion

        public async ValueTask<int> Flush()
        {

            StreamWriteFormat.Flush();

            if (FiberWriteStream.Length > 0)
            {
                return await FiberWriteStream.AwaitFlush();
            }
            else
                return 0;
        }
    }


}

﻿using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ZYSocket.Interface;
using ZYSocket.FiberStream.Synchronization;
using System.Runtime.InteropServices;

namespace ZYSocket.FiberStream
{
    public class FiberRw<T> : IDisposable, IFiberRw<T> where T : class
    {
        private readonly MemoryPool<byte> memoryPool;
        public MemoryPool<byte> MemoryPool { get => memoryPool; }

        private readonly bool isinit;
        public bool IsInit { get => isinit; }

        public Encoding Encoding { get; }

        public ISerialization ObjFormat { get; }

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
        public ISockAsyncEvent Async { get; }
        public T? UserToken { get => (T?)Async.UserToken; set => Async.UserToken = value; }

        public ISyncRun Sync { get;}

        private readonly byte[] read_Numericbytes;
        private readonly byte[] write_Numericbytes;

        public FiberRw(ISockAsyncEvent async, IFiberReadStream fiberRStream, IFiberWriteStream fiberWStream, MemoryPool<byte> memoryPool, Encoding encoding, ISerialization? objFormat, bool isLittleEndian = false, Stream? inputStream = null, Stream? outputStream = null, Func<Stream, Stream, GetFiberRwResult>? init = null)
        {
            if (!(fiberRStream is Stream r_stream))
                throw new NullReferenceException("fiberRStream not stream");
            if (!(fiberWStream is Stream w_stream))
                throw new NullReferenceException("fiberWStream not stream");

            if (init != null)
            {

                var result = init(inputStream ?? r_stream, outputStream ?? w_stream);

                if (result != null)
                {
                    streamReadFormat = result.Input;
                    streamWriteFormat = result.Output;

                }
                else
                {
                    streamReadFormat = inputStream ?? r_stream;
                    streamWriteFormat = outputStream ?? w_stream;
                }
            }
            else
            {
                streamReadFormat = inputStream ?? r_stream;
                streamWriteFormat = outputStream ?? w_stream;
            }
            this.Async = async;
            UserToken = null;
            fiberReadStream = fiberRStream;
            fiberWriteStream = fiberWStream;
            this.Encoding = encoding;
            this.memoryPool = memoryPool;
            this.isLittleEndian = isLittleEndian;
            read_Numericbytes = fiberReadStream.Numericbytes;
            write_Numericbytes = fiberWriteStream.Numericbytes;
            Sync = new SyncRun();
            if (objFormat is null)
                ObjFormat = new ProtobuffObjFormat();
            else
                ObjFormat = objFormat;

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
                if (fiberReadStream.NeedRead)
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
            if (fiberReadStream.NeedRead)
                await fiberReadStream.Check();
            return fiberReadStream.ReadToBlockEnd();
        }

        public async Task<ArraySegment<byte>> ReadToBlockArrayEnd()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");
            if (fiberReadStream.NeedRead)
                await fiberReadStream.Check();
            return fiberReadStream.ReadToBlockArrayEnd();
        }

        public async Task<int> ReadAsync(byte[] data, int offset, int count)
        {
            if (!isinit)
                throw new NotSupportedException("not init it");

            int needcount = count;
            int offset_next = offset;
            do
            {

                var res = await streamReadFormat.ReadAsync(data, offset_next, needcount, CancellationToken.None).ConfigureAwait(false);

                if (res == 0)
                    return count - needcount;

                needcount -= res;
                offset_next += res;



            } while (needcount > 0);

            return count;
        }


        public async ValueTask<int> ReadAsync(Memory<byte> data,bool doall=true)
        {
            if (!isinit)
                throw new NotSupportedException("not init it");
            if (doall)
            {
                if (MemoryMarshal.TryGetArray(data, out ArraySegment<byte> destinationArray))
                    return await ReadAsync(destinationArray.Array, destinationArray.Offset, destinationArray.Count);
                else
                    return 0;
            }
            else
            {
                return await streamReadFormat.ReadAsync(data, CancellationToken.None);
            }
        }



        public IMemoryOwner<byte> GetMemory(int inithint)
        {
            return memoryPool.Rent(inithint);
        }

        #region read integer
        public async Task<byte> ReadByte()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");


            int count = await this.ReadAsync(read_Numericbytes, 0, 1);

            if (count == 1)
                return read_Numericbytes[0];
            else
                throw new IOException("read data len error"); 

        }

        public async Task<bool> ReadBoolean()
        {
            var b = await ReadByte();

            return b == 1 ? true : false;
        }

        public async Task<ushort> ReadUInt16()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");

            int count = await this.ReadAsync(read_Numericbytes, 0, 2);

            if (count == 2)
            {
                return (ushort)ReadInt16(read_Numericbytes);
            }
            else
                throw new IOException("read data len error"); 
        }

        public async Task<short> ReadInt16()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");

            int count = await this.ReadAsync(read_Numericbytes, 0, 2);

            if (count == 2)
            {
                return ReadInt16(read_Numericbytes);
            }
            else
                throw new IOException("read data len error");
        }

        private unsafe short ReadInt16(byte[] value)
        {         

            fixed (byte* numRef = &(value[0]))
            {
                var x = *(short*)numRef;
                if (isLittleEndian)
                    return BinaryPrimitives.ReverseEndianness(x);
                else
                    return x;
            }
        }

        public async Task<uint> ReadUInt32()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");

            int count = await this.ReadAsync(read_Numericbytes, 0, 4);

            if (count == 4)
            {
                return (uint)ReadInt32(read_Numericbytes);
            }
            else
                throw new IOException("read data len error");
        }

        public async Task<int> ReadInt32()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");

            int count = await this.ReadAsync(read_Numericbytes, 0, 4);

            if (count == 4)
            {
                return ReadInt32(read_Numericbytes);
            }
            else
                throw new IOException("read data len error");
        }

        private unsafe int ReadInt32(byte[] value)
        {           
            fixed (byte* numRef = &(value[0]))
            {
                var x = *(int*)numRef;
                if (isLittleEndian)
                    return BinaryPrimitives.ReverseEndianness(x);
                else
                    return x;
            }
        }

        public async Task<ulong> ReadUInt64()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");

            int count = await this.ReadAsync(read_Numericbytes, 0, 8);

            if (count == 8)
            {
                return (ulong)ReadInt64(read_Numericbytes);
            }
            else
                throw new IOException("read data len error");
        }

        public async Task<long> ReadInt64()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");

            int count = await this.ReadAsync(read_Numericbytes, 0, 8);

            if (count == 8)
            {
                return ReadInt64(read_Numericbytes);
            }
            else
                throw new IOException("read data len error");
        }

        private unsafe long ReadInt64(byte[] value)
        {
           
            fixed (byte* numRef = &(value[0]))
            {
                var x = *(long*)numRef;
                if (isLittleEndian)
                    return BinaryPrimitives.ReverseEndianness(x);
                else
                    return x;
            }
        }


        public async Task<double> ReadDouble()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");


            int count = await this.ReadAsync(read_Numericbytes, 0, 8);

            if (count == 8)
            {
                return ReadDouble(read_Numericbytes);
            }
            else
                throw new IOException("read data len error");
        }

        private unsafe double ReadDouble(byte[] value)
        {           

            fixed (byte* numRef = &(value[0]))
            {
                var x = *(ulong*)numRef;
                ulong p = IsLittleEndian ? BinaryPrimitives.ReverseEndianness(x) : x;
                return *(double*)&p;
            }
        }

        public async Task<float> ReadSingle()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");


            int count = await this.ReadAsync(read_Numericbytes, 0, 4);

            if (count == 4)
            {
                return ReadSingle(read_Numericbytes);
            }
            else
                throw new IOException("read data len error");
        }

        private unsafe float ReadSingle(byte[] value)
        {           

            fixed (byte* numRef = &(value[0]))
            {
                var x = *(uint*)numRef;
                uint p = IsLittleEndian ? BinaryPrimitives.ReverseEndianness(x) : x;
                return *((float*)&p);
            }
        }


        #endregion

        #region read memory block

        public async Task<ResultByMemoryOwner<Memory<byte>>> ReadMemory(int size)
        {
            if (size == 0)
                return default;

            var imo = GetMemory(size);
            try
            {
                var memory = imo.Memory;
                var array = memory.GetArray();
                int len = await ReadAsync(array.Array, array.Offset, size);

                if (len != size)
                    throw new System.IO.IOException($"not read data");

                var slice_mem = memory.Slice(0, len);

                return new ResultByMemoryOwner<Memory<byte>>(imo, slice_mem);
            }
            catch
            {
                imo.Dispose();
                throw;
            }
        }
    

        public async Task<ResultByMemoryOwner<Memory<byte>>> ReadMemory()
        {
            int? len = await ReadInt32();

            if (len == null || len.Value == 0)
                return default;
            else
            {
                return await ReadMemory(len.Value);
            }
        }

     

        public async Task<byte[]> ReadArray(int size)
        {
            if (size == 0)
                return new byte[] { };

            byte[] array = new byte[size];

            int len = await ReadAsync(array, 0, size);

            if (len != size)
                throw new System.IO.IOException($"not read data");

            return array;
        }

      

        public async Task<byte[]> ReadArray()
        {
            int? len = await ReadInt32();

            if (len == null)
                return new byte[] { };
            else
            {
                return await ReadArray(len.Value);
            }
        }

       

        public async Task<string> ReadString(int len)
        {

            if (len == 0)
                return "";
            else
            {

                using var imo = GetMemory(len);

                var array = imo.Memory.GetArray();
                int rlen = await ReadAsync(array.Array, array.Offset, len);
                if (rlen != len)
                    throw new System.IO.IOException($"not read data");
                return Encoding.GetString(array.Array, array.Offset, rlen);

            }
        }

        public async ValueTask<ResultByMemoryOwner<Memory<byte>>> ReadLine()
        {
            if (!isinit)
                throw new NotSupportedException("not init it");

            var imo = GetMemory(4096);

            try
            {
                var memory = imo.Memory;
                var array = memory.GetArray();
                int needcount = array.Count;
                int offset_next = array.Offset;
                do
                {

                    var res = streamReadFormat.Read(array.Array, offset_next, 1);

                    if (res == 0)
                    {
                        if (fiberReadStream.NeedRead)
                            await fiberReadStream.Check();
                    }
                    else
                    {
                        needcount--;
                        if (array.Array[offset_next] == 10)
                            break;
                        offset_next++;
                    }

                } while (needcount > 0);

                var slice_mem = imo.Memory.Slice(0, array.Count - needcount);

                return new ResultByMemoryOwner<Memory<byte>>(imo, slice_mem);
             }
            catch
            {
                imo.Dispose();
                throw;
            }
        }

        public async ValueTask<Memory<byte>> ReadLine(Memory<byte> memory)
        {
            if (!isinit)
                throw new NotSupportedException("not init it");
            var array = memory.GetArray();
            int needcount = array.Count;
            int offset_next = array.Offset;
            do
            {

                var res = streamReadFormat.Read(array.Array, offset_next, 1);

                if (res == 0)
                {
                    if (fiberReadStream.NeedRead)
                        await fiberReadStream.Check();
                }
                else
                {
                    needcount--;
                    if (array.Array[offset_next] == 10)
                        break;                  
                    offset_next++;
                }
            } while (needcount > 0);

            return memory.Slice(0, array.Count - needcount); 
        }


        public async Task<string> ReadString()
        {
            int? len = await ReadInt32();

            if (len == null)
                return "";
            else
            {
                return await ReadString(len.Value);
            }
        }


      


        #endregion

        #region read obj
        public async Task<S> ReadObject<S>()
        {
            using var mem = await ReadMemory();
            var array = mem.Value.GetArray();
            return ObjFormat.Deserialize<S>(array.Array, array.Offset, array.Count);
        }

        public async Task<object> ReadObject(Type type)
        {
            using var mem = await ReadMemory();
            var array = mem.Value.GetArray();
            return ObjFormat.Deserialize(type, array.Array, array.Offset, array.Count);
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

            Write(data.Value, offset,count);
        }


        public void Write(string data, bool wrlen = true)
        {
            byte[] bytes = Encoding.GetBytes(data);
            Write(bytes,wrlen);
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
                ulong format = *(ulong*)&data;
                Write(format);
            }
        }

        public void Write(float data)
        {
            unsafe
            {
                uint format = *(uint*)&data;
                Write(format);
            }
        }


        public void Write(bool data)
        {
            Write(data ? (byte)1 : (byte)0);
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


        #endregion

        #region wr obj

        public void Write(object obj) => Write(ObjFormat.Serialize(obj));


        #endregion

        public Task FlushAsync(bool send = true)
        {
            if (!send)
                return Task.FromResult(0);           
            StreamWriteFormat.Flush();
            if (FiberWriteStream.Length > 0)
            {
                return FiberWriteStream.FlushAsync();
            }
            else
                return Task.CompletedTask;
        }


        public void Flush(bool send = true)
        {
            if (!send)
                return;

            StreamWriteFormat.Flush();
            if (FiberWriteStream.Length > 0)            
                 FiberWriteStream.Flush();                     
        }
   
    }


}

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZYSocket.FiberStream
{
    public interface IFiberRw
    {

        ISockAsyncEvent Async { get; }
        Encoding Encoding { get; }
        IFiberReadStream FiberReadStream { get; }
        IFiberWriteStream FiberWriteStream { get; }
        bool IsInit { get; }
        bool IsLittleEndian { get; }    
        Stream StreamReadFormat { get; }
        Stream StreamWriteFormat { get; }
        IMemoryOwner<byte> GetMemory(int inithint);     
        Task<long> NextMove(long offset);
        ValueTask<int> Read(byte[] data, int offset, int count);
        ValueTask<byte[]> ReadArray();
        ValueTask<byte[]> ReadArray(int count);   
        ValueTask<bool?> ReadBoolean();
        ValueTask<byte?> ReadByte();
        ValueTask<double?> ReadDouble();
        ValueTask<short?> ReadInt16();
        ValueTask<int?> ReadInt32();
        ValueTask<long?> ReadInt64();
        ValueTask<ResultByMemoryOwner<Memory<byte>>> ReadMemory();
        ValueTask<ResultByMemoryOwner<Memory<byte>>> ReadMemory(int count);
        ValueTask<T> ReadObject<T>();
        ValueTask<float?> ReadSingle();
        ValueTask<string> ReadString();
        Task<ArraySegment<byte>> ReadToBlockArrayEnd();
        Task<Memory<byte>> ReadToBlockEnd();
        ValueTask<ushort?> ReadUInt16();
        ValueTask<uint?> ReadUInt32();
        ValueTask<ulong?> ReadUInt64();

        void Write(ArraySegment<byte> data);
        void Write(byte[] data, int offset, int count);
        void Write(byte[] data, bool wlen = true);
        void Write(Memory<byte> data, int offset, int count);
        void Write(Memory<byte> data, bool wlen = true);
        void Write(string data);
        void Write(byte data);
        void Write(short data);
        void Write(int data);
        void Write(long data);
        void Write(ushort data);
        void Write(uint data);
        void Write(ulong data);
        void Write(double data);
        void Write(float data);
        void Write(bool? data);
        void Write(byte? data);
        void Write(short? data);
        void Write(int? data);
        void Write(long? data);
        void Write(ushort? data);
        void Write(uint? data);
        void Write(ulong? data);
        void Write(double? data);
        void Write(float? data);
        void Write(bool data);
        void Write(object obj);
        ValueTask<int> Flush();
    }

    public interface IFiberRw<T> : IFiberRw
    {
        T UserToken { get; set; }
    }
}
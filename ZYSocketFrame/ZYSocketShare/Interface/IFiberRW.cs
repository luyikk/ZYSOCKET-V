using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZYSocket.Interface;

namespace ZYSocket.FiberStream
{
    public interface IFiberRw
    {

        ISockAsyncEvent Async { get; }
        Encoding Encoding { get; }
        ISerialization ObjFormat { get; }
        IFiberReadStream FiberReadStream { get; }
        IFiberWriteStream FiberWriteStream { get; }
        bool IsInit { get; }
        bool IsLittleEndian { get; }    
        Stream StreamReadFormat { get; }
        Stream StreamWriteFormat { get; }
        IMemoryOwner<byte> GetMemory(int inithint);     
        Task<long> NextMove(long offset);
        Task<int> ReadAsync(byte[] data, int offset, int count);  
        Task<byte[]> ReadArray();
        Task<byte[]> ReadArray(int count);
        Task<bool?> ReadBoolean();
        Task<byte?> ReadByte();
        Task<double?> ReadDouble();
        Task<short?> ReadInt16();
        Task<int?> ReadInt32();
        Task<long?> ReadInt64();
        Task<ResultByMemoryOwner<Memory<byte>>> ReadMemory();
        Task<ResultByMemoryOwner<Memory<byte>>> ReadMemory(int count);
        Task<T> ReadObject<T>();
        Task<float?> ReadSingle();
        Task<string> ReadString();
        Task<ArraySegment<byte>> ReadToBlockArrayEnd();
        Task<Memory<byte>> ReadToBlockEnd();
        Task<ushort?> ReadUInt16();
        Task<uint?> ReadUInt32();
        Task<ulong?> ReadUInt64();

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
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using ZYSocket.Server;
using System.Buffers;

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
        void Disconnect();
        ValueTask<long> NextMove(long offset);
        ValueTask<int> Read(byte[] data, int offset, int count);
        ValueTask<byte[]> ReadArray();
        ValueTask<byte[]> ReadArray(int count);
        ValueTask<int> ReadAsync(byte[] data, int offset, int count);
        ValueTask<int> ReadAsync(byte[] data, int offset, int count, CancellationToken cancellationToken);
        ValueTask<bool?> ReadBoolean();
        ValueTask<byte?> ReadByte();
        ValueTask<double?> ReadDouble();
        ValueTask<short?> ReadInt16();
        ValueTask<int?> ReadInt32();
        ValueTask<long?> ReadInt64();
        ValueTask<Result<Memory<byte>>> ReadMemory();
        ValueTask<Result<Memory<byte>>> ReadMemory(int count);
        ValueTask<T> ReadObject<T>();
        ValueTask<float?> ReadSingle();
        ValueTask<string> ReadString();
        ValueTask<ArraySegment<byte>> ReadToBlockArrayEnd();
        ValueTask<Memory<byte>> ReadToBlockEnd();
        ValueTask<ushort?> ReadUInt16();
        ValueTask<uint?> ReadUInt32();
        ValueTask<ulong?> ReadUInt64();

        ValueTask<int> Write(ArraySegment<byte> data);
        ValueTask<int> Write(byte[] data, int offset, int count);
        ValueTask<int> Write(byte[] data, bool wlen = true);
        ValueTask<int> Write(Memory<byte> data, int offset, int count);
        ValueTask<int> Write(Memory<byte> data, bool wlen = true);
        ValueTask<int> Write(string data);
        ValueTask<int> Write(byte data);
        ValueTask<int> Write(short data);
        ValueTask<int> Write(int data);
        ValueTask<int> Write(long data);
        ValueTask<int> Write(ushort data);
        ValueTask<int> Write(uint data);
        ValueTask<int> Write(ulong data);
        ValueTask<int> Write(double data);
        ValueTask<int> Write(float data);
        ValueTask<int> Write(bool data);
        ValueTask<int> Write(object obj);

    }

    public interface IFiberRw<T> : IFiberRw
    {
        T UserToken { get; set; }
    }
}
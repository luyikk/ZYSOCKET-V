using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using ZYSocket.Server;

namespace ZYSocket.FiberStream
{
    public interface IFiberRw
    {

        ZYSocketAsyncEventArgs Async { get; }
        Encoding Encoding { get; }
        IFiberReadStream FiberReadStream { get; }
        IFiberWriteStream FiberWriteStream { get; }
        bool IsInit { get; }
        bool IsLittleEndian { get; }    
        Stream StreamReadFormat { get; }
        Stream StreamWriteFormat { get; }
        Memory<byte> GetMemory(int inithint);
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
        ValueTask<Memory<byte>> ReadMemory();
        ValueTask<Memory<byte>> ReadMemory(int count);
        ValueTask<S> ReadObject<S>();
        ValueTask<float?> ReadSingle();
        ValueTask<string> ReadString();
        ValueTask<ArraySegment<byte>> ReadToBlockArrayEnd();
        ValueTask<Memory<byte>> ReadToBlockEnd();
        ValueTask<ushort?> ReadUInt16();
        ValueTask<uint?> ReadUInt32();
        ValueTask<ulong?> ReadUInt64();
    }

}
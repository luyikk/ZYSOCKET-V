﻿using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ZYSocket.FiberStream.Synchronization;
using ZYSocket.Interface;

namespace ZYSocket.FiberStream
{

    public interface IFiberRw: IBufferWrite, IBufferAsyncRead
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
    }

    public interface IFiberRw<T> : IFiberRw where T:class 
    {
        T? UserToken { get; set; }
    }
}
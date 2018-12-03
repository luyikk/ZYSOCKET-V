using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace ZYSocket.FiberStream
{
    public struct ResultByMemoryOwner<T>:IDisposable 
    {
        public bool IsInit { get; private set; }
        public T Value { get; private set; }

        public IMemoryOwner<byte> MemoryOwner { get; private set; }

        public ResultByMemoryOwner(IMemoryOwner<byte> memoryOwner,T value)
        {
            MemoryOwner = memoryOwner;
            Value = value;
            IsInit = true;
        }

        public void Dispose()
        {
            MemoryOwner?.Dispose();
        }
    }
}

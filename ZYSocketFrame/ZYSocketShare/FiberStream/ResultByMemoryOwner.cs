using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace ZYSocket.FiberStream
{
    public readonly struct ResultByMemoryOwner<T>:IDisposable 
    {
        public bool IsInit { get;  }
        public T Value { get;  }

        public IMemoryOwner<byte> MemoryOwner { get;  }

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

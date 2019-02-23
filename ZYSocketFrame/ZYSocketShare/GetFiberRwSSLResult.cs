using System;
using System.Collections.Generic;
using System.Text;
using ZYSocket.FiberStream;

namespace ZYSocket
{
    public struct GetFiberRwSSLResult
    {
        public bool IsError { get; set; }

        public IFiberRw FiberRw { get; set; }

        public string ErrMsg { get; set; }
    }

    public struct GetFiberRwSSLResult<T>
    {
        public bool IsError { get; set; }

        public IFiberRw<T> FiberRw { get; set; }

        public string ErrMsg { get; set; }
    }
}

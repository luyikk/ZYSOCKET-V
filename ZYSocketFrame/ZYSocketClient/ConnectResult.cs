using System;
using System.Collections.Generic;
using System.Text;

namespace ZYSocket.Client
{
    public struct ConnectResult
    {
        public bool IsSuccess { get; set; }

        public string Msg { get; set; }

        public ConnectResult(bool isSuccess,string msg)
        {
            IsSuccess = isSuccess;
            Msg = msg;
        }

        public override string ToString()
        {
            return $"Success:{IsSuccess}->{Msg}";
        }
    }
}

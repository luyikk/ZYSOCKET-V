using System;
using System.Collections.Generic;
using System.Text;

namespace ZYSocket.Server.Builder
{
    public class SocketServerOptions
    {
        public string Host { get; set; } = "Any";

        public int Port { get; set; } = 1000;

        public int MaxConnectCout { get; set; } = 1000;

        public int MaxBufferSize { get; set; } = 4096;

        public bool NoDelay { get; set; } = false;

        public int ReceiveTimeout { get; set; } = 0;
        public int SendTimeout { get; set; } = 0;
        public int BackLog { get; set; } = 512;

        public bool IsLittleEndian { get; set; } = false;

    }
}

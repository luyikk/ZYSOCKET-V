using System;
using System.Collections.Generic;
using System.Text;

namespace ZYSocket.Server.Builder
{
    public class SocketServerOptions
    {
        /// <summary>
        /// 监听的IP
        /// </summary>
        public string Host { get; set; } = "Any";

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; } = 1000;

        /// <summary>
        /// 最大连接数
        /// </summary>
        public int MaxConnectCout { get; set; } = 1000;

        /// <summary>
        /// 缓冲区大小
        /// </summary>
        public int MaxBufferSize { get; set; } = 4096;

        /// <summary>
        /// 最大包长度
        /// </summary>
        public int MaxPackerSize { get; set; } = 128 * 1024;

        /// <summary>
        /// 是否关闭Nelay
        /// </summary>
        public bool NoDelay { get; set; } = false;
        /// <summary>
        /// 读取超时时间
        /// </summary>
        public int ReceiveTimeout { get; set; } = 0;
        /// <summary>
        /// 发送超时时间
        /// </summary>
        public int SendTimeout { get; set; } = 0;
        /// <summary>
        /// 监听 BackLog
        /// </summary>
        public int BackLog { get; set; } = 512;
        /// <summary>
        /// 是否小尾编码
        /// </summary>
        public bool IsLittleEndian { get; set; } = false;

    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ZYSocket.FiberStream;

namespace ZYSocket.Interface
{
    public interface ISerialization
    {
        /// <summary>
        /// 序列化对象成二进制数据
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        byte[] Serialize(object obj);

        /// <summary>
        /// 反序列化二进制数据成对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data">二进制对象</param>
        /// <param name="offset">偏移值</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        T Deserialize<T>(byte[] data, int offset, int length);
        /// <summary>
        /// 反序列化二进制数据成对象
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="data">二进制对象</param>
        /// <param name="offset">偏移值</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        object Deserialize(Type type, byte[] data, int offset, int length);
    }
}

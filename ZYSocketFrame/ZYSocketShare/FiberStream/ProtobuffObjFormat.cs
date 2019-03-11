using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZYSocket.FiberStream;
using ZYSocket.Interface;

namespace ZYSocket.FiberStream
{
    public class ProtobuffObjFormat : ISerialization
    {
        public T Deserialize<T>(byte[] data,int offset,int length)
        {
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream(data, offset, length))
            {
                return ProtoBuf.Serializer.Deserialize<T>(stream);
            }
        }

        public object Deserialize(Type type, byte[] data, int offset, int length)
        {
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream(data, offset, length))
            {
                return ProtoBuf.Serializer.Deserialize(type,stream);
            }
        }

        public byte[] Serialize(object obj)
        {
            using (var mmstream = new MemoryStream())
            {
                ProtoBuf.Meta.RuntimeTypeModel.Default.Serialize(mmstream, obj);
                return mmstream.ToArray();
            }
        }
    }
}

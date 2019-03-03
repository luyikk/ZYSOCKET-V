using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZYSocket.FiberStream;
using ZYSocket.Interface;

namespace ZYSocket.FiberStream
{
    public class ProtobuffObjFormat : IObjFormat
    {
        public T Read<T>(byte[] data,int offset,int count)
        {
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream(data, offset, count))
            {
                return ProtoBuf.Serializer.Deserialize<T>(stream);
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

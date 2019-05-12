using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ZYSocket.FiberStream;

namespace ZYSocket
{



    public  class ReadBytes:IDisposable
    {
        public static LengthLen LenType { get; set; } = LengthLen.Int32;

        public static long MaxPackerSize { get; set; } = 1024 * 16;

        public IFiberRw FiberRw { get; }

        public int Packerlen { get; private set; }

        public IMemoryOwner<byte> MemoryOwner { get; private set; }

        public Memory<byte> Memory { get; private set; }

        public int HandLen { get; }

        private readonly bool IsLittleEndian;

        public ReadBytes(IFiberRw readFiber)
        {
            this.FiberRw = readFiber;
            this.Packerlen = -1;
            Memory = null;
            MemoryOwner = null;
            HandLen = (int)LenType;
            IsLittleEndian = FiberRw.IsLittleEndian;
        }


        public void Dispose()
        {
            MemoryOwner?.Dispose();
            MemoryOwner = null;
        }

        public async Task<ReadBytes> Init()
        {
            switch (LenType)
            {
                case LengthLen.Byte:
                    {
                        var l = await FiberRw.ReadByte();
                        if (l != null)
                            Packerlen = l.Value;
                    }
                    break;
                case LengthLen.Int16:
                    {
                        var l = await FiberRw.ReadUInt16();
                        if (l != null)
                            Packerlen = l.Value;
                    }
                    break;
                case LengthLen.Int32:
                    {
                        var l = await FiberRw.ReadUInt32();
                        if (l != null)
                            Packerlen = (int)l.Value;
                    }
                    break;
                case LengthLen.Int64:
                    {
                        var l = await FiberRw.ReadInt64();
                        if (l != null)
                            Packerlen = (int)l.Value;
                    }
                    break;

            }

            Packerlen -= HandLen;


            if (Packerlen > MaxPackerSize)
                throw new System.IO.IOException($"the packer size greater than MaxPackerSize:{Packerlen}");

            if (Packerlen == -1)
                throw new System.IO.IOException($"not read packer size");


            var res = await FiberRw.ReadMemory(Packerlen);
            MemoryOwner = res.MemoryOwner;
            Memory = res.Value;

            return this;
        }

        public Task<long> NextMove(int offset)
        {
            return FiberRw.NextMove(offset);
        }


        public byte? ReadByte()
        {
            if (Memory.Length > 0)
            {

                var value = MemoryMarshal.Read<byte>(Memory.Span);

                Memory = Memory.Slice(1);

                return value;
            }
            else
                return null;
        }

        public bool? ReadBoolean()
        {
            if (Memory.Length > 1)
            {

                var value = MemoryMarshal.Read<bool>(Memory.Span);

                Memory = Memory.Slice(1);

                return value;
            }
            else
                return null;
        }

    

        public short? ReadInt16()
        {
            if (Memory.Length > 1)
            {

                var value = MemoryMarshal.Read<short>(Memory.Span);

                Memory = Memory.Slice(2);

                if (IsLittleEndian)
                {
                    unsafe
                    {
                        var v = BinaryPrimitives.ReverseEndianness(*(ushort*)&value);
                        return *(short*)&v;
                    }
                }
                else
                    return value;
            }
            else
                return null;
        }

        public ushort? ReadUint16()
        {
            if (Memory.Length > 1)
            {

                var value = MemoryMarshal.Read<ushort>(Memory.Span);

                Memory = Memory.Slice(2);

                return IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
            }
            else
                return null;
        }

        public int? ReadInt32()
        {
            if (Memory.Length > 3)
            {
                var span = Memory.Span;
                var value = MemoryMarshal.Read<int>(span);

                Memory = Memory.Slice(4);

                if (IsLittleEndian)
                {
                    unsafe
                    {
                        var v = BinaryPrimitives.ReverseEndianness(*(uint*)&value);
                        return *(int*)&v;
                    }
                }
                else
                    return value;
            }
            else
                return null;
        }



        public uint? ReadUint32()
        {
            if (Memory.Length > 3)
            {

                var value = MemoryMarshal.Read<uint>(Memory.Span);

                Memory = Memory.Slice(4);

                return IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
            }
            else
                return null;
        }


        public long? ReadInt64()
        {
            if (Memory.Length > 7)
            {

                var value = MemoryMarshal.Read<long>(Memory.Span);

                Memory = Memory.Slice(8);

                return IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
            }
            else
                return null;
        }

        public ulong? ReadUint64()
        {
            if (Memory.Length > 7)
            {

                var value = MemoryMarshal.Read<ulong>(Memory.Span);

                Memory = Memory.Slice(8);

                return IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
            }
            else
                return null;
        }

        public float? ReadSingle()
        {
            if (Memory.Length > 3)
            {
                unsafe
                {
                    var value = MemoryMarshal.Read<uint>(Memory.Span);

                    Memory = Memory.Slice(4);

                    uint p = IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;

                    return *(float*)&p;
                }
            }
            else
                return null;
        }

        public double? ReadDouble()
        {
            if (Memory.Length > 7)
            {
                unsafe
                {
                    var value = MemoryMarshal.Read<ulong>(Memory.Span);

                    Memory = Memory.Slice(8);

                    ulong p = IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;

                    return *(double*)&p;
                }
            }
            else
                return null;
        }


        public string ReadString()
        {
            var len = ReadInt32();

            if (len is null)
                return null;


            var mm = Memory.GetArray();
            var tmpstr = FiberRw.Encoding.GetString(mm.Array, mm.Offset, len.Value);
            Memory = Memory.Slice(len.Value);
            return tmpstr;

        }

        public Memory<byte> ReadMemory()
        {
            var len = ReadInt32();

            if (len is null)
                return null;


            var mm = Memory.Slice(0, len.Value);
            Memory = Memory.Slice(len.Value);
            return mm;

        }

        public Span<byte> ReadSpan(int  count)
        {
            var mm = Memory.Slice(0, count);
            Memory = Memory.Slice(count);
            return  mm.Span;
        }

        public Span<byte> ReadSpan()
        {
            return ReadMemory().Span;
        }

        public byte[] ReadArray()
        {
            var len = ReadInt32();

            if (len is null)
                return null;


            var mm = Memory.Slice(0, len.Value).GetArray();
            Memory = Memory.Slice(len.Value);

            byte[] source = mm.Array;

            byte[] target = new byte[len.Value];

            unsafe
            {
                fixed(byte* sourcep=&source[mm.Offset])
                    fixed(byte * targetp=&target[0])
                {
                    Buffer.MemoryCopy(sourcep, targetp, target.LongLength, target.LongLength);

                    return target;
                }
            }
            
        }

        public T ReadObject<T>()
        {
            var mem = ReadMemory();
            var array = mem.GetArray();
            return FiberRw.ObjFormat.Deserialize<T>(array.Array, array.Offset, array.Count);
        }

        public object ReadObject(Type type)
        {
            var mem = ReadMemory();
            var array = mem.GetArray();
            return FiberRw.ObjFormat.Deserialize(type,array.Array, array.Offset, array.Count);
        }

    }
}

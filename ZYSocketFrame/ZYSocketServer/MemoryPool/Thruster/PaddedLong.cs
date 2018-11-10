using System.Runtime.InteropServices;

namespace Thruster
{
    [StructLayout(LayoutKind.Explicit, Size = Util.CacheLineSize)]
    struct PaddedLong
    {
        [FieldOffset(0)]
        public long Value;
    }
}
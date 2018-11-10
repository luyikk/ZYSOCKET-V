using System.Runtime.CompilerServices;
using System.Threading;

namespace Thruster
{
    public static class Intelocked2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long GetMask(int continousItems) => (1L << continousItems) - 1;

        public static long Xor(ref long v, int offset, int length)
        {
            var mask = GetMask(length);
            mask <<= offset;
            var value = Volatile.Read(ref v);
            long previous;
            do
            {
                previous = value;
                value = Interlocked.CompareExchange(ref v, mask ^ previous, previous);
            } while (value != previous);

            return value;
        }
    }
}
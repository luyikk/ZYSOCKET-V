using System.Runtime.CompilerServices;
using System.Threading;

namespace Thruster
{
    /// <summary>
    /// This class is responsible for managing leases of conitnous bits 1111 over a ref long. This means that any lease cannot be claimed for more than 63 consecutive elements
    ///
    /// A lease of specific length n is represented as a n consecutive bits set to 1. This mask is easily calculated: 2^n - 1.
    /// </summary>
    struct Leasing
    {
        readonly PaddedLong[] masks;
        internal const int ChunksPerLeaseLong = 63;

        public Leasing(int size)
        {
            masks = new PaddedLong[size + 2];
        }

        /// <summary>
        /// Simply calculates 2^n - 1;
        /// </summary>
        /// <param name="continousItems"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long GetMask(int continousItems) => (1L << continousItems) - 1;

        public static long Lease(ref Leasing leasing, int index, int continousItems, int retries) => Lease(ref leasing.masks[index + 1].Value, continousItems, retries);

        /// <summary>
        /// Returns a positive position of the continousItem, or negative value that was observed as the last, providing opportunity to base selection of the bucket on this value.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="continousItems"></param>
        /// <param name="retries"></param>
        /// <returns></returns>
        static long Lease(ref long v, int continousItems, int retries)
        {
            var length = ChunksPerLeaseLong + 1 - continousItems;
            var mask = GetMask(continousItems);

            // initially, the value is read from ref v. Later, if leasing fails, is obtained from CompareExchange.
            var value = Volatile.Read(ref v);

            var i = 0;
            for (; i < length & retries > 0; i++)
            {
                if ((value & mask) == 0)
                {
                    var nextValue = value | mask;

                    var result = Interlocked.CompareExchange(ref v, nextValue, value);
                    if (result == value)
                    {
                        return i;
                    }

                    // leasing attempt failed, retry with the recently obtained value from the last not checked index
                    value = result;
                    retries--;
                }

                mask <<= 1;
            }

            return -value;
        }

        public static void Release(ref Leasing leasing, int index, int continousItems, short lease) =>
            Release(ref leasing.masks[index + 1].Value, continousItems, lease);

        static void Release(ref long v, int continousItems, short lease)
        {
            Intelocked2.Xor(ref v, lease, continousItems);
        }
    }
}
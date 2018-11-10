using System.Runtime.CompilerServices;

namespace Thruster
{
    static class Util
    {
        ///<summary>
        /// Aligns <paramref name="value"/> to the next multiple of <paramref name="alignment"/>.
        /// If the value equals an alignment multiple then it's returned without changes.
        /// </summary>
        /// <remarks>
        /// No branching :D
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AlignToMultipleOf(this int value, int alignment)
        {
            return (value + (alignment - 1)) & ~(alignment - 1);
        }

        ///<summary>
        /// Aligns <paramref name="value"/> to the next multiple of <paramref name="alignment"/>.
        /// If the value equals an alignment multiple then it's returned without changes.
        /// </summary>
        /// <remarks>
        /// No branching :D
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long AlignToMultipleOf(this long value, long alignment)
        {
            return (value + (alignment - 1)) & ~(alignment - 1);
        }

        public const int CacheLineSize = 64;
    }
}
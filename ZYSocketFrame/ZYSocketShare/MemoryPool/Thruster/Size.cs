using System.Runtime.CompilerServices;

namespace Thruster
{
    interface ISize
    {
        int GetChunkSize();
        int GetChunkSizeLog();
    }

    struct Size4K : ISize
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetChunkSize() => 4 * 1024;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetChunkSizeLog() => 12;
    }

    struct Size8K : ISize
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetChunkSize() => 8 * 1024;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetChunkSizeLog() => 13;
    }

    struct Size16K : ISize
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetChunkSize() => 16 * 1024;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetChunkSizeLog() => 14;
    }
}
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Thruster
{
    class MemoryPoolImpl<T, TSize>
        where TSize : struct, ISize
    {
        internal const int ChunksPerBucket = Leasing.ChunksPerLeaseLong;

        readonly int processorCount;
        readonly T[] memory;
        readonly GCHandle gcHandle;
        Leasing leasing;

        bool disposed;

        public MemoryPoolImpl(int processorCount)
        {
            this.processorCount = processorCount;
            var allocSize = this.processorCount * ChunksPerBucket * default(TSize).GetChunkSize();
            leasing = new Leasing(processorCount);

            memory = new T[allocSize];
            gcHandle = GCHandle.Alloc(memory, GCHandleType.Pinned);
        }

        public IMemoryOwner<T> Rent(int size)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("MemoryPool has been disposed.");
            }

            var capacity = size.AlignToMultipleOf(default(TSize).GetChunkSize());
            var chunkCount = capacity >> default(TSize).GetChunkSizeLog();

            var bucketId = GetBucketId();
            var owner = Lease(bucketId, chunkCount, 1);
            if (owner != null)
            {
                return owner;
            }

            return LeaseSlowPath(bucketId, chunkCount);
        }

        Owner Lease(int bucketId, int chunkCount, int retries)
        {
            var index = bucketId;

            var lease = Leasing.Lease(ref leasing, index, chunkCount, retries);
            if (lease >= 0)
            {
                return new Owner(this, (byte)bucketId, (byte)chunkCount, (byte)lease);
            }

            return default;
        }

        void Release(Owner owner)
        {
            Leasing.Release(ref leasing, owner.BucketId, owner.ChunkCount, owner.Lease);
        }

        IMemoryOwner<T> LeaseSlowPath(int bucketId, int chunkCount)
        {
            var spin = new SpinWait();
            for (var i = 0; i < processorCount; i++)
            {
                spin.SpinOnce();
                bucketId = (bucketId + i) % processorCount;

                var owner = Lease(bucketId, chunkCount, 3);
                if (owner != null)
                {
                    return owner;
                }
            }

            // allocate if none is found
            return new Owner(new Memory<T>(new T[chunkCount * default(TSize).GetChunkSize()]));
        }

        public void Dispose(bool disposing)
        {
            disposed = true;
            if (gcHandle.IsAllocated)
            {
                gcHandle.Free();
            }
        }

        class Owner : IMemoryOwner<T>
        {
            MemoryPoolImpl<T, TSize> pool;
            public readonly byte BucketId;
            public readonly byte ChunkCount;
            public readonly byte Lease;

            public Owner(Memory<T> memory)
            {
                Memory = memory;
            }

            public Owner(MemoryPoolImpl<T, TSize> pool, byte bucketId, byte chunkCount, byte lease)
            {
                this.pool = pool;
                BucketId = bucketId;
                ChunkCount = chunkCount;
                Lease = lease;
                var offset = (bucketId * ChunksPerBucket + lease) * default(TSize).GetChunkSize();
                Memory = MemoryMarshal.CreateFromPinnedArray(pool.memory, offset, default(TSize).GetChunkSize());
            }

            public void Dispose()
            {
                pool?.Release(this);
                pool = null;
            }

            public Memory<T> Memory { get; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetBucketId()
        {
#if NETCOREAPP2_1
            return Thread.GetCurrentProcessorId() % processorCount;
#else
            return Environment.CurrentManagedThreadId % processorCount;
#endif
        }
    }
}
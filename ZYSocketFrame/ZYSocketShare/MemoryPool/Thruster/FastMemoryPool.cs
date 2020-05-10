﻿using System;
using System.Buffers;

namespace Thruster
{
    public class FastMemoryPool<T> : MemoryPool<T>
    {
        readonly int processorCount;
        readonly MemoryPoolImpl<T, Size4K> pool4K;
        volatile MemoryPoolImpl<T, Size8K>? pool8K;
        volatile MemoryPoolImpl<T, Size16K>? pool16K;

        readonly int maxBufferSize;

        public FastMemoryPool()
            :this(8 * default(Size16K).GetChunkSize())
        {

        }

        public FastMemoryPool(int maxBufferSize)
            : this(Math.Min(Environment.ProcessorCount, 64), maxBufferSize)

        {
        }

        internal FastMemoryPool(int processorCount,int maxBufferSize)
        {
            this.processorCount = processorCount;

            if (maxBufferSize > 2576 * 1024)
                this.maxBufferSize = 2576 * 1024;
            else
                this.maxBufferSize = maxBufferSize;

            pool4K = new MemoryPoolImpl<T, Size4K>(processorCount);
        }

        public override IMemoryOwner<T> Rent(int size = 0)
        {
            if (size <= 0)
            {
                size = 1;
            }

            if (size > MaxBufferSize)
                throw new System.IO.IOException($"the size > max buffer size: {MaxBufferSize}");



            var chunk4KCount = size >> default(Size4K).GetChunkSizeLog();

            if (chunk4KCount < 15)
            {
                return pool4K.Rent(size);
            }

            if (chunk4KCount < 30)
            {
                return Pool8K.Rent(size);
            }

            return Pool16K.Rent(size);
        }

        MemoryPoolImpl<T, Size8K> Pool8K
        {
            get
            {
                if (pool8K != null)
                {
                    return pool8K;
                }

                lock (pool4K)
                {
                    if (pool8K == null)
                    {
                        pool8K = new MemoryPoolImpl<T, Size8K>(processorCount);
                    }
                }

                return pool8K;
            }
        }

        MemoryPoolImpl<T, Size16K> Pool16K
        {
            get
            {
                if (pool16K != null)
                {
                    return pool16K;
                }

                lock (pool4K)
                {
                    if (pool16K == null)
                    {
                        pool16K = new MemoryPoolImpl<T, Size16K>(processorCount);
                    }
                }

                return pool16K;
            }
        }

        public override int MaxBufferSize => maxBufferSize;  //max 2576 * 1024;

        protected override void Dispose(bool disposing)
        {
            pool4K?.Dispose(disposing);
            pool8K?.Dispose(disposing);
            pool16K?.Dispose(disposing);
        }
    }
}
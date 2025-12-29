// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading;

namespace CSVTranslationLookup.Common.Text
{
    /// <summary>
    /// Provides optimized and cached access to <see cref="StringBuilder"/> instances.
    /// </summary>
    public static class StringBuilderCache
    {
        private const int DefaultCapacity = 0x10;

        // 16KB, don't cache larger instances
        private const int MaxCachedCapacity = 1024 * 16;

        [ThreadStatic]
        private static StringBuilder _perThread;   //  ONe cached instance per thread
        private static StringBuilder _shared;      //  One cached instance shared between threads

        /// <summary>
        /// Obtains a <see cref="StringBuilder"/> instance, which could be a recycled instance or a new one.
        /// </summary>
        /// <param name="capacity">The capcity to start the <see cref="StringBuilder"/> instance at.</param>
        /// <returns>The <see cref="StringBuilder"/> instance.</returns>
        public static StringBuilder Get(int capacity = DefaultCapacity)
        {
            // For very large capacity requesets, always create new instances (don't pollute cache)
            if(capacity > MaxCachedCapacity)
            {
                return new StringBuilder(capacity);
            }

            StringBuilder temp = _perThread;
            if (temp != null)
            {
                _perThread = null;

                // Clear the builder (resets length but preserves Capacity)
                temp.Clear();

                // Ensure capacity meets request (may grow if needed)
                if(temp.Capacity < capacity)
                {
                    temp.Capacity = capacity;
                }

                return temp;
            }

            temp = Interlocked.Exchange(ref _shared, null);

            if(temp != null)
            {
                // Clear the builder (resets length but preserves Capacity)
                temp.Clear();

                // Ensure capacity meets request (may grow if needed)
                if (temp.Capacity < capacity)
                {
                    temp.Capacity = capacity;
                }

                return temp;
            }

            // No cached instances available, create a new one
            return new StringBuilder(capacity);
        }

        /// <summary>
        /// Returns the contents of the given <see cref="StringBuilder"/> instance and immediatly recucles it.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> instance.</param>
        /// <returns>The string contents of the given <see cref="StringBuilder"/>.</returns>
        public static string GetStringAndRecycle(this StringBuilder builder)
        {
            string value = builder.ToString();
            builder.Recycle();
            return value;
        }

        /// <summary>
        /// Recycles the given <see cref="StringBuilder"/> instance if possible.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> instance to recycle.</param>
        public static void Recycle(this StringBuilder builder)
        {
            if (builder == null)
            {
                return;
            }

            // Don't cache oversized builders, let GC handle them
            if(builder.Capacity > MaxCachedCapacity)
            {
                return;
            }

            // Clear the builder before cachine
            builder.Clear();

            // Try to cache in thread-local slot first (fastest access)
            if (_perThread == null)
            {
                _perThread = builder;
                return;
            }

            // Thread-local slot full, try shared slot
            Interlocked.CompareExchange(ref _shared, builder, null);
        }
    }
}

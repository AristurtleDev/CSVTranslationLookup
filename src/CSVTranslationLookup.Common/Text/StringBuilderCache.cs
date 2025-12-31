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
    /// <remarks>
    /// Implements a two-tier caching strategy to reduce allocations:
    /// <list type="number">
    /// <item>Thread-local cache: One instance per thread for fastest access without synchronization</item>
    /// <item>Shared cache: One instance shared across threads using atomic operations</item>
    /// </list>
    /// Builders with capacity exceeding <c>16KB</c> are not cached to avoid excessive memory retention.
    /// Typical usage pattern: call <see cref="Get"/> to obtain a builder, use it, then call
    /// <see cref="GetStringAndRecycle"/> to retrieve the string and return the builder to the cache.
    /// </remarks>
    public static class StringBuilderCache
    {
        /// <summary>
        /// Default capacity for new StringBuilder instances (16 characters).
        /// </summary>
        private const int DefaultCapacity = 0x10;

        /// <summary>
        /// Maximum capacity for cached instances (16KB).
        /// Builders exceeding this size are not cached to prevent excessive memory retention.
        /// </summary>vs
        private const int MaxCachedCapacity = 1024 * 16;

        /// <summary>
        /// One cached StringBuilder instance per thread for fast access without synchronization.
        /// </summary>
        [ThreadStatic]
        private static StringBuilder s_perThread;

        /// <summary>
        /// One cached StringBuilder instance shared between threads using atomic operations.
        /// </summary>
        private static StringBuilder s_shared;

        /// <summary>
        /// Obtains a <see cref="StringBuilder"/> instance from the cache or creates a new one.
        /// </summary>
        /// <param name="capacity">The initial capacity for the <see cref="StringBuilder"/>. Defaults to 16 characters.</param>
        /// <returns>
        /// A <see cref="StringBuilder"/> instance, either from the cache (cleared and resized if necessary)
        /// or a newly allocated instance.
        /// </returns>
        /// <remarks>
        /// The method attempts to retrieve a cached instance in this order:
        /// <list type="number">
        /// <item>Thread-local cache (fastest, no synchronization)</item>
        /// <item>Shared cache (requires atomic operation)</item>
        /// <item>New allocation (if no cached instance available)</item>
        /// </list>
        /// Requests for capacity exceeding <see cref="MaxCachedCapacity"/> always allocate new instances
        /// to avoid polluting the cache with large builders.
        /// </remarks>
        public static StringBuilder Get(int capacity = DefaultCapacity)
        {
            // For very large capacity requests, always create new instances (don't pollute cache)
            if (capacity > MaxCachedCapacity)
            {
                return new StringBuilder(capacity);
            }

            // Try thread-local cache first (fastest path, no synchronization needed)
            StringBuilder temp = s_perThread;
            if (temp != null)
            {
                s_perThread = null;

                // Clear the builder (resets length but preserves Capacity)
                temp.Clear();

                // Ensure capacity meets request (may grow if needed)
                if (temp.Capacity < capacity)
                {
                    temp.Capacity = capacity;
                }

                return temp;
            }

            // Thread-local cache empty, try shared cache (requires atomic operation)
            temp = Interlocked.Exchange(ref s_shared, null);

            if (temp != null)
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
        /// Returns the string contents of the <see cref="StringBuilder"/> and immediately recycles it to the cache.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> instance to extract and recycle.</param>
        /// <returns>The string contents of the <see cref="StringBuilder"/>.</returns>
        /// <remarks>
        /// This is a convenience method combining <see cref="StringBuilder.ToString"/> and <see cref="Recycle"/>.
        /// Use this when you're done building the string and want to return the builder to the cache in one operation.
        /// </remarks>
        public static string GetStringAndRecycle(this StringBuilder builder)
        {
            string value = builder.ToString();
            builder.Recycle();
            return value;
        }

        /// <summary>
        /// Recycles the <see cref="StringBuilder"/> instance to the cache if possible.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> instance to recycle.</param>
        /// <remarks>
        /// The builder is recycled using this priority:
        /// <list type="number">
        /// <item>Thread-local cache if empty (fastest)</item>
        /// <item>Shared cache if empty (requires atomic operation)</item>
        /// <item>Discarded if both caches are full or builder exceeds <see cref="MaxCachedCapacity"/></item>
        /// </list>
        /// Oversized builders are not cached to prevent excessive memory retention.
        /// Always call this method or <see cref="GetStringAndRecycle"/> when done with a cached builder.
        /// </remarks>
        public static void Recycle(this StringBuilder builder)
        {
            if (builder == null)
            {
                return;
            }

            // Don't cache oversized builders, let GC handle them
            if (builder.Capacity > MaxCachedCapacity)
            {
                return;
            }

            // Clear the builder before caching
            builder.Clear();

            // Try to cache in thread-local slot first (fastest access)
            if (s_perThread == null)
            {
                s_perThread = builder;
                return;
            }

            // Thread-local slot full, try shared slot (atomic operation ensures thread safety)
            Interlocked.CompareExchange(ref s_shared, builder, null);
            // If shared slot is also full, the builder is simply discarded (GC will collect it)
        }
    }
}

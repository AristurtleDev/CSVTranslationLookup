// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Text;

namespace CSVTranslationLookup.Common.Text
{
    /// <summary>
    /// Provides optimized and cached access to <see cref="StringBuilder"/> instances.
    /// </summary>
    public static class StringBuilderCache
    {
        private const int DefaultCapacity = 0x10;

        [ThreadStatic]
        private static StringBuilder? _perThread;   //  One cached instance per thread
        private static StringBuilder? _shared;      //  One cached instance shared between threads.

        /// <summary>
        /// Obtains a <see cref="StringBuilder"/> instance, which could be a recycled insance or a new one.
        /// </summary>
        /// <param name="capacity">The capacity to start the <see cref="StringBuilder"/> instance at.</param>
        /// <returns></returns>
        public static StringBuilder Get(int capacity = DefaultCapacity)
        {
            StringBuilder? temp = _perThread;
            if (temp is not null)
            {
                _perThread = null;
                temp.Length = 0;
                return temp;
            }

            temp = Interlocked.Exchange(ref _shared, null);
            if (temp is null)
            {
                return new StringBuilder(capacity);
            }
            temp.Length = 0;
            return temp;
        }

        /// <summary>
        /// Return the contents of the given <see cref="StringBuilder"/> instance and immediatly recycles it.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="StringBuilder"/> instance to get the contents of and then recycle.
        /// </param>
        /// <returns>The string contents of the given <see cref="StringBuilder"/>.</returns>
        public static string GetStringAndRecycle(this StringBuilder builder)
        {
            string value = builder.ToString();
            Recycle(builder);
            return value;
        }

        /// <summary>
        /// Recycles the given <see cref="StringBuilder"/> instance if possible
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> instance to recycle.</param>
        public static void Recycle(StringBuilder builder)
        {
            if (builder is null) return;

            if (_perThread is null)
            {
                _perThread = builder;
                return;
            }

            Interlocked.CompareExchange(ref _shared, builder, null);

        }

    }
}

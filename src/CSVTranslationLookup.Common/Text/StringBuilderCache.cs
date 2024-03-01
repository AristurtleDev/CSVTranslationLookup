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
            StringBuilder temp = _perThread;
            if (temp != null)
            {
                _perThread = null;
                temp.Length = 0;
                return temp;
            }

            temp = Interlocked.Exchange(ref _shared, null);
            if (temp == null)
            {
                return new StringBuilder(capacity);
            }
            temp.Length = 0;
            return temp;
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

            if (_perThread == null)
            {
                _perThread = builder;
                return;
            }

            Interlocked.CompareExchange(ref _shared, builder, null);
        }
    }
}

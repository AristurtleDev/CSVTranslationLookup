// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using CSVTranslationLookup.Common.Text;

namespace CSVTranslationLookup.Common.IO
{
    /// <summary>
    /// Provides helper extensions for the <see cref="StringReader"/> class.
    /// </summary>
    public static class StringReaderExtensions
    {
        /// <summary>
        /// Reads from the <see cref="StringReader"/> up until the specified character.
        /// </summary>
        /// <param name="reader">The <see cref="StringReader"/> instance to read from.</param>
        /// <param name="character">The character to read up to.</param>
        /// <returns>The strintg contents of the <see cref="StringReader"/> up to the specified character.</returns>
        public static string ReadTo(this StringReader reader, char character)
        {
            StringBuilder buffer = StringBuilderCache.Get();
            bool isNewLine = false;
            while (reader.Peek() != -1 && reader.Peek() != character)
            {
                char c = (char)reader.Read();

                if (IsNewLine(c))
                {
                    if (!isNewLine)
                    {
                        buffer.AppendLine();
                        isNewLine = true;
                    }

                    continue;
                }

                if (isNewLine && !IsNewLine(c))
                {
                    isNewLine = false;
                }

                buffer.Append(c);
            }
            return buffer.GetStringAndRecycle();
        }

        private static bool IsNewLine(char c) => c == '\n' || c == '\r';
    }
}

// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using CSVTranslationLookup.Common.Text;

namespace CSVTranslationLookup.Common.IO
{
    /// <summary>
    /// Provides extension methods for the <see cref="StringReader"/> class.
    /// </summary>
    public static class StringReaderExtensions
    {
        /// <summary>
        /// Reads characters from the <see cref="StringReader"/> until the specified character is encountered.
        /// </summary>
        /// <param name="reader">The <see cref="StringReader"/> instance to read from.</param>
        /// <param name="character">The character to read up to (not included in the result).</param>
        /// <returns>
        /// A string containing all characters read up to (but not including) the specified character,
        /// or all remaining characters if the specified character is not found.
        /// </returns>
        /// <remarks>
        /// Consecutive newline characters (\r and \n in any combination) are consolidated into a single
        /// line break in the output. This normalization handles different line ending formats (Windows CRLF,
        /// Unix LF, legacy Mac CR) consistently.gkds
        /// </remarks>
        public static string ReadTo(this StringReader reader, char character)
        {
            StringBuilder buffer = StringBuilderCache.Get();
            bool isNewLine = false;
            while (reader.Peek() != -1 && reader.Peek() != character)
            {
                char c = (char)reader.Read();

                if (IsNewLine(c))
                {
                    // Consolidate consecutive newline characters into a single line break
                    if (!isNewLine)
                    {
                        buffer.AppendLine();
                        isNewLine = true;
                    }

                    continue;
                }

                // Reset newline tracking when we encounter a non-newline character
                if (isNewLine && !IsNewLine(c))
                {
                    isNewLine = false;
                }

                buffer.Append(c);
            }
            return buffer.GetStringAndRecycle();
        }

        /// <summary>
        /// Determines whether the specified character is a newline character.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>
        /// <see langword="true"/> if the character is \n (line feed) or \r (carriage return);
        /// otherwise, <see langword="false"/>.
        /// </returns>vs
        private static bool IsNewLine(char c) => c == '\n' || c == '\r';
    }
}

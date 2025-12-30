// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using CSVTranslationLookup.Common.Text;

namespace CSVTranslationLookup.Common.Tokens
{
    /// <summary>
    /// Provides methods for tokenizing a given string based on RFC4180 CSV specifications.
    /// </summary>
    public static class Tokenizer
    {
        public static Token[] Tokenize(string input, string fileName, int lineNumber, char delimiter = ',', char quote = '"')
        {
            if (string.IsNullOrEmpty(input))
            {
                return new Token[] { new Token(TokenType.EndOfRecord) { FileName = fileName, LineNumber = lineNumber } };
            }

            // Pre-allocate for common case (most CSV rows are probably 2 to 10 columns)
            var tokens = new List<Token>(capacity: 8);
            int position = 0;
            int length = input.Length;

            while (position < length)
            {
                // Skip leading whitespace
                position = SkipWhitespace(input, position, length, delimiter);

                if (position >= length)
                {
                    // End of input
                    tokens.Add(new Token(TokenType.EndOfRecord, string.Empty) { FileName = fileName, LineNumber = lineNumber });
                    break;
                }

                char currentChar = input[position];

                // Check for delimiter (empty field)
                if (currentChar == delimiter)
                {
                    tokens.Add(new Token(TokenType.Token, string.Empty) { FileName = fileName, LineNumber = lineNumber });

                    // Skip delimiter
                    position++;
                    continue;
                }

                // Check for quoted field
                if (currentChar == quote)
                {
                    string value = ReadQuotedField(input, ref position, length, quote);

                    // Skip trailing whitespace
                    position = SkipWhitespace(input, position, length, delimiter);

                    // Determine token type
                    if (position >= length)
                    {
                        tokens.Add(new Token(TokenType.EndOfRecord, value) { FileName = fileName, LineNumber = lineNumber });
                        break;
                    }
                    else if (position < length && input[position] == delimiter)
                    {
                        tokens.Add(new Token(TokenType.Token, value) { FileName = fileName, LineNumber = lineNumber });

                        // Skip delimiter
                        position++;
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Token, value) { FileName = fileName, LineNumber = lineNumber });
                    }
                    continue;
                }

                // Read uquoted field
                string unquotedvalue = ReadUnquotedField(input, ref position, length, delimiter);

                // Skip trailing whitespace
                position = SkipWhitespace(input, position, length, delimiter);

                // Determine token type
                if (position >= length)
                {
                    tokens.Add(new Token(TokenType.EndOfRecord, unquotedvalue) { FileName = fileName, LineNumber = lineNumber });
                    break;
                }
                else if (position < length && input[position] == delimiter)
                {
                    tokens.Add(new Token(TokenType.Token, unquotedvalue) { FileName = fileName, LineNumber = lineNumber });

                    // Skip delimiter
                    position++;
                }
                else
                {
                    tokens.Add(new Token(TokenType.Token, unquotedvalue) { FileName = fileName, LineNumber = lineNumber });
                }
            }

            // If we ended on a delimiter, add empty EndOfRecord token
            if (position > 0 && position <= length && input[position - 1] == delimiter)
            {
                tokens.Add(new Token(TokenType.EndOfRecord, string.Empty) { FileName = fileName, LineNumber = lineNumber });
            }

            return tokens.ToArray();
        }

        /// <summary>
        /// Skips whitespace characters (space and tab only, not newlines).
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="position">The current position.</param>
        /// <param name="length">The length of the input string.</param>
        /// <param name="delimiter">The delimiter character to stop at.</param>
        /// <returns>The new position after skipping whitespace.</returns>
        private static int SkipWhitespace(string input, int position, int length, char delimiter)
        {
            while (position < length)
            {
                char c = input[position];
                if (c != delimiter && (c == ' ' || c == '\t'))
                {
                    position++;
                }
                else
                {
                    break;
                }
            }

            return position;
        }

        /// <summary>
        /// Reads a quoted field from the input string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="position">The current position (will be updated)</param>
        /// <param name="length">The length of the input string.</param>
        /// <param name="quote">The quote character.</param>
        /// <returns>The content of the quoted field (without surrounding quotes).</returns>
        private static string ReadQuotedField(string input, ref int position, int length, char quote)
        {
            // Skip opening quote
            position++;

            StringBuilder buffer = StringBuilderCache.Get();
            bool insideQutoes = true;

            while (position < length && insideQutoes)
            {
                char c = input[position];

                if (c == quote)
                {
                    // Check for escaped quote (two consecutive quotes)
                    if (position + 1 < length && input[position + 1] == quote)
                    {
                        // Escaped quote, add single quote to output
                        buffer.Append(quote);

                        // Skip both quotes
                        position += 2;
                    }
                    else
                    {
                        // End of quoted field
                        // Skip closing quote
                        position++;
                        insideQutoes = false;
                    }
                }
                else
                {
                    buffer.Append(c);
                    position++;
                }
            }

            return buffer.GetStringAndRecycle();
        }

        /// <summary>
        /// Reads an unquoted field form the input string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="position">The currnet position (will be updated).</param>
        /// <param name="length">The length of the input string.</param>
        /// <param name="delimiter">The delimiter character.</param>
        /// <returns>The content of the unquoted field (trimmed).</returns>
        private static string ReadUnquotedField(string input, ref int position, int length, char delimiter)
        {
            int startPosition = position;

            // Find end of field (delimiter or end of string)
            while (position < length && input[position] != delimiter)
            {
                position++;
            }

            // Extract substring and trim
            int fieldLength = position - startPosition;
            if (fieldLength == 0)
            {
                return string.Empty;
            }

            string value = input.Substring(startPosition, fieldLength);
            return value.Trim();
        }
    }
}

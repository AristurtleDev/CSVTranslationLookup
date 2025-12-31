// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using CSVTranslationLookup.Common.Text;

namespace CSVTranslationLookup.Common.Tokens
{
    /// <summary>
    /// Provides methods for tokenizing strings based on RFC4180 CSV specifications.
    /// </summary>
    /// <remarks>
    /// Implements RFC4180 CSV parsing with support for:
    /// <list type="bullet">
    /// <item>Quoted fields containing delimiters and newlines</item>
    /// <item>Escaped quotes (two consecutive quote characters)</item>
    /// <item>Leading and trailing whitespace handling</item>
    /// <item>Empty fields</item>
    /// <item>Custom delimiters and quote characters</item>
    /// </list>
    /// Each token includes metadata about its source file and line number for diagnostics.
    /// </remarks>
    public static class Tokenizer
    {
        /// <summary>
        /// Tokenizes a CSV row string into an array of tokens.
        /// </summary>
        /// <param name="input">The CSV row string to tokenize.</param>
        /// <param name="fileName">The absolute path to the source file for metadata.</param>
        /// <param name="lineNumber">The line number in the source file for metadata.</param>
        /// <param name="delimiter">The character that represents a field delimiter. Defaults to <c>,</c> (comma).</param>
        /// <param name="quote">The character that represents the start and end of a quoted field. Defaults to <c>"</c> (double quote).</param>
        /// <returns>
        /// An array of <see cref="Token"/> instances representing the fields in the CSV row.
        /// Each token includes file name and line number metadata.
        /// </returns>
        /// <remarks>
        /// The tokenizer handles several cases:
        /// <list type="bullet">
        /// <item>Empty input: Returns a single EndOfRecord token</item>
        /// <item>Empty fields (consecutive delimiters): Creates tokens with empty string content</item>
        /// <item>Quoted fields: Removes surrounding quotes and unescaped doubled quotes</item>
        /// <item>Unquoted fields: Trims leading and trailing whitespace</item>
        /// <item>Trailing delimiter: Adds an empty EndOfRecord token</item>
        /// </list>
        /// The last token in each row is marked with <see cref="TokenType.EndOfRecord"/>.
        /// </remarks>
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
                // Skip leading whitespace (spaces and tabs, not newlines)
                position = SkipWhitespace(input, position, length, delimiter);

                if (position >= length)
                {
                    // Reached end of input
                    tokens.Add(new Token(TokenType.EndOfRecord, string.Empty) { FileName = fileName, LineNumber = lineNumber });
                    break;
                }

                char currentChar = input[position];

                // Check for delimiter (indicates empty field)
                if (currentChar == delimiter)
                {
                    tokens.Add(new Token(TokenType.Token, string.Empty) { FileName = fileName, LineNumber = lineNumber });

                    // Skip delimiter and continue to next field
                    position++;
                    continue;
                }

                // Check for quoted field
                if (currentChar == quote)
                {
                    string value = ReadQuotedField(input, ref position, length, quote);

                    // Skip trailing whitespace after closing quote
                    position = SkipWhitespace(input, position, length, delimiter);

                    // Determine token type based on what follows
                    if (position >= length)
                    {
                        // End of row
                        tokens.Add(new Token(TokenType.EndOfRecord, value) { FileName = fileName, LineNumber = lineNumber });
                        break;
                    }
                    else if (position < length && input[position] == delimiter)
                    {
                        // More fields follow
                        tokens.Add(new Token(TokenType.Token, value) { FileName = fileName, LineNumber = lineNumber });

                        // Skip delimiter
                        position++;
                    }
                    else
                    {
                        // Malformed (no delimiter after quoted field), treat as regular token
                        tokens.Add(new Token(TokenType.Token, value) { FileName = fileName, LineNumber = lineNumber });
                    }
                    continue;
                }

                // Read uquoted field
                string unquotedvalue = ReadUnquotedField(input, ref position, length, delimiter);

                // Skip trailing whitespace
                position = SkipWhitespace(input, position, length, delimiter);

                // Determine token type based on what follows
                if (position >= length)
                {
                    // End of row
                    tokens.Add(new Token(TokenType.EndOfRecord, unquotedvalue) { FileName = fileName, LineNumber = lineNumber });
                    break;
                }
                else if (position < length && input[position] == delimiter)
                {
                    // More fields follow
                    tokens.Add(new Token(TokenType.Token, unquotedvalue) { FileName = fileName, LineNumber = lineNumber });

                    // Skip delimiter
                    position++;
                }
                else
                {
                    // Malformed (unexpected characters), treat as regular token
                    tokens.Add(new Token(TokenType.Token, unquotedvalue) { FileName = fileName, LineNumber = lineNumber });
                }
            }

            // If row ended on a delimiter, add empty EndOfRecord token
            if (position > 0 && position <= length && input[position - 1] == delimiter)
            {
                tokens.Add(new Token(TokenType.EndOfRecord, string.Empty) { FileName = fileName, LineNumber = lineNumber });
            }

            return tokens.ToArray();
        }

        /// <summary>
        /// Skips whitespace characters from the current position.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="position">The current position in the input string.</param>
        /// <param name="length">The length of the input string.</param>
        /// <param name="delimiter">The delimiter character to stop at.</param>
        /// <returns>The new position after skipping all whitespace.</returns>
        /// <remarks>
        /// Only skips spaces and tabs, not newlines. Stops when encountering a delimiter,
        /// non-whitespace character, or end of string.
        /// </remarks>
        private static int SkipWhitespace(string input, int position, int length, char delimiter)
        {
            while (position < length)
            {
                char c = input[position];

                // Only skip spaces and tabs (not newlines), and stop at delimiters
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
        /// <param name="position">The current position (will be updated to position after closing quote).</param>
        /// <param name="length">The length of the input string.</param>
        /// <param name="quote">The quote character.</param>
        /// <returns>
        /// The content of the quoted field with surrounding quotes removed and escaped quotes (doubled quotes) unescaped.
        /// </returns>
        /// <remarks>
        /// Handles RFC4180 quote escaping: two consecutive quote characters within a quoted field
        /// represent a single quote in the output. For example, the field <c>"He said ""Hello"""</c>
        /// returns <c>He said "Hello"</c>.
        /// </remarks>
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
                    // Regular character within quoted field
                    buffer.Append(c);
                    position++;
                }
            }

            return buffer.GetStringAndRecycle();
        }
        /// <summary>
        /// Reads an unquoted field from the input string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="position">The current position (will be updated to position after field).</param>
        /// <param name="length">The length of the input string.</param>
        /// <param name="delimiter">The delimiter character that marks the end of the field.</param>
        /// <returns>
        /// The content of the unquoted field with leading and trailing whitespace removed.
        /// </returns>
        /// <remarks>
        /// Reads characters until a delimiter or end of string is encountered, then trims
        /// the result. Returns an empty string for zero-length fields.
        /// </remarks>
        private static string ReadUnquotedField(string input, ref int position, int length, char delimiter)
        {
            int startPosition = position;

            // Find end of field (delimiter or end of string)
            while (position < length && input[position] != delimiter)
            {
                position++;
            }

            // Extract substring and trim whitespace
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

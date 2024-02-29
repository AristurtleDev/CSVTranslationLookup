// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using CSVTranslationLookup.Common.Text;
using CSVTranslationLookup.Common.Tokens;

namespace CSVTranslationLookup.Common.IO
{
    /// <summary>
    /// Represents a thin <see cref="StringReader"/> wrapper that reads token words from an input string.
    /// </summary>
    public class TokenReader : IDisposable
    {
        private readonly StringReader _reader;
        private readonly char _delimiter;
        private readonly char _quote;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenReader"/> class.
        /// </summary>
        /// <param name="input">The input string to read tokens from.</param>
        /// <param name="delimiter">The character that represents a delimiter.</param>
        /// <param name="quote">The character that represents the start of a quoted token.</param>
        public TokenReader(string input, char delimiter = ',', char quote = '"')
        {
            _reader = new StringReader(input);
            _delimiter = delimiter;
            _quote = quote;
        }

        ~TokenReader() => Dispose(false);

        /// <summary>
        /// Reads the next token.
        /// </summary>
        /// <returns>The token that was read.</returns>
        public Token NextToken()
        {
            ValidateDisposed(_isDisposed);

            SkipWhitespace();
            string? result = null;
            int c = _reader.Peek();

            if (IsDelimiter(c))
            {
                _ = _reader.Read();
                return new Token(TokenType.Token);
            }

            if (IsQuotedCharacter(c))
            {
                result = ReadQuoted();
                SkipWhitespace();

                if (IsEndOfStream(_reader.Peek()))
                {
                    return new Token(TokenType.EndOfRecord, result);
                }

                if (IsDelimiter(_reader.Peek()))
                {
                    _ = _reader.Read();
                }

                return new Token(TokenType.Token, result);
            }

            if (IsEndOfStream(c))
            {
                return new Token(TokenType.EndOfRecord);
            }

            result = _reader.ReadTo(_delimiter).Trim();
            SkipWhitespace();
            if (IsEndOfStream(_reader.Peek()))
            {
                return new Token(TokenType.EndOfRecord, result);
            }

            if (IsDelimiter(_reader.Peek()))
            {
                _ = _reader.Read();
            }

            return new Token(TokenType.Token, result);
        }

        /// <summary>
        /// Reads a quoted token.
        /// </summary>
        /// <returns>The string contents of the quoted token.</returns>
        private string ReadQuoted()
        {
            _reader.Read();
            string result = _reader.ReadTo(_quote);
            _ = _reader.Read();

            if (!IsQuotedCharacter(_reader.Peek()))
            {
                return result;
            }

            StringBuilder buffer = StringBuilderCache.Get();
            do
            {
                buffer.Append((char)_reader.Read());
                buffer.Append(_reader.ReadTo(_quote));
                _ = _reader.Read();
            } while (IsQuotedCharacter(_reader.Peek()));

            return buffer.GetStringAndRecycle();
        }

        /// <summary>
        /// Skips all whitespace by advancing hte reader until a non-whitespace character is found.
        /// </summary>
        private void SkipWhitespace()
        {
            while (IsWhitespace(_reader.Peek()))
            {
                _ = _reader.Read();
            }
        }

        /// <summary>
        /// Returns a value that indicates wehther the specified character matches the delimiter character.
        /// </summary>
        /// <param name="c">The 32-bit signed integer value representing the character to check.</param>
        /// <returns>true if the character is equal tot he delimiter character; otherwise, false.</returns>
        private bool IsDelimiter(int c)
        {
            return c == _delimiter;
        }

        /// <summary>
        /// Returns a value that indicats whether the specified character matches the quote character.
        /// </summary>
        /// <param name="c">The 32-bit signed integer value representing the character to check.</param>
        /// <returns>true if the character is equal to the quote chracter; otherwise, false.</returns>
        private bool IsQuotedCharacter(int c)
        {
            return c == _quote;
        }

        /// <summary>
        /// Returns a value that indicates whether the specified character is a whitespace character.
        /// </summary>
        /// <param name="c">The 32-bit signed integer value representing the character to check.</param>
        /// <returns>true if the character is a whitespae character; otherwise, false.</returns>
        private bool IsWhitespace(int c)
        {
            return !IsDelimiter(c) && (c == ' ' || c == '\t');
        }

        /// <summary>
        /// Returns a value that indicates whether the specified character represents the end of stream.
        /// </summary>
        /// <param name="c">The 32-bit signed integer value representing the character to check.</param>
        /// <returns>true if the character represents the end of stream, otherwise, false.</returns>
        private bool IsEndOfStream(int c)
        {
            return c == -1;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            _reader.Dispose();
            _isDisposed = true;
        }

        private static void ValidateDisposed(bool disposed)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(TokenReader));
            }
        }
    }
}

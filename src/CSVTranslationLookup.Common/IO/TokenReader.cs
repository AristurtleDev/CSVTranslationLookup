// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using CSVTranslationLookup.Common.Text;
using CSVTranslationLookup.Common.Tokens;

namespace CSVTranslationLookup.Common.IO
{
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
        /// <param name="delimiter">The character the represents a delimiter.</param>
        /// <param name="quote">The character that represents the start of a quoted token.</param>
        public TokenReader(string input, char delimiter = ',', char quote = '"')
        {
            _reader = new StringReader(input);
            _delimiter = delimiter;
            _quote = quote;
        }

        ~TokenReader() => Dispose(false);

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

            if (IsQuoteCharacter(c))
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
        /// Reads a quoted token
        /// </summary>
        /// <returns>The string contents of the quoted token read.</returns>
        private string ReadQuoted()
        {
            _reader.Read();
            string result = _reader.ReadTo(_quote);
            _ = _reader.Read();

            if (!IsQuoteCharacter(_reader.Peek()))
            {
                return result;
            }

            StringBuilder buffer = StringBuilderCache.Get();
            do
            {
                buffer.Append((char)_reader.Read());
                buffer.Append(_reader.ReadTo(_quote));
                _ = _reader.Read();
            } while (IsQuoteCharacter(_reader.Peek()));

            return buffer.GetStringAndRecycle();
        }

        /// <summary>
        /// Skips all white space by advancing the reader until a non-whitespace character is found.
        /// </summary>
        private void SkipWhitespace()
        {
            while (IsWhitespace(_reader.Peek()))
            {
                _ = _reader.Read();
            }
        }

        /// <summary>
        /// Returns a value that indicates whether the specified character matches the delimiter character.
        /// </summary>
        /// <param name="c">The 32-bit signed integer value representing the character to check.</param>
        /// <returns>true if the character is equal to the delimiter character; otherwise, false.</returns>
        private bool IsDelimiter(int c) => c == _delimiter;

        /// <summary>
        /// Returns a value that indiacates whether the specified character matches the quote character.
        /// </summary>
        /// <param name="c">The 32-bit signed integer value representing the character to check.</param>
        /// <returns>true if the character is equal to the quote character; otherwise, false.</returns>
        private bool IsQuoteCharacter(int c) => c == _quote;

        /// <summary>
        /// Return a value that indicates if the specific character is a whitespace character.
        /// </summary>
        /// <param name="c">The 32-bit signed integer value representing the character to check.</param>
        /// <returns>true if the character is a whitespace character; otherwise, false.</returns>
        private bool IsWhitespace(int c) => !IsDelimiter(c) && (c == ' ' || c == '\t');

        /// <summary>
        /// Returns a value that indicates if the specified character represents the end of stream.
        /// </summary>
        /// <param name="c">The 32-bit signed integer value representing hte character to check.</param>
        /// <returns>true if the character represents the end of stream; otherwise, false.</returns>
        private bool IsEndOfStream(int c) => c == -1;

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

            if (disposing)
            {

            }

            _reader.Dispose();
            _isDisposed = true;
        }

        private static void ValidateDisposed(bool disposed)
        {
            if(disposed)
            {
                throw new ObjectDisposedException(nameof(TokenReader));
            }    
        }
    }
}

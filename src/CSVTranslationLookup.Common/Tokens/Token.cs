// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace CSVTranslationLookup.Common.Tokens
{
    /// <summary>
    /// Represents a single token (cel) in a CSV row.,
    /// </summary>
    public class Token : IEquatable<Token>
    {
        /// <summary>
        /// Gets the type of this token.
        /// </summary>
        public TokenType TokenType { get; }

        /// <summary>
        /// Gets the string contents of this token.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Gets the absolute path to the file this token is from.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets the line number within the file this token is at.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> class.
        /// </summary>
        /// <param name="tokenType">The type of token.</param>
        /// <param name="content">The string contents of the token.</param>
        public Token(TokenType tokenType, string content = null)
        {
            TokenType = tokenType;
            Content = content;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is Token other && Equals(other);
        }

        /// <inheritdoc />
        public bool Equals(Token other)
        {
            return other != null &&
                   TokenType == other.TokenType &&
                   Content == other.Content;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int prime = 13;
                int hash = 27;
                hash = (prime * hash) + TokenType.GetHashCode();
                hash = (prime * hash) + Content.GetHashCode();
                hash = prime * hash + FileName.GetHashCode();
                hash = prime * hash + LineNumber.GetHashCode();
                return hash;
            }
        }
    }
}

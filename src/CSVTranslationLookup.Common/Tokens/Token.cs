// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace CSVTranslationLookup.Common.Tokens
{
    /// <summary>
    /// Represents a single token (cell) in a CSV row.
    /// </summary>
    /// <remarks>
    /// A token corresponds to a single field in a CSV file, with metadata about its type,
    /// content, source file, and line number.
    /// </remarks>
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
        /// Gets or sets the line number within the file where this token appears.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> class.
        /// </summary>
        /// <param name="tokenType">The type of token.</param>
        /// <param name="content">The string contents of the token. Can be <see langword="null"/> for empty tokens.</param>
        public Token(TokenType tokenType, string content = null)
        {
            TokenType = tokenType;
            Content = content;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current token.
        /// </summary>
        /// <param name="obj">The object to compare with the current token.</param>
        /// <returns>
        /// <see langword="true"/> if the specified object is a <see cref="Token"/> and has the same
        /// <see cref="TokenType"/> and <see cref="Content"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// File location metadata (<see cref="FileName"/> and <see cref="LineNumber"/>) is not
        /// considered in equality comparisons.
        /// </remarks>
        public override bool Equals(object obj)
        {
            return obj is Token other && Equals(other);
        }

        /// <summary>
        /// Determines whether the specified token is equal to the current token.
        /// </summary>
        /// <param name="other">The token to compare with the current token.</param>
        /// <returns>
        /// <see langword="true"/> if the specified token has the same <see cref="TokenType"/>
        /// and <see cref="Content"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// File location metadata (<see cref="FileName"/> and <see cref="LineNumber"/>) is not
        /// considered in equality comparisons.
        /// </remarks>
        public bool Equals(Token other)
        {
            return other != null &&
                   TokenType == other.TokenType &&
                   Content == other.Content;
        }

        /// <summary>
        /// Returns the hash code for this token.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer hash code based on <see cref="TokenType"/>, <see cref="Content"/>,
        /// <see cref="FileName"/>, and <see cref="LineNumber"/>.
        /// </returns>
        /// <remarks>
        /// The hash code includes file location metadata even though equality comparisons do not.
        /// This allows tokens from different file locations to hash differently while still being
        /// considered equal based on content.
        /// </remarks>
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

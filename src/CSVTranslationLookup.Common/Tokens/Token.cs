// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CSVTranslationLookup.Common.Tokens
{
    /// <summary>
    /// Represents a single token (cel) in a CSV row. 
    /// </summary>
    /// <param name="tokenType">The type of token.</param>
    /// <param name="content">The token contents</param>
    public class Token(TokenType tokenType, string? content = null) : IEquatable<Token>
    {
        /// <summary>
        /// Gets the type of this token.
        /// </summary>
        public TokenType TokenType => tokenType;

        /// <summary>
        /// Gets the string contents of this token.
        /// </summary>
        public string? Content => content;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Token other && Equals(other);

        /// <inheritdoc />
        public bool Equals(Token? other) => other is not null && TokenType == other.TokenType && Content == other.Content;

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(TokenType, Content);

    }
}

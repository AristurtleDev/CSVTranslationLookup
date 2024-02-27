// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CSVTranslationLookup.Common.Tokens
{
    public class TokenizedRow(int index, Token?[] tokens) : IEquatable<TokenizedRow>
    {
        /// <summary>
        /// Gets the index of this row.
        /// </summary>
        public int Index => index;

        /// <summary>
        ///     Gets all tokens in this row.
        /// </summary>
        public Token?[] Tokens => tokens;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is TokenizedRow other && Equals(other);

        /// <inheritdoc />
        public bool Equals(TokenizedRow? other) => other is not null && Index == other.Index && Tokens.SequenceEqual(other.Tokens);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Index, Tokens);
    }
}

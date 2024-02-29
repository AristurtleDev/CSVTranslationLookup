// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace CSVTranslationLookup.Common.Tokens
{
    /// <summary>
    /// Represents a row of tokens from a CSV file.
    /// </summary>
    public class TokenizedRow : IEquatable<TokenizedRow>
    {
        /// <summary>
        /// Gets the path to the file this row is from.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets the zero based index of this row in the file.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets all tokens in this row.
        /// </summary>
        public Token?[] Tokens { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenizedRow"/> class.
        /// </summary>
        /// <param name="fileName">The absolute path to the file this tokenized row is from.</param>
        /// <param name="index">The zero-based index of the row in the file this row is at.</param>
        /// <param name="tokens">The tokens that are in this row</param>
        public TokenizedRow(string fileName, int index, Token?[] tokens)
        {
            FileName = fileName;
            Index = index;
            Tokens = tokens;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is TokenizedRow other && Equals(other);
        }

        /// <inheritdoc />
        public bool Equals(TokenizedRow? other)
        {
            return other != null &&
                   Index == other.Index &&
                   FileName == other.FileName &&
                   Tokens.SequenceEqual(other.Tokens);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 27;
                hash = (13 * hash) + FileName.GetHashCode();
                hash = (13 * hash) + Index.GetHashCode();
                for (int i = 0; i < Tokens.Length; i++)
                {
                    hash = (13 * hash) + (Tokens[i]?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }
    }
}

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
    /// <remarks>
    /// Each tokenized row corresponds to a single line in a CSV file (or multiple lines if the
    /// row contains multi-line quoted fields). The row maintains its source file location and
    /// zero-based index for tracking purposes.
    /// </remarks>
    public class TokenizedRow : IEquatable<TokenizedRow>
    {
        /// <summary>
        /// Gets the path to the file this row is from.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets the zero-based index of this row in the file.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets all tokens in this row.
        /// </summary>
        public Token[] Tokens { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenizedRow"/> class.
        /// </summary>
        /// <param name="fileName">The absolute path to the file this tokenized row is from.</param>
        /// <param name="index">The zero-based index of the row in the file.</param>
        /// <param name="tokens">The tokens that are in this row.</param>
        public TokenizedRow(string fileName, int index, Token[] tokens)
        {
            FileName = fileName;
            Index = index;
            Tokens = tokens;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current tokenized row.
        /// </summary>
        /// <param name="obj">The object to compare with the current tokenized row.</param>
        /// <returns>
        /// <see langword="true"/> if the specified object is a <see cref="TokenizedRow"/> with the same
        /// file name, index, and token sequence; otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is TokenizedRow other && Equals(other);
        }

        /// <summary>
        /// Determines whether the specified tokenized row is equal to the current tokenized row.
        /// </summary>
        /// <param name="other">The tokenized row to compare with the current tokenized row.</param>
        /// <returns>
        /// <see langword="true"/> if the specified tokenized row has the same file name, index,
        /// and token sequence; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Token sequence comparison uses <see cref="Enumerable.SequenceEqual"/>,
        /// which checks both token count and individual token equality.
        /// </remarks>
        public bool Equals(TokenizedRow other)
        {
            return other != null &&
                   Index == other.Index &&
                   FileName == other.FileName &&
                   Tokens.SequenceEqual(other.Tokens);
        }

        /// <summary>
        /// Returns the hash code for this tokenized row.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer hash code based on <see cref="FileName"/>, <see cref="Index"/>,
        /// and all tokens in <see cref="Tokens"/>.
        /// </returns>
        /// <remarks>
        /// The hash code combines the file name, index, and each individual token's hash code
        /// for consistent hashing of tokenized rows.
        /// </remarks>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 27;
                hash = (13 * hash) + FileName.GetHashCode();
                hash = (13 * hash) + Index.GetHashCode();
                if (Tokens != null)
                {
                    for (int i = 0; i < Tokens.Length; i++)
                    {
                        hash = (13 * hash) + (Tokens[i].GetHashCode());
                    }
                }
                return hash;
            }
        }
    }
}

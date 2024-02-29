// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CSVTranslationLookup.Common.Tokens
{
    /// <summary>
    /// Specifies the type of token.
    /// </summary>
    public enum TokenType
    {
        /// <summary>
        /// A normal token
        /// </summary>
        Token,

        /// <summary>
        /// The final token in a row
        /// </summary>
        EndOfRecord
    }
}

// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CSVTranslationLookup.Common.Tokens
{
    /// <summary>
    /// Specifies the type of token in a CSV row.
    /// </summary>
    /// <remarks>
    /// Token types are used to distinguish between regular fields and the final field
    /// in a CSV row. The final field is marked with <see cref="EndOfRecord"/> to indicate
    /// the end of the row during tokenization.
    /// </remarks>
    public enum TokenType
    {
        /// <summary>
        /// A regular field token within a CSV row.
        /// </summary>
        /// <remarks>
        /// Represents any field that is not the last field in the row. Multiple <see cref="Token"/>
        /// values may appear in a single row, followed by one <see cref="EndOfRecord"/> token.
        /// </remarks>
        Token,

        /// <summary>
        /// The final token in a CSV row, indicating the end of the record.
        /// </summary>
        /// <remarks>
        /// Marks the last field in a row. Each tokenized row contains exactly one <see cref="EndOfRecord"/>
        /// token at the end, which may have content (if the row ends with a field) or be empty
        /// (if the row ends with a trailing delimiter).
        /// </remarks>
        EndOfRecord
    }
}

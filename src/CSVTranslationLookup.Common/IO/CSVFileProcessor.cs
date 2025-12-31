// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CSVTranslationLookup.Common.Text;
using CSVTranslationLookup.Common.Tokens;

namespace CSVTranslationLookup.Common.IO
{
    /// <summary>
    /// Provides methods for processing and tokenizing CSV files.
    /// </summary>
    /// <remarks>
    /// Handles complex CSV parsing including quoted fields, escaped quotes (double quotes),
    /// and multi-line quoted fields. Files are opened with shared read/write access to allow
    /// concurrent access by other applications. Parsing is performed sequentially with
    /// tokenization parallelized for optimal performance.
    /// </remarks>
    public static class CSVFileProcessor
    {
        /// <summary>
        /// Processes a CSV file and returns a parallel query of tokenized rows.
        /// </summary>
        /// <param name="filename">The path of the CSV file to process.</param>
        /// <param name="delimiter">The character that represents a field delimiter. Defaults to <c>,</c> (comma).</param>
        /// <param name="quote">The character that represents the start and end of a quoted field. Defaults to <c>"</c> (double quote).</param>
        /// <returns>
        /// A parallel query of <see cref="TokenizedRow"/> instances representing the tokenized content of the CSV file,
        /// maintaining the original row order.
        /// </returns>
        /// <remarks>
        /// The processing handles:
        /// <list type="bullet">
        /// <item>Quoted fields that may contain delimiters and newlines</item>
        /// <item>Escaped quotes (two consecutive quote characters within a quoted field)</item>
        /// <item>Multi-line quoted fields that span multiple lines in the file</item>
        /// <item>Empty rows (which are skipped)</item>
        /// </list>
        /// The file is opened with <see cref="FileShare.ReadWrite"/> to allow other applications
        /// to access it concurrently. Row parsing is sequential to handle multi-line quoted fields,
        /// but tokenization is parallelized using all available processor cores for performance.
        /// </remarks>
        public static ParallelQuery<TokenizedRow> ProcessFile(string filename, char delimiter = ',', char quote = '"')
        {
            List<string> rows = new List<string>();

            // Open with FileShare.ReadWrite to allow concurrent access by other applications
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(fs))
                {
                    StringBuilder currentRow = StringBuilderCache.Get();
                    bool inQuotedField = false;
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        // process each character in the line to track quote state
                        for (int i = 0; i < line.Length; i++)
                        {
                            char c = line[i];

                            if (c == quote)
                            {
                                // Check for escaped quote (two consecutive quotes)
                                if (i + 1 < line.Length && line[i + 1] == quote)

                                {
                                    // Escaped quote, add both and skip next
                                    currentRow.Append(quote);
                                    currentRow.Append(quote);
                                    i++;
                                }
                                else
                                {
                                    // Toggle quote state (entering or leaving quoted field)
                                    inQuotedField = !inQuotedField;
                                    currentRow.Append(c);
                                }
                            }
                            else
                            {
                                currentRow.Append(c);
                            }
                        }

                        if (inQuotedField)
                        {
                            // inside a multi-line quoted field, preserve the newline and continue
                            currentRow.AppendLine();
                        }
                        else
                        {
                            // End of row, add to collection and reset for next row
                            string rowText = currentRow.ToString().Trim();
                            if (!string.IsNullOrWhiteSpace(rowText))
                            {
                                rows.Add(rowText);
                            }
                            currentRow.Clear();
                        }
                    }

                    // Handle any remaining content (file without trailing new line)
                    if (currentRow.Length > 0)
                    {
                        string rowText = currentRow.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(rowText))
                        {
                            rows.Add(rowText);
                        }
                    }

                    currentRow.Recycle();
                }
            }

            // Tokenize rows in parallel for performance while maintaining order
            var query = rows.AsParallel()
                            .AsOrdered()
                            .WithDegreeOfParallelism(Environment.ProcessorCount);

            // Line number is index + 1 (1-based) for user-friendly display
            return query.Select((line, index) => new TokenizedRow(filename, index, Tokenizer.Tokenize(line, filename, index + 1, delimiter, quote)));
        }
    }
}

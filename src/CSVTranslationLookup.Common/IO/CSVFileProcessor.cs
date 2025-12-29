// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using CSVTranslationLookup.Common.Text;
using CSVTranslationLookup.Common.Tokens;

namespace CSVTranslationLookup.Common.IO
{
    /// <summary>
    /// Provides methos for processing and tokenizing a CSV file.
    /// </summary>
    public static class CSVFileProcessor
    {
        /// <summary>
        /// Tokenizes and processes a CSV file, returning a parallel query of TokenizedRow instances.
        /// </summary>
        /// <param name="filename">The path of the CSV file to be processed.</param>
        /// <param name="delimiter">The character that represents a delimiter.</param>
        /// <param name="quote">The character that represents the start of a quoted token.</param>
        /// <returns>
        /// A parallel query of TokenizedRow instances representing the tokenized content of the CSV file.
        /// </returns>
        public static ParallelQuery<TokenizedRow> ProcessFile(string filename, char delimiter = ',', char quote = '"')
        {
            List<string> rows = new List<string>();

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
                                    // Toggle quote state
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
                            // End of row, add to collection and reset
                            string rowText = currentRow.ToString().Trim();
                            if (!string.IsNullOrWhiteSpace(rowText))
                            {
                                rows.Add(rowText);
                            }
                            currentRow.Clear();
                        }
                    }

                    // Handle any remaining content (file witout trailing new line)
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

            // Tokenize rows in parallel for performance
            var query = rows.AsParallel()
                            .AsOrdered()
                            .WithDegreeOfParallelism(Environment.ProcessorCount);

            return query.Select((line, index) => new TokenizedRow(filename, index, Tokenizer.Tokenize(line, filename, index + 1, delimiter, quote)));
        }
    }
}

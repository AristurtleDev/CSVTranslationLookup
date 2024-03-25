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
            List<string> lines = new List<string>();

            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(fs))
                {
                    StringBuilder buffer = StringBuilderCache.Get();

                    bool waitingForQuote = false;
                    int c;

                    while ((c = reader.Read()) != -1)
                    {
                        char character = (char)c;

                        if (!waitingForQuote)
                        {
                            if (character == quote)
                            {
                                waitingForQuote = true;
                                buffer.Append(character);
                                continue;
                            }

                            if (character == '\n')
                            {
                                lines.Add(buffer.ToString().Trim());
                                buffer.Clear();
                            }
                        }
                        else
                        {
                            if (character == '\n')
                            {
                                buffer.AppendLine();
                                continue;
                            }

                            if (character == quote)
                            {
                                buffer.Append(quote);
                                waitingForQuote = false;
                                continue;
                            }
                        }

                        buffer.Append(character);
                    }
                    lines.Add(buffer.GetStringAndRecycle().Trim());
                }
            }

            var query = lines.AsParallel()
                             .AsOrdered()
                             .WithDegreeOfParallelism(Environment.ProcessorCount)
                             .Where(row => !string.IsNullOrEmpty(row));

            return query.Select((line, index) => new TokenizedRow(filename, index, Tokenizer.Tokenize(line, filename, index + 1, delimiter, quote)));
        }
    }
}

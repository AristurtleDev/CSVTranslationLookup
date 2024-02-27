// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using CSVTranslationLookup.Common.Text;
using CSVTranslationLookup.Common.Tokens;

namespace CSVTranslationLookup.Common.IO
{
    public static class CSVReader
    {
        public static ParallelQuery<TokenizedRow> FromFile(string filename, char delimiter = ',', char quote = '"')
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
                                continue;
                            }

                            if (character == '\n')
                            {
                                lines.Add(buffer.ToString());
                                buffer.Clear();
                            }
                        }
                        else
                        {
                            if(character == '\n')
                            {
                                buffer.AppendLine();
                                continue;
                            }

                            if(character == quote)
                            {
                                if(reader.Peek() == quote)
                                {
                                    buffer.Append(quote);
                                    _ = reader.Read();  //  Discard the next quote
                                    continue;
                                }

                                waitingForQuote = false;
                                continue;
                            }
                        }

                        buffer.Append(character);
                    }
                    buffer.Recycle();
                }
            }

            var query = lines.AsParallel()
                             .AsOrdered()
                             .WithDegreeOfParallelism(Environment.ProcessorCount)
                             .Where(row => !string.IsNullOrEmpty(row));

            return query.Select((line, index) => new TokenizedRow(index, Tokenizer.Tokenize(line, delimiter, quote)));

        }
    }
}

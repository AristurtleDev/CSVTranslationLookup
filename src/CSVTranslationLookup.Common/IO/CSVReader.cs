// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSVTranslationLookup.Common.Tokens;

namespace CSVTranslationLookup.Common.IO
{
    public static class CSVReader
    {
        public static ParallelQuery<TokenizedRow> FromFile(string filename, char delimiter = ',', char quote = '"')
        {
            string[] lines = File.ReadAllLines(filename);
            var query = lines.AsParallel()
                             .AsOrdered()
                             .WithDegreeOfParallelism(Environment.ProcessorCount)
                             .Where(row => !string.IsNullOrEmpty(row));

            return query.Select((line, index) => new TokenizedRow(index, Tokenizer.Tokenize(line, delimiter, quote)));

        }
    }
}

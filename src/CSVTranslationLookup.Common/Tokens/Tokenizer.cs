﻿// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using CSVTranslationLookup.Common.IO;

namespace CSVTranslationLookup.Common.Tokens
{
    /// <summary>
    /// Provides methods for tokenizing a given string based on RFC4180 CSV specifications.
    /// </summary>
    public static class Tokenizer
    {
        /// <summary>
        /// Tokenizes the given string based on CSV specification as defined in RFC4180
        /// </summary>
        /// <param name="input">The string to tokenize</param>
        /// <param name="fileName">The file that the input string is from.</param>
        /// <param name="lineNumber">The line number in the file that the input string is from.</param>
        /// <param name="delimiter">The character that represnets a delimiter</param>
        /// <param name="quote">The character that represents a quote.</param>
        /// <returns>An array containing the tokens generated</returns>
        public static Token[] Tokenize(string input, string fileName, int lineNumber, char delimiter = ',', char quote = '"')
        {
            using (TokenReader reader = new TokenReader(input, delimiter, quote))
            {
                IList<Token> tokens = new List<Token>();

                while (true)
                {
                    Token token = reader.NextToken();
                    token.FileName = fileName;
                    token.LineNumber = lineNumber;
                    tokens.Add(token);
                    if (token.TokenType == TokenType.EndOfRecord)
                    {
                        break;
                    }
                }

                return tokens.ToArray();
            }
        }
    }
}

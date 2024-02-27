// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CSVTranslationLookup.Common.IO;

namespace CSVTranslationLookup.Common.Tokens
{
    public static class Tokenizer
    {
        /// <summary>
        /// Tokenizes the given string based on CSV specification as defined in RFC4180
        /// </summary>
        /// <param name="input">The string to tokenize</param>
        /// <param name="delimiter">The character that represnets a delimiter</param>
        /// <param name="quote">The character that represents a quote.</param>
        /// <returns></returns>
        public static Token?[] Tokenize(string input, char delimiter = ',', char quote = '"')
        {
            using (TokenReader reader = new TokenReader(input, delimiter, quote))
            {
                return ReadTokens(reader).ToArray();
            }
        }

        private static IList<Token> ReadTokens(TokenReader reader)
        {
            IList<Token> tokens = new List<Token>(); ;
            while (true)
            {
                Token token = reader.NextToken();
                tokens.Add(token);
                if (token.TokenType == TokenType.EndOfRecord)
                {
                    break;
                }
            }
            return tokens;
        }
    }
}

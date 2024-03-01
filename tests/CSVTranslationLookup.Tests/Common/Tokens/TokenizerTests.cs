// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Text;
using CSVTranslationLookup.Common.Tokens;

namespace CSVTranslationLookup.Tests.Common.Tokens
{
    public class TokenizerTests
    {
        [Fact]
        public void Tokenize_Non_Quoted()
        {
            Token[] expected = new Token[]
            {
                new Token(TokenType.Token, "one"),
                new Token(TokenType.Token, "two"),
                new Token(TokenType.EndOfRecord, "three")
            };

            string line = "one,two,three";
            Token[] actual = Tokenizer.Tokenize(line, string.Empty, 0);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Tokenize_Quoted()
        {
            Token[] expected = new Token[]
            {
                new Token(TokenType.Token, "one"),
                new Token(TokenType.Token, "two"),
                new Token(TokenType.EndOfRecord, "three")
            };

            string line = "one,\"two\",three";
            Token[] actual = Tokenizer.Tokenize(line, string.Empty, 0);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Tokenize_Quoted_NewLines()
        {
            Token[] expected = new Token[]
            {
                new Token(TokenType.Token, "one"),
                new Token(TokenType.Token, "two"),
                new Token(TokenType.EndOfRecord, $"three{Environment.NewLine}four")
            };

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("one,two,\"three")
                   .Append("four\"");

            string line = builder.ToString();

            Token[] actual = Tokenizer.Tokenize(line, string.Empty, 0);
            Assert.Equal(expected, actual);
        }
    }
}

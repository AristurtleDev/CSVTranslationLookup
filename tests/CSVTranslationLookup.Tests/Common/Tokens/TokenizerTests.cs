using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSVTranslationLookup.Common.Tokens;

namespace CSVTranslationLookup.Tests.Common.Tokens
{
    public sealed class TokenizerTests
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
            Token?[] actual = Tokenizer.Tokenize(line);

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
            Token?[] actual = Tokenizer.Tokenize(line);

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

            string line =
                """
                one,two,"three
                four"
                """;

            Token?[] actual = Tokenizer.Tokenize(line);
            Assert.Equal(expected, actual);
        }
    }
}

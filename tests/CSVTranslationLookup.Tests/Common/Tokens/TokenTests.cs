using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSVTranslationLookup.Common.Tokens;

namespace CSVTranslationLookup.Tests.Common.Tokens
{
    public class TokenTests
    {
        [Fact]
        public void Equal_True_When_Same_Values()
        {
            Token expecteed = new Token(TokenType.Token, "example");
            Token actual = new Token(TokenType.Token, "example");
            Assert.True(expecteed.Equals(actual));
        }

        [Fact]
        public void Equals_False_When_Different_Values()
        {
            Token expected = new Token(TokenType.Token, "example");
            Token actual = new Token(TokenType.Token);
            Assert.False(expected.Equals(actual));
            actual = new Token(TokenType.EndOfRecord, expected.Content);
            Assert.False(expected.Equals(actual));
        }

        [Fact]
        public void GetHashCode_Same_When_Same_Values()
        {
            TokenType tokenType = TokenType.Token;
            string content = "example";

            int expected = new Token(tokenType, content).GetHashCode();
            int actual = new Token(tokenType, content).GetHashCode();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetHashCode_Different_When_Different_Values()
        {
            int expected = new Token(TokenType.Token, "example").GetHashCode();
            int actual = new Token(TokenType.EndOfRecord, "example").GetHashCode();
            Assert.NotEqual(expected, actual);
        }
    }
}

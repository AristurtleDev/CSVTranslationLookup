// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CSVTranslationLookup.Common.Tokens;

namespace CSVTranslationLookup.Tests.Common.Tokens
{
    public class TokenTests
    {
        [Fact]
        public void Equals_True_When_Same_Values()
        {
            Token expected = new Token(TokenType.Token, "example");
            Token actual = new Token(TokenType.Token, "example");
            Assert.True(expected.Equals(actual));
        }

        [Fact]
        public void Equals_False_When_Different_Values()
        {
            Token expected = new Token(TokenType.Token, "example");
            Token actual = new Token(TokenType.Token);
            Assert.False(expected.Equals(actual));
            actual = new Token(TokenType.EndOfRecord, "example");
            Assert.False(expected.Equals(actual));
        }

        [Fact]
        public void GetHashCode_Same_When_Same_Values()
        {
            int expected = new Token(TokenType.Token, "example").GetHashCode();
            int actual = new Token(TokenType.Token, "example").GetHashCode();
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

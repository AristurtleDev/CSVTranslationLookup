// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CSVTranslationLookup.Common.Tokens;

namespace CSVTranslationLookup.Tests.Common.Tokens
{
    public class TokenizedRowTests
    {
        [Fact]
        public void Equals_True_When_Same_Values()
        {
            Token token = new Token(TokenType.Token, "example");
            TokenizedRow expected = new TokenizedRow(string.Empty, 0, new Token[] { token });
            TokenizedRow actual = new TokenizedRow(string.Empty, 0, new Token[] { token });
            Assert.True(expected.Equals(actual));
        }

        [Fact]
        public void Equals_False_When_Different_Values()
        {
            Token token = new Token(TokenType.Token, "example");
            TokenizedRow expected = new TokenizedRow(string.Empty, 0, new Token[] { token });
            TokenizedRow actual = new TokenizedRow(string.Empty, 1, new Token[] { token });
            Assert.False(expected.Equals(actual));
        }

        [Fact]
        public void GetHashCode_Same_When_Same_Values()
        {
            Token token = new Token(TokenType.Token, "example");
            int expected = new TokenizedRow(string.Empty, 0, new Token[] { token }).GetHashCode();
            int actual = new TokenizedRow(string.Empty, 0, new Token[] { token }).GetHashCode();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetHashCode_Different_When_Different_Values()
        {
            Token token = new Token(TokenType.Token, "example");
            int expected = new TokenizedRow(string.Empty, 0, new Token[] { token }).GetHashCode();
            int actual = new TokenizedRow(string.Empty, 1, new Token[] { token }).GetHashCode();
            Assert.NotEqual(expected, actual);
        }
    }
}

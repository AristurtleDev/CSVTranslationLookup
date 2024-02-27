// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

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
        public void Equality()
        {
            Token expected = new Token(TokenType.Token, "example");
            Token actual = new Token(TokenType.Token, "example");
            Assert.Equal(expected, actual);
        }
    }
}

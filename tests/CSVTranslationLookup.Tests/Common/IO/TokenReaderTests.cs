// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CSVTranslationLookup.Common.IO;
using CSVTranslationLookup.Common.Tokens;

namespace CSVTranslationLookup.Tests.Common.IO
{
    public sealed class TokenReaderTests
    {
        [Fact]
        public void Dispose_Test()
        {
            //  Multiple dispose should not throw an exception.
            using (TokenReader reader = new TokenReader(string.Empty))
            {
                reader.Dispose();
                reader.Dispose();
                reader.Dispose();
            }
        }

        [Fact]
        public void ObjectDisposedException_If_Disposed_And_Used()
        {
            TokenReader reader = new TokenReader(string.Empty);
            reader.Dispose();
            Assert.Throws<ObjectDisposedException>(() => reader.NextToken());
        }

        [Fact]
        public void NextToken_NonQuoted()
        {
            string value = nameof(value);
            Token expected = new Token(TokenType.EndOfRecord, value);
            Token actual;

            using (TokenReader reader = new TokenReader(value))
            {
                actual = reader.NextToken();
            }

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NextToken_Quoted()
        {
            string value = $"\"{nameof(value)}\"";

            Token expected = new Token(TokenType.EndOfRecord, value.TrimStart('"').TrimEnd('"'));
            Token actual;

            using (TokenReader reader = new TokenReader(value))
            {
                actual = reader.NextToken();
            }

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NextToken_Quoted_MultiLine()
        {
            string value = $"\"{nameof(value)}{Environment.NewLine}{nameof(value)}\"";

            Token expected = new Token(TokenType.EndOfRecord, value.TrimStart('"').TrimEnd('"'));
            Token actual;

            using (TokenReader reader = new TokenReader(value))
            {
                actual = reader.NextToken();
            }

            Assert.Equal(expected, actual);
        }
    }
}

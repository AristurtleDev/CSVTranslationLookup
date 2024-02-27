// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CSVTranslationLookup.Common.IO;
using CSVTranslationLookup.Common.Tokens;

namespace CSVTranslationLookup.Tests.Common.IO
{
    public class CSVReaderTests
    {
        private string GetPath(string name)
        {
            return Path.Combine(Environment.CurrentDirectory, "Files", name);
        }

        [Fact]
        public void FromFile()
        {
            TokenizedRow row0 = new TokenizedRow(0, new Token[]
            {
                new Token(TokenType.Token, "id"),
                new Token(TokenType.Token, "birthday"),
                new Token(TokenType.Token, "first_name"),
                new Token(TokenType.Token, "last_name"),
                new Token(TokenType.Token, "address"),
                new Token(TokenType.Token, "city"),
                new Token(TokenType.Token, "state"),
                new Token(TokenType.Token, "zip"),
                new Token(TokenType.Token, "phone"),
                new Token(TokenType.Token, "email"),
                new Token(TokenType.EndOfRecord, "description")
            });

            TokenizedRow row1 = new TokenizedRow(1, new Token[]
            {
            new Token(TokenType.Token, "1"),
                new Token(TokenType.Token, "1990-05-15"),
                new Token(TokenType.Token, "John"),
                new Token(TokenType.Token, "Doe"),
                new Token(TokenType.Token, "123 Main St"),
                new Token(TokenType.Token, "Anytown"),
                new Token(TokenType.Token, "CA"),
                new Token(TokenType.Token, "12345"),
                new Token(TokenType.Token, "555-1234"),
                new Token(TokenType.Token, "john.doe@example.com"),
                new Token(TokenType.EndOfRecord, $"A friendly and outgoing person who enjoys outdoor activities and traveling.{Environment.NewLine}Always up for a good book or movie night.")
            });

            List<TokenizedRow> expected= new List<TokenizedRow>() { row0, row1 };

            string path = GetPath("example.csv");
            ParallelQuery<TokenizedRow> actual = CSVReader.FromFile(path);
            Assert.Equal(expected.Count, actual.Count());
        }
    }
}

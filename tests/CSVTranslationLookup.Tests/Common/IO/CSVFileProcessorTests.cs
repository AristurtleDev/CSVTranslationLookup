// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CSVTranslationLookup.Common.IO;
using CSVTranslationLookup.Common.Tokens;

namespace CSVTranslationLookup.Tests.Common.IO
{
    public class CSVFileProcessorTests
    {
        private string GetPath(string name)
        {
            return Path.Combine(Environment.CurrentDirectory, "Files", name);
        }

        [Fact]
        public void ProcessFile()
        {
            TokenizedRow row0 = new TokenizedRow(string.Empty, 0, new Token[]
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

            TokenizedRow row1 = new TokenizedRow(string.Empty, 1, new Token[]
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

            List<TokenizedRow> expected = new List<TokenizedRow>() { row0, row1 };

            string path = GetPath("example.csv");
            ParallelQuery<TokenizedRow> actual = CSVFileProcessor.ProcessFile(path);
            Assert.Equal(expected.Count, actual.Count());
        }

        //  This was a specific edge case issue that was occuring where if the csv file did not end with
        //  an empty last line, then the last line read would not be added to the lines[] to tokenize
        //  internally.
        //  This test is to ensure that doesn't happen again.
        [Fact]
        public void Tokenizes_All_Lines_When_No_Empty_Last_Line()
        {
            TokenizedRow row0 = new TokenizedRow(string.Empty, 0, new Token[]
            {
                new Token(TokenType.Token, "key"),
                new Token(TokenType.EndOfRecord, "en")
            });

            TokenizedRow row1 = new TokenizedRow(string.Empty, 0, new Token[]
            {
                new Token(TokenType.Token, "ABILITY_NAME"),
                new Token(TokenType.EndOfRecord, "Defend")
            });

            List<TokenizedRow> expected = new List<TokenizedRow>() { row0, row1 };

            string path = GetPath("issue-not-tokenizing-last-line.csv");
            ParallelQuery<TokenizedRow> actual = CSVFileProcessor.ProcessFile(path);
            Assert.Equal(expected.Count, actual.Count());
        }
    }
}

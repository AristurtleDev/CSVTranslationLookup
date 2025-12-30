// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using CSVTranslationLookup.Common.Tokens;

namespace CSVTranslationLookup.Tests.Common.Tokens
{
    public class TokenizerTests
    {
        [Fact]
        public void Tokenize_Simple_NonQuoted_Fields()
        {
            string input = "one,two,three";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1);

            Assert.Equal(3, tokens.Length);
            Assert.Equal("one", tokens[0].Content);
            Assert.Equal("two", tokens[1].Content);
            Assert.Equal("three", tokens[2].Content);
            Assert.Equal(TokenType.Token, tokens[0].TokenType);
            Assert.Equal(TokenType.Token, tokens[1].TokenType);
            Assert.Equal(TokenType.EndOfRecord, tokens[2].TokenType);
        }

        [Fact]
        public void Tokenize_Quoted_Fields()
        {
            string input = "\"one\",\"two\",\"three\"";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1);

            Assert.Equal(3, tokens.Length);
            Assert.Equal("one", tokens[0].Content);
            Assert.Equal("two", tokens[1].Content);
            Assert.Equal("three", tokens[2].Content);
        }

        [Fact]
        public void Tokenize_Mixed_Quoted_And_NonQuoted()
        {
            string input = "one,\"two\",three";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1);

            Assert.Equal(3, tokens.Length);
            Assert.Equal("one", tokens[0].Content);
            Assert.Equal("two", tokens[1].Content);
            Assert.Equal("three", tokens[2].Content);
        }

        [Fact]
        public void Tokenize_Empty_Fields()
        {
            string input = "one,,three";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1);

            Assert.Equal(3, tokens.Length);
            Assert.Equal("one", tokens[0].Content);
            Assert.Empty(tokens[1].Content);
            Assert.Equal("three", tokens[2].Content);
        }

        [Fact]
        public void Tokenize_Trailing_Empty_Field()
        {
            string input = "one,two,";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1);

            Assert.Equal(3, tokens.Length);
            Assert.Equal("one", tokens[0].Content);
            Assert.Equal("two", tokens[1].Content);
            Assert.Empty(tokens[2].Content);
        }

        [Fact]
        public void Tokenize_Leading_Whitespace()
        {
            string input = "  one,  two,  three";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1);

            Assert.Equal(3, tokens.Length);
            Assert.Equal("one", tokens[0].Content);
            Assert.Equal("two", tokens[1].Content);
            Assert.Equal("three", tokens[2].Content);
        }

        [Fact]
        public void Tokenize_Trailing_Whitespace()
        {
            string input = "one  ,two  ,three  ";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1);

            Assert.Equal(3, tokens.Length);
            Assert.Equal("one", tokens[0].Content);
            Assert.Equal("two", tokens[1].Content);
            Assert.Equal("three", tokens[2].Content);
        }

        [Fact]
        public void Tokenize_Escaped_Quotes()
        {
            string input = "\"one \"\"quoted\"\" value\",\"two\",\"three\"";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1);

            Assert.Equal(3, tokens.Length);
            Assert.Equal("one \"quoted\" value", tokens[0].Content);
            Assert.Equal("two", tokens[1].Content);
            Assert.Equal("three", tokens[2].Content);
        }

        [Fact]
        public void Tokenize_Comma_Inside_Quoted_Field()
        {
            string input = "\"one, with comma\",two,three";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1);

            Assert.Equal(3, tokens.Length);
            Assert.Equal("one, with comma", tokens[0].Content);
            Assert.Equal("two", tokens[1].Content);
            Assert.Equal("three", tokens[2].Content);
        }

        [Fact]
        public void Tokenize_Newline_Inside_Quoted_Field()
        {
            string input = "\"one\nwith\nnewlines\",two,three";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1);

            Assert.Equal(3, tokens.Length);
            Assert.Equal("one\nwith\nnewlines", tokens[0].Content);
            Assert.Equal("two", tokens[1].Content);
            Assert.Equal("three", tokens[2].Content);
        }

        [Fact]
        public void Tokenize_Empty_String()
        {
            string input = "";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1);

            Assert.Single(tokens);
            Assert.Equal(TokenType.EndOfRecord, tokens[0].TokenType);
            Assert.Null(tokens[0].Content);
        }

        [Fact]
        public void Tokenize_Null_String()
        {
            string input = null;
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1);

            Assert.Single(tokens);
            Assert.Equal(TokenType.EndOfRecord, tokens[0].TokenType);
        }

        [Fact]
        public void Tokenize_Single_Field()
        {
            string input = "single";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1);

            Assert.Single(tokens);
            Assert.Equal("single", tokens[0].Content);
            Assert.Equal(TokenType.EndOfRecord, tokens[0].TokenType);
        }

        [Fact]
        public void Tokenize_Sets_FileName_And_LineNumber()
        {
            string input = "one,two";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 42);

            Assert.All(tokens, token =>
            {
                Assert.Equal("test.csv", token.FileName);
                Assert.Equal(42, token.LineNumber);
            });
        }

        [Fact]
        public void Tokenize_EndOfRecord_Token_Is_Last()
        {
            string input = "one,two,three";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1);

            Assert.Equal(TokenType.EndOfRecord, tokens[^1].TokenType);
            Assert.All(tokens.Take(tokens.Length - 1), token =>
            {
                Assert.Equal(TokenType.Token, token.TokenType);
            });
        }

        [Fact]
        public void Tokenize_Custom_Delimiter()
        {
            string input = "one|two|three";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1, delimiter: '|');

            Assert.Equal(3, tokens.Length);
            Assert.Equal("one", tokens[0].Content);
            Assert.Equal("two", tokens[1].Content);
            Assert.Equal("three", tokens[2].Content);
        }

        [Fact]
        public void Tokenize_Custom_Quote()
        {
            string input = "'one','two','three'";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1, quote: '\'');

            Assert.Equal(3, tokens.Length);
            Assert.Equal("one", tokens[0].Content);
            Assert.Equal("two", tokens[1].Content);
            Assert.Equal("three", tokens[2].Content);
        }

        [Fact]
        public void Tokenize_Complex_Escaped_Quotes()
        {
            string input = "\"He said \"\"Hello\"\" to me\",\"She said \"\"Hi\"\" back\"";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1);

            Assert.Equal(2, tokens.Length);
            Assert.Equal("He said \"Hello\" to me", tokens[0].Content);
            Assert.Equal("She said \"Hi\" back", tokens[1].Content);
        }

        [Fact]
        public void Tokenize_Only_Delimiter()
        {
            string input = ",";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1);

            Assert.Equal(2, tokens.Length);
            Assert.Empty(tokens[0].Content);
            Assert.Empty(tokens[1].Content);
        }

        [Fact]
        public void Tokenize_Multiple_Delimiters()
        {
            string input = ",,,";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1);

            Assert.Equal(4, tokens.Length);
            Assert.All(tokens, token => Assert.Empty(token.Content));
        }

        [Fact]
        public void Tokenize_Tab_Delimiter()
        {
            string input = "one\ttwo\tthree";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1, delimiter: '\t');

            Assert.Equal(3, tokens.Length);
            Assert.Equal("one", tokens[0].Content);
            Assert.Equal("two", tokens[1].Content);
            Assert.Equal("three", tokens[2].Content);
        }

        [Fact]
        public void Tokenize_Semicolon_Delimiter()
        {
            string input = "one;two;three";
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1, delimiter: ';');

            Assert.Equal(3, tokens.Length);
            Assert.Equal("one", tokens[0].Content);
            Assert.Equal("two", tokens[1].Content);
            Assert.Equal("three", tokens[2].Content);
        }

        [Fact]
        public void Performance_Test_Large_Row()
        {
            // Create a row with 100 fields
            var fields = Enumerable.Range(1, 100).Select(i => $"field{i}");
            string input = string.Join(",", fields);

            var sw = Stopwatch.StartNew();
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1);
            sw.Stop();

            Assert.Equal(100, tokens.Length);
            Assert.True(sw.ElapsedMilliseconds < 10, $"Tokenizing 100 fields took {sw.ElapsedMilliseconds}ms (should be <10ms)");
        }

        [Fact]
        public void Performance_Test_Many_Quoted_Fields()
        {
            // Create a row with 50 quoted fields
            var fields = Enumerable.Range(1, 50).Select(i => $"\"field {i} with spaces\"");
            string input = string.Join(",", fields);

            var sw = Stopwatch.StartNew();
            Token[] tokens = Tokenizer.Tokenize(input, "test.csv", 1);
            sw.Stop();

            Assert.Equal(50, tokens.Length);
            Assert.True(sw.ElapsedMilliseconds < 10, $"Tokenizing 50 quoted fields took {sw.ElapsedMilliseconds}ms (should be <10ms)");
        }

        [Fact]
        public void Tokenize_Real_World_CSV_Row()
        {
            // Real-world example from game localization
            string input = "ABILITY_ATTACK,Attack,\"Deal 5 damage to target enemy. This damage ignores armor.\"";
            Token[] tokens = Tokenizer.Tokenize(input, "abilities.csv", 10);

            Assert.Equal(3, tokens.Length);
            Assert.Equal("ABILITY_ATTACK", tokens[0].Content);
            Assert.Equal("Attack", tokens[1].Content);
            Assert.Equal("Deal 5 damage to target enemy. This damage ignores armor.", tokens[2].Content);
            Assert.Equal("abilities.csv", tokens[0].FileName);
            Assert.Equal(10, tokens[0].LineNumber);
        }
    }
}

// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Text;
using CSVTranslationLookup.Common.IO;

namespace CSVTranslationLookup.Tests.Common.IO
{
    public class StringReaderExtensionTests
    {
        [Fact]
        public void ReadTo_WithoutNewLines()
        {
            string value = "abcdefghijklmno\"pqrstuvwxy\"z;,.?@1234567890";
            StringReader reader = new StringReader(value);
            string expected = value[..value.IndexOf(';')];
            string actual = reader.ReadTo(';');
            Assert.Equal(expected, actual);
        }
        [Fact]
        public void ReadTo_WithNewLines()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("abcde")
                   .AppendLine("fghij")
                   .AppendLine("klmno")
                   .AppendLine("\"pqrs")
                   .AppendLine("tuvwx")
                   .AppendLine("y\"z;,")
                   .AppendLine(".?@12")
                   .AppendLine("34567")
                   .AppendLine("890");
            string value = builder.ToString();

            StringReader reader = new StringReader(value);
            string expected = value[..value.IndexOf(';')];
            string actual = reader.ReadTo(';');
            Assert.Equal(expected, actual);
        }
    }
}

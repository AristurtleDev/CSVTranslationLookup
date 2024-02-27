// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

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
            string value =
                """
                abcde
                fghij
                klmno
                "pqrs
                tuvwx
                y"z;,
                .?@12
                34567
                890
                """;

            StringReader reader = new StringReader(value);
            string expected = value[..value.IndexOf(';')];
            string actual = reader.ReadTo(';');
            Assert.Equal(expected, actual);
        }
    }
}

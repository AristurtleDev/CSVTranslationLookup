// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CSVTranslationLookup.CSV
{
    internal class CSVItem
    {
        /// <summary>
        /// Gets the token keyword unique to this item.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the value of this item.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the line number in the CSV file this item is located at.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Gets the fully-qualified absolute path to the CSV file this iatem is located in.
        /// </summary>
        public string FilePath { get; set; }

        public CSVItem(string key, string value, int lineNumber, string filePath)
        {
            Key = key;
            Value = value;
            LineNumber = lineNumber;
            FilePath = filePath;
        }
    }
}

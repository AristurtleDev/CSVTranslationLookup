// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CSVTranslationLookup
{
    internal class LookupItem
    {
        /// <summary>
        /// Gets or Sets the token keyword unique to the item.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or Sets the value of the item.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets the line number within the file that the item is located.
        /// </summary>
        public int LineNumber { get; set; }


        /// <summary>
        /// Gets or Sets the path of the file the item is contained in.
        /// </summary>
        public string FilePath { get; set; }

        public LookupItem(string key, string value, int lineNumber, string filePath)
        {
            Key = key;
            Value = value;
            LineNumber = lineNumber;
            FilePath = filePath;
        }
    }
}

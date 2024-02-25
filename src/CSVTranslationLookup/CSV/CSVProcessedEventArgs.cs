// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace CSVTranslationLookup.CSV
{
    internal class CSVProcessedEventArgs
    {
        /// <summary>
        /// Gets the items that were processed.
        /// </summary>
        public Dictionary<string, CSVItem> Items { get; }

        /// <summary>
        /// Gets the path to the file that was procesed.
        /// </summary>
        public string FilePath { get; set; }

        public CSVProcessedEventArgs(string filePath, Dictionary<string, CSVItem> items)
        {
            FilePath = filePath;
            Items = items;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSVTranslationLookup.Common
{
    /// <summary>
    /// Represents an entry (aka row) in a CSV file.
    /// </summary>
    public class CSVEntry
    {
        /// <summary>
        /// Gets the values of this entry.  
        /// </summary>
        /// <remarks>
        /// The order of items in this collection is the order of values for the CSV entry from its first column to
        /// its last column.
        /// </remarks>
        public List<string> Values { get; } = new List<string>();

        /// <summary>
        /// Gets the token keyword unique to this entry.
        /// </summary>
        /// <remarks>
        /// The token keyword will always be the value of the entry's first column.
        /// </remarks>
        public string Key => Values.Count > 0 ? Values[0] : string.Empty;

        /// <summary>
        /// Gets the absolute file path to the CSV file that this entry is located in.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the line number in the CSV file this entry is located at.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSVEntry"/> class.
        /// </summary>
        /// <param name="filePath">The absolute full path to the CSV file that this entry is located in.</param>
        /// <param name="lineNumber">The line number in the fiel that this entry is located at.</param>
        public CSVEntry(string filePath, int lineNumber)
        {
            FilePath = filePath;
            LineNumber = lineNumber;
        }
    }
}

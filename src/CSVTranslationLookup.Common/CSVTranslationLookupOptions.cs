// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CSVTranslationLookup.Common
{
    /// <summary>
    /// Defines the options used to configure the CSVTranslationLookup extension.
    /// </summary>
    public class CSVTranslationLookupOptions
    {
        /// <summary>
        /// Gets or Sets the absolute path to the configuration file.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or Sets an optional path that would be relative to the configuration file to watch for changes to
        /// CSV files in.
        /// </summary>
        public string? WatchPath { get; set; } = null;

        /// <summary>
        /// Gets or Sets an optional path ot the executable to open the CSV with.  If no value is provided, then the
        /// csv will be opened using the application set as the default application to open CSVs with in Windows.
        /// </summary>
        public string? OpenWith { get; set; } = null;

        /// <summary>
        /// Gets or Sets the optional arguments to supply to the execuable used to open the CSV with.
        /// These arguments are only supplied if <see cref="OpenWith"/> has a value.
        /// </summary>
        public string? Arguments { get; set; } = null;

        /// <summary>
        /// <para>
        /// Gets or sets a collection of suffixes to check as fallbacks if a keyword token is not found.  Fallbacks
        /// are searched in teh order they are added to this colleciton.
        /// </para>
        /// <para>
        /// For example, if the suffix is <c>_M</c>, and the token is <c>ABILITY_NAME</c> and <c>ABILITY_NAME</c>
        /// is not found, it will check for <c>ABILITY_NAME_M</c> as a fallback.
        /// </para>
        /// </summary>
        /// 
        public List<string> FallbackSuffixes { get; set; } = new List<string>();

        /// <summary>
        /// Gets or Sets the character used to mark a quote block in the CSV File.  The default is <c>"</c>
        /// </summary>
        public char Quote { get; set; } = '"';

        /// <summary>
        /// Gets or Sets the delimiter to used in the CSV files.  The default value is <c>,</c>
        /// </summary>
        public char Delimiter { get; set; } = ',';

        public CSVTranslationLookupOptions(string filename)
        {
            FileName = filename;
        }
    }

}

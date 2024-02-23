// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace CSVTranslationLookup.Configuration
{
    internal class CSVTranslationLookupSettings
    {
        public const string FileName = ".csvsettings";
        private static readonly CSVTranslationLookupSettings _default = new CSVTranslationLookupSettings()
        {
            WatchPath = string.Empty,
            OpenWith = "notepad.exe",
            Arguments = string.Empty,
            FallbackSuffixes = new List<string>(),
            Delimiter = ","
        };

        public static CSVTranslationLookupSettings Default => _default;

        /// <summary>
        /// Gets or Sets the fully-qualified directory path to watch for changes to CSV files and these settings.
        /// </summary>
        public string WatchPath { get; set; }

        /// <summary>
        /// Gets or Sets the fully-qualified path to the executable used to open a CSV file in.
        /// </summary>
        public string OpenWith { get; set; }

        /// <summary>
        /// Gets or Sets additional CLI arguments to pass to the executable called when opening a CSV file.
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// <para>
        /// Gets or Sets a collection of suffixes to check as falbacks if a keyword token is not found.  Fallbacks are
        /// searched in the order they are added to this collection.
        /// </para>
        /// <para>
        /// For example, if the suffix is _M, and the token is ABILITY_NAME and ABILITY_NAME is not found, it will
        /// check for ABILITY_NAME_M as a fallback.
        /// </para>
        /// </summary>
        public List<string> FallbackSuffixes { get; set; } = new List<string>();

        /// <summary>
        /// Gets or Sets the delimeter used in the CSV files.  If one isn't specified, the default comma <c>,</c>
        /// will be used.
        /// </summary>
        public string Delimiter { get; set; }

        [JsonIgnore]
        internal string SettingsFilePath { get; set; }
    }
}

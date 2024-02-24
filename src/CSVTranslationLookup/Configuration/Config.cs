// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace CSVTranslationLookup.Configuration
{
    internal class Config
    {
        /// <summary>
        /// Gets or sets the fully-qualified path to the configuration file
        /// </summary>
        [JsonIgnore]
        public string FileName { get; set; }

        /// <summary>
        /// Gets or Sets the path relative configuration file to watch for changes to CSV files.
        /// </summary>
        [JsonProperty("watchPath")]
        public string WatchPath { get; set; }

        /// <summary>
        /// Gets or SEts an optional path to the executable to open the CSV with. If not value is provided, then the
        /// csv will open using the application set as default in Windows.
        /// </summary>
        [JsonProperty("openWith")]
        public string OpenWith { get; set; }

        /// <summary>
        /// Gets or Sets the optional arguments to supply to the executable used to open the CSV with.
        /// These arguments are prepended to the command to open the file.
        /// These areguments are only supplied if <see cref="OpenWith"/> has a value.
        /// </summary>
        [JsonProperty("arguments")]
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
        [JsonProperty("fallBackSuffixes")]

        public List<string> FallbackSuffixes { get; set; }

        /// <summary>
        /// Gets or Sets the delimiter used in the CSV files.  If one isn't specified, the default comma <c>,</c>
        /// will be used.
        /// </summary>
        [JsonProperty("delimiter")]
        public string Delimiter { get; set; }

        /// <summary>
        /// Gets the fully-qualified absolute path to the watch directory.
        /// </summary>
        /// <returns>The fully-qualified absolute path to the watch directory.</returns>
        public DirectoryInfo GetAbsoluteWatchDirectory()
        {
            string dir = new FileInfo(FileName).DirectoryName;
            return new DirectoryInfo(Path.Combine(dir, WatchPath.Replace('/', '\\')));
        }
    }
}

// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace CSVTranslationLookup.Configuration
{
    public class Config
    {
        /// <summary>
        /// The name of the configuration file.
        /// </summary>
        public const string ConfigurationFilename = "csvconfig.json";

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
        /// Gets or SEts teh character that represents the delimiter used int he CSV Files.
        /// The default is <c>,</c>.
        /// </summary>
        [JsonProperty("delimiter")]
        public char Delimiter { get; set; }

        /// <summary>
        /// Gets or Sets the character that represents the start and end of a quoted block in the CSV files.
        /// The default is <c>"</c>.
        /// </summary>
        [JsonProperty("quote")]
        public char Quote { get; set; }

        [JsonProperty("diagnostic")]
        public bool Diagnostic { get; set; }

        /// <summary>
        /// Gets the fully-qualified absolute path to the watch directory.
        /// </summary>
        /// <returns>The fully-qualified absolute path to the watch directory.</returns>
        public DirectoryInfo GetAbsoluteWatchDirectory()
        {
            string path = new FileInfo(FileName).DirectoryName;
            if (!string.IsNullOrEmpty(WatchPath))
            {
                path = Path.GetFullPath(path + WatchPath.Replace('/', '\\'));
            }

            return new DirectoryInfo(path);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Config"/> class initialized from the vlaues in the specified file.
        /// </summary>
        /// <param name="fileName">The path to the configuration file.</param>
        /// <returns>The instance of the <see cref="Config"/> class created by this method.</returns>
        public static Config FromFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return null;
            }

            string json = File.ReadAllText(fileName);
            Config config = JsonConvert.DeserializeObject<Config>(json);
            if (config == null)
            {
                config = new Config();
            }
            config.FileName = fileName;

            if(config.Delimiter == default)
            {
                config.Delimiter = ',';
            }

            if(config.Quote == default)
            {
                config.Quote = '"';
            }
            return config;
        }
    }
}

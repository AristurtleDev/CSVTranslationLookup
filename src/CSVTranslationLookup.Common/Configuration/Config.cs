// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            if (string.IsNullOrEmpty(FileName))
            {
                throw new InvalidOperationException($"FilName must be set before calling {nameof(GetAbsoluteWatchDirectory)}");
            }

            try
            {
                string configDir = new FileInfo(FileName).DirectoryName;
                string targetPath = configDir;

                if (!string.IsNullOrEmpty(WatchPath))
                {
                    // Normalize path separators for current platform
                    string normalizedWatchPath = WatchPath.Replace('/', Path.DirectorySeparatorChar)
                                                          .Replace('\\', Path.DirectorySeparatorChar);

                    targetPath = Path.GetFullPath(Path.Combine(configDir, normalizedWatchPath));
                }

                return new DirectoryInfo(targetPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve watch directory.  Config file: '{FileName}', Watch path: '{WatchPath}'",
                    ex
                );
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Config"/> class initialized from the vlaues in the specified file.
        /// </summary>
        /// <param name="fileName">The path to the configuration file.</param>
        /// <param name="validationResult">
        /// When this method returns, contains the validation result, or null if file doesn't exist.
        /// </param>
        /// <returns>
        /// The instance of the <see cref="Config"/> class created by this method, or null if the file doesn't exist or parsing failed.
        /// </returns>
        public static Config FromFile(string fileName, out ConfigValidationResult validationResult)
        {
            validationResult = null;

            if(!File.Exists(fileName))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(fileName);

                Config config;
                try
                {
                    config = JsonConvert.DeserializeObject<Config>(json);
                }
                catch(JsonException ex)
                {
                    validationResult = new ConfigValidationResult();
                    validationResult.AddError($"INvalid JSON in configuration file: {ex.Message}");
                    return null;
                }

                // Handle null result (empty file or null content)
                if(config == null)
                {
                    config = new Config();
                }

                config.FileName = fileName;

                // Apply defaults
                if(config.Delimiter == default(char))
                {
                    config.Delimiter = ',';
                }

                if(config.Quote == default(char))
                {
                    config.Quote = '"';
                }

                if(config.FallbackSuffixes == null)
                {
                    config.FallbackSuffixes = new List<string>();
                }

                validationResult = config.Validate();

                return config;
            }
            catch(Exception ex)
            {
                validationResult = new ConfigValidationResult();
                validationResult.AddError($"Failed to load configuration: {ex.Message}");
                return null;
            }
        }

        public ConfigValidationResult Validate()
        {
            var result = new ConfigValidationResult();

            ValidateDelimiterAndQuote(result);
            ValidatePaths(result);
            ValidateExecutable(result);
            ValidateFallbackSuffixes(result);

            return result;
        }

        public void ValidateDelimiterAndQuote(ConfigValidationResult result)
        {
            // Check for null characters
            if(Delimiter == default(char))
            {
                result.AddError("Delimiter cannot be null.  Use ',' for comma-seperated values");
            }

            if(Quote == default(char))
            {
                result.AddError("Quote cannot be null character.  Use '\"' for double-quote.");
            }

            // Check for same character
            if(Delimiter != default(char) && Quote != default(char) && Delimiter == Quote)
            {
                result.AddError($"Delimiter and Quote cannot be the same character ('{Delimiter}'). CSV parsing will fail.");
            }

            // Check for problematic characters
            if(Delimiter == '\n' || Delimiter == '\r')
            {
                result.AddError("Delimiter cannot be a newline character.  This will break CSV parsing.");
            }

            if(Quote == '\n' || Quote == '\r')
            {
                result.AddError("Quote cannot be a newline character.  This will break CSV parsing.");
            }                
        }

        private void ValidatePaths(ConfigValidationResult result)
        {
            // Validate the watch path
            if(!string.IsNullOrEmpty(WatchPath))
            {
                // Check for invalid path characters
                char[] invalidChars = Path.GetInvalidFileNameChars();
                if(WatchPath.Any(c => invalidChars.Contains(c)))
                {
                    result.AddError($"WatchPath contains invalid characters: '{WatchPath}'");
                }

                // Check for absolute path (should be relative)
                if(Path.IsPathRooted(WatchPath))
                {
                    result.AddWarning($"WatchPath is an absolute path: '{WatchPath}'.  Relative paths are recommended for portability");
                }
            }

            // Validate that the watch directory can be resolved
            if(!string.IsNullOrEmpty(FileName))
            {
                try
                {
                    DirectoryInfo watchDir = GetAbsoluteWatchDirectory();
                    if(!watchDir.Exists)
                    {
                        result.AddWarning($"Watch directory does not exist: '{watchDir.FullName}'.  It will be created or you shoudl create it manually");
                    }
                }
                catch(Exception ex)
                {
                    result.AddError($"Cannot resolve watch directory: {ex.Message}");
                }
            }
        }

        private void ValidateExecutable(ConfigValidationResult result)
        {
            if(!string.IsNullOrEmpty(OpenWith))
            {
                // Check for invalid character paths
                char[] invalidChars = Path.GetInvalidPathChars();
                if(OpenWith.Any(c => invalidChars.Contains(c)))
                {
                    result.AddError($"OpenWith contains invalid path characers: '{OpenWith}'");
                    return;
                }

                // If it looks like a path, validate it
                if(OpenWith.Contains(Path.DirectorySeparatorChar) || OpenWith.Contains(Path.AltDirectorySeparatorChar))
                {
                    try
                    {
                        if(!File.Exists(OpenWith))
                        {
                            result.AddWarning($"OpenWith executable not found: '{OpenWith}'.  Verify the path is correct");
                        }
                    }
                    catch(Exception ex)
                    {
                        result.AddError($"Cannot validate OpenWith path: {ex.Message}");
                    }
                }

                // Warn if Argument specified without OpenWith
                if(string.IsNullOrEmpty(OpenWith) && !string.IsNullOrEmpty(Arguments))
                {
                    result.AddWarning("Arguments specified without OpenWith.  Arguments will be ignored when using default application.");
                }
            }
        }

        private void ValidateFallbackSuffixes(ConfigValidationResult result)
        {
            if(FallbackSuffixes != null && FallbackSuffixes.Count > 0)
            {
                // Check for empty or whitespace-only suffixes
                for(int i = 0; i < FallbackSuffixes.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(FallbackSuffixes[i]))
                    {
                        result.AddError($"FallbackSuffixes[{i}] is empty or whitespace.  Remove empty entries.");
                    }
                }

                // Check for duplicates
                var duplicates = FallbackSuffixes.GroupBy(e => e)
                                                 .Where(g => g.Count() > 1)
                                                 .Select(g => g.Key)
                                                 .ToList();

                if(duplicates.Count > 0)
                {
                    result.AddWarning($"Duplciate fallback suffixes: {string.Join(", ", duplicates.Select(d => $"'{d}'"))}.  Duplicates will be checked multiple times unnecessarily.");
                }

                // Warn about too many suffixes
                if(FallbackSuffixes.Count > 10)
                {
                    result.AddWarning($"Large number of fallback suffxes ({FallbackSuffixes.Count}).  This may impact lookup performance.  Consider reducing to 5-10 suffixes.");
                }
            }
        }
    }
}

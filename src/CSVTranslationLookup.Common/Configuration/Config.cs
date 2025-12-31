// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CSVTranslationLookup.Common.Utilities;
using Newtonsoft.Json;

namespace CSVTranslationLookup.Configuration
{
    /// <summary>
    /// Represents the configuration settings for CSV translation lookup.
    /// </summary>
    /// <remarks>
    /// Loaded from a JSON configuration file (csvconfig.json) and provides settings for CSV file monitoring,
    /// parsing behavior, and fallback token resolution. Supports validation of all settings with detailed
    /// error and warning messages. Default values are applied automatically for delimiter and quote characters
    /// if not specified in the configuration file.
    /// </remarks>
    public class Config
    {
        /// <summary>
        /// The name of the configuration file.
        /// </summary>
        public const string ConfigurationFilename = "csvconfig.json";

        /// <summary>
        /// Gets or sets the fully-qualified path to the configuration file.
        /// </summary>
        [JsonIgnore]
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the path relative to the configuration file to watch for CSV file changes.
        /// </summary>
        [JsonProperty("watchPath")]
        public string WatchPath { get; set; }

        /// <summary>
        /// Gets or sets the path to the executable used to open CSV files (e.g., VS Code, Notepad++).
        /// If <see langword="null"/> or empty, CSV files open with the Windows default application.
        /// </summary>
        [JsonProperty("openWith")]
        public string OpenWith { get; set; }

        /// <summary>
        /// Gets or sets the command-line arguments supplied to the executable when opening CSV files.
        /// Arguments prepended to the CSV file path when launching the executable.
        /// Only used when <see cref="OpenWith"/> is specified. Supports <c>{linenum}</c> placeholder
        /// for line number substitution.
        /// </summary>
        [JsonProperty("arguments")]
        public string Arguments { get; set; }

        /// <summary>
        /// Gets or sets the collection of suffixes to check as fallbacks when a token is not found.
        /// Suffixes are checked in the order they appear in the collection.
        /// For example, if the list contains ["_M", "_F"] and "ABILITY_NAME" is not found,
        /// the lookup will try "ABILITY_NAME_M" then "ABILITY_NAME_F".
        /// </summary>
        [JsonProperty("fallBackSuffixes")]
        public List<string> FallbackSuffixes { get; set; }

        /// <summary>
        /// Gets or sets the character that represents the delimiter used in the CSV files.
        /// Defaults to <c>,</c> (comma).
        /// </summary>
        [JsonProperty("delimiter")]
        public char Delimiter { get; set; }

        /// <summary>
        /// Gets or sets the character that represents the start and end of a quoted block in the CSV files.
        /// Defaults to <c>"</c> (double quote).
        /// </summary>
        [JsonProperty("quote")]
        public char Quote { get; set; }

        /// <summary>
        /// Gets or sets whether diagnostic logging is enabled.
        /// </summary>
        [JsonProperty("diagnostic")]
        public bool Diagnostic { get; set; }

        /// <summary>
        /// Gets the fully-qualified absolute path to the watch directory.
        /// </summary>
        /// <returns>A <see cref="DirectoryInfo"/> representing the watch directory.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="FileName"/> is not set or the watch directory cannot be resolved.
        /// </exception>
        /// <remarks>
        /// If <see cref="WatchPath"/> is specified, it is combined with the configuration file's directory.
        /// Otherwise, the configuration file's directory is used.
        /// </remarks>
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
                    string normalizedWatchPath = PathHelper.NormalizePath(WatchPath);
                    targetPath = PathHelper.GetFullPathSafe(PathHelper.SafeCombine(configDir, normalizedWatchPath));
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
        /// Creates a new instance of the <see cref="Config"/> class initialized from the values in the specified file.
        /// </summary>
        /// <param name="fileName">The path to the configuration file.</param>
        /// <param name="validationResult">
        /// When this method returns, contains the validation result with any errors or warnings,
        /// or <see langword="null"/> if the file doesn't exist.
        /// </param>
        /// <returns>
        /// A <see cref="Config"/> instance loaded from the file, or <see langword="null"/> if the file
        /// doesn't exist or JSON parsing failed.
        /// </returns>
        /// <remarks>
        /// Default values are applied automatically: delimiter defaults to <c>,</c>, quote defaults to <c>"</c>,
        /// and FallbackSuffixes is initialized to an empty list if not specified. The configuration is validated
        /// after loading, and results are returned via the <paramref name="validationResult"/> parameter.
        /// </remarks>
        public static Config FromFile(string fileName, out ConfigValidationResult validationResult)
        {
            validationResult = null;

            if (!File.Exists(fileName))
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
                catch (JsonException ex)
                {
                    validationResult = new ConfigValidationResult();
                    validationResult.AddError($"Invalid JSON in configuration file: {ex.Message}");
                    return null;
                }

                // Handle null result (empty file or null content)
                if (config == null)
                {
                    config = new Config();
                }

                config.FileName = fileName;

                // Apply defaults
                if (config.Delimiter == default(char))
                {
                    config.Delimiter = ',';
                }

                if (config.Quote == default(char))
                {
                    config.Quote = '"';
                }

                if (config.FallbackSuffixes == null)
                {
                    config.FallbackSuffixes = new List<string>();
                }

                validationResult = config.Validate();

                return config;
            }
            catch (Exception ex)
            {
                validationResult = new ConfigValidationResult();
                validationResult.AddError($"Failed to load configuration: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validates all configuration settings.
        /// </summary>
        /// <returns>
        /// A <see cref="ConfigValidationResult"/> containing any validation errors or warnings.
        /// </returns>
        /// <remarks>
        /// Validates delimiter and quote characters, path settings, executable configuration,
        /// and fallback suffixes. Errors indicate critical problems that will prevent proper operation.
        /// Warnings indicate potential issues or suboptimal configurations.
        /// </remarks>
        public ConfigValidationResult Validate()
        {
            var result = new ConfigValidationResult();

            ValidateDelimiterAndQuote(result);
            ValidatePaths(result);
            ValidateExecutable(result);
            ValidateFallbackSuffixes(result);

            return result;
        }

        /// <summary>
        /// Validates the delimiter and quote character settings.
        /// </summary>
        /// <param name="result">The validation result to populate with errors or warnings.</param>
        /// <remarks>
        /// Checks that delimiter and quote are not null characters, not the same character,
        /// and not newline characters (which would break CSV parsing).
        /// </remarks>
        public void ValidateDelimiterAndQuote(ConfigValidationResult result)
        {
            // Check for null characters
            if (Delimiter == default(char))
            {
                result.AddError("Delimiter cannot be null.  Use ',' for comma-separated values");
            }

            if (Quote == default(char))
            {
                result.AddError("Quote cannot be null character.  Use '\"' for double-quote.");
            }

            // Check for same character
            if (Delimiter != default(char) && Quote != default(char) && Delimiter == Quote)
            {
                result.AddError($"Delimiter and Quote cannot be the same character ('{Delimiter}'). CSV parsing will fail.");
            }

            // Check for problematic characters
            if (Delimiter == '\n' || Delimiter == '\r')
            {
                result.AddError("Delimiter cannot be a newline character.  This will break CSV parsing.");
            }

            if (Quote == '\n' || Quote == '\r')
            {
                result.AddError("Quote cannot be a newline character.  This will break CSV parsing.");
            }
        }

        /// <summary>
        /// Validates the watch path setting.
        /// </summary>
        /// <param name="result">The validation result to populate with errors or warnings.</param>
        /// <remarks>
        /// Checks for invalid path characters, warns if an absolute path is used (relative paths
        /// are recommended for portability), and verifies that the watch directory can be resolved
        /// and exists.
        /// </remarks>
        private void ValidatePaths(ConfigValidationResult result)
        {
            // Validate the watch path
            if (!string.IsNullOrEmpty(WatchPath))
            {
                // Check for invalid path characters
                char[] invalidChars = Path.GetInvalidFileNameChars();
                if (WatchPath.Any(c => invalidChars.Contains(c)))
                {
                    result.AddError($"WatchPath contains invalid characters: '{WatchPath}'");
                }

                // Check for absolute path (should be relative)
                if (Path.IsPathRooted(WatchPath))
                {
                    result.AddWarning($"WatchPath is an absolute path: '{WatchPath}'.  Relative paths are recommended for portability");
                }
            }

            // Validate that the watch directory can be resolved
            if (!string.IsNullOrEmpty(FileName))
            {
                try
                {
                    DirectoryInfo watchDir = GetAbsoluteWatchDirectory();
                    if (!watchDir.Exists)
                    {
                        result.AddWarning($"Watch directory does not exist: '{watchDir.FullName}'.  It will be created or you should create it manually");
                    }
                }
                catch (Exception ex)
                {
                    result.AddError($"Cannot resolve watch directory: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Validates the OpenWith executable and Arguments settings.
        /// </summary>
        /// <param name="result">The validation result to populate with errors or warnings.</param>
        /// <remarks>
        /// Checks for invalid path characters in OpenWith, verifies the executable exists if a path
        /// is provided, and warns if Arguments are specified without OpenWith (arguments would be ignored).
        /// </remarks>
        private void ValidateExecutable(ConfigValidationResult result)
        {
            if (!string.IsNullOrEmpty(OpenWith))
            {
                // Check for invalid character paths
                char[] invalidChars = Path.GetInvalidPathChars();
                if (OpenWith.Any(c => invalidChars.Contains(c)))
                {
                    result.AddError($"OpenWith contains invalid path characters: '{OpenWith}'");
                    return;
                }

                // If it looks like a path (contains directory separators), validate it exists
                if (OpenWith.Contains(Path.DirectorySeparatorChar) || OpenWith.Contains(Path.AltDirectorySeparatorChar))
                {
                    try
                    {
                        if (!File.Exists(OpenWith))
                        {
                            result.AddWarning($"OpenWith executable not found: '{OpenWith}'.  Verify the path is correct");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.AddError($"Cannot validate OpenWith path: {ex.Message}");
                    }
                }

                // Warn if Argument specified without OpenWith
                if (string.IsNullOrEmpty(OpenWith) && !string.IsNullOrEmpty(Arguments))
                {
                    result.AddWarning("Arguments specified without OpenWith.  Arguments will be ignored when using default application.");
                }
            }
        }

        /// <summary>
        /// Validates the fallback suffixes collection.
        /// </summary>
        /// <param name="result">The validation result to populate with errors or warnings.</param>
        /// <remarks>
        /// Checks for empty or whitespace-only suffixes, warns about duplicates (which cause
        /// unnecessary repeated lookups), and warns if there are more than 10 suffixes (which
        /// may impact lookup performance).
        /// </remarks>
        private void ValidateFallbackSuffixes(ConfigValidationResult result)
        {
            if (FallbackSuffixes != null && FallbackSuffixes.Count > 0)
            {
                // Check for empty or whitespace-only suffixes
                for (int i = 0; i < FallbackSuffixes.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(FallbackSuffixes[i]))
                    {
                        result.AddError($"FallbackSuffixes[{i}] is empty or whitespace.  Remove empty entries.");
                    }
                }

                // Check for duplicates (causes unnecessary repeated lookups)
                var duplicates = FallbackSuffixes.GroupBy(e => e)
                                                 .Where(g => g.Count() > 1)
                                                 .Select(g => g.Key)
                                                 .ToList();

                if (duplicates.Count > 0)
                {
                    result.AddWarning($"Duplicate fallback suffixes: {string.Join(", ", duplicates.Select(d => $"'{d}'"))}.  Duplicates will be checked multiple times unnecessarily.");
                }

                // Warn about too many suffixes (performance impact)
                if (FallbackSuffixes.Count > 10)
                {
                    result.AddWarning($"Large number of fallback suffixes ({FallbackSuffixes.Count}).  This may impact lookup performance.  Consider reducing to 5-10 suffixes.");
                }
            }
        }
    }
}

// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSVTranslationLookup.Common.IO;
using CSVTranslationLookup.Common.Text;
using CSVTranslationLookup.Common.Tokens;
using CSVTranslationLookup.Common.Utilities;
using CSVTranslationLookup.Configuration;
using EnvDTE80;
using Microsoft.VisualStudio.VCProjectEngine;

namespace CSVTranslationLookup.Services
{
    public sealed class CSVTranslationLookupService : IDisposable
    {
        // Wait time before processing a file change.
        private const int DebounceDelayMilliseconds = 500;

        // Number of times to retry on file lock.
        private const int MaxRetryAttempts = 3;

        // Base delay between retries (gets multiplied by attempt number for exponential backoff).
        private const int RetryDelayMilliseconds = 100;

        // Ignore duplicate events within this window.
        private const int DuplicateEventWindowMilliseconds = 50;

        private readonly ConcurrentDictionary<string, Token> _tokens = new ConcurrentDictionary<string, Token>(Environment.ProcessorCount, 31);
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _fileChangeDebounce = new ConcurrentDictionary<string, CancellationTokenSource>();
        private readonly ConcurrentDictionary<string, DateTime> _lastEventTime = new ConcurrentDictionary<string, DateTime>();
        private readonly ConcurrentDictionary<string, Token> _resolvedTokenCache = new ConcurrentDictionary<string, Token>();
        private FileSystemWatcher _watcher;
        private DTE2 DTE => CSVTranslationLookupPackage.DTE;
        private bool _isDisposed;

        public static Config Config { get; private set; }

        private void EnsureWatcher()
        {
            if(_watcher != null)
            {
                return;
            }

            _watcher = new FileSystemWatcher
            {
                Filter = "*.csv",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = false
            };

            _watcher.Changed += CSVChanged;
            _watcher.Created += CSVCreated;
            _watcher.Deleted += CSVDeleted;
            _watcher.Renamed += CSVRenamed;
        }

        /// <summary>Processes a configuration file and initializes the CSV translation lookup service.</summary>
        /// <param name="configFile">The path to the configuration file to process.</param>
        /// <exception cref="ObjectDisposedException">Throw if this CSVTranslationLookupServerice has been disposed of.</exception>
        public void ProcessConfig(string configFile)
        {
            if(_isDisposed)
            {
                throw new ObjectDisposedException(nameof(CSVTranslationLookupService));
            }

            // If we have already loaded a configuration file previously either during the initialization of this
            // extension or after one was created in a project, and this new configuration fiel is not hte same
            // file as the one we're already using, then we ignore.  Only use one configuration file.
            if(Config != null && PathHelper.ArePathsEqual(Config.FileName, configFile))
            {
                return;
            }

            EnsureWatcher();
            _watcher.EnableRaisingEvents = false;

            // Attempt to load the configuration from file.  This should only ever throw an exception if the JSON
            // in the configuration file is malformed
            try
            {
                Config = Config.FromFile(configFile, out ConfigValidationResult validationResult);

                // Check if the file was loaded successfully
                if(Config == null)
                {
                    string errorMessage = "Failed to load configuration file.";
                    if(validationResult != null && validationResult.Errors.Count > 0)
                    {
                        errorMessage = validationResult.GetFormattedMessage();
                    }

                    Logger.Log(errorMessage);
                    ShowError("Configuration file could not be loaded.  See See CSVTranslationLookup in Output Panel for details.");
                    CSVTranslationLookupPackage.StatusText("Configuration file error; check Output Panel");
                    return;
                }

                // Check validation result
                if(validationResult != null)
                {
                    if(!validationResult.IsValid)
                    {
                        // Configuration has errors, log and notify
                        Logger.Log(validationResult.GetFormattedMessage());
                        ShowError($"Configuration file has errors:\n\n{validationResult.GetFormattedMessage()}\n\nPlease fix these issues in {Path.GetFileName(configFile)}");
                        CSVTranslationLookupPackage.StatusText("Configuration validation failed; check Output panel");
                        return;
                    }

                    if(validationResult.Warnings.Count > 0)
                    {
                        // Configuration has warnings, log but continue
                        Logger.Log($"Configuration loaded with warnings:\n{validationResult.GetFormattedMessage()}");
                        CSVTranslationLookupPackage.StatusText($"Configuration loaded with {validationResult.Warnings.Count} warning(s)");
                    }
                    else
                    {
                        Logger.Log("Configuration loaded succsssfully with no issues");
                    }
                }


                if(Config.Diagnostic)
                {
                    StringBuilder builder = StringBuilderCache.Get();
                    builder.AppendLine("Configuration file loaded with the following values:");
                    builder.Append("  ").Append(nameof(Config.WatchPath)).Append(": ").AppendLine(string.IsNullOrEmpty(Config.WatchPath) ? "(current directory)" : Config.WatchPath);
                    builder.Append("  ").Append(nameof(Config.OpenWith)).Append(": ").AppendLine(string.IsNullOrEmpty(Config.OpenWith) ? "(default application)" : Config.OpenWith);
                    builder.Append("  ").Append(nameof(Config.Arguments)).Append(": ").AppendLine(string.IsNullOrEmpty(Config.Arguments) ? "(none)" : Config.Arguments);

                    if (Config.FallbackSuffixes != null && Config.FallbackSuffixes.Count > 0)
                    {
                        builder.Append("  ").Append(nameof(Config.FallbackSuffixes)).Append(": [").Append(string.Join(", ", Config.FallbackSuffixes)).AppendLine("]");
                    }
                    else
                    {
                        builder.Append("  ").Append(nameof(Config.FallbackSuffixes)).Append(": [ ]").AppendLine();
                    }

                    builder.Append("  ").Append(nameof(Config.Delimiter)).Append(": '").Append(Config.Delimiter).AppendLine("'");
                    builder.Append("  ").Append(nameof(Config.Quote)).Append(": '").Append(Config.Quote).AppendLine("'");
                    builder.Append("  ").Append(nameof(Config.Diagnostic)).Append(": ").AppendLine(Config.Diagnostic.ToString());
                    Logger.Log(builder.GetStringAndRecycle());
                }
            }
            catch(Exception ex)
            {
                string message = "There was an unexpected error loading the configuration file. See CSVTranslationLookup in Output Panel for details.";
                ShowError(message);
                CSVTranslationLookupPackage.StatusText("Configuration error - check Output panel");
                Logger.Log($"Unexpected error loading configuration from {configFile}:");
                Logger.Log(ex);
                return;
            }
            finally
            {
                DTE.StatusBar.Progress(false);
            }

            // Try to process the CSV files to get the keyword tokens
            // Exceptions are logged in the CSVTranslationOutput panel.
            try
            {
                _tokens.Clear();
                _resolvedTokenCache.Clear();

                if (Config?.Diagnostic ?? false)
                {
                    Logger.Log("Resolved token cache cleared");
                }
            
            
                DirectoryInfo dir = Config.GetAbsoluteWatchDirectory();

                ParallelQuery<ParallelQuery<TokenizedRow>> query = dir.GetFiles("*.csv")
                                                                      .AsParallel()
                                                                      .WithDegreeOfParallelism(Environment.ProcessorCount)
                                                                      .Select(x =>
                                                                      {
                                                                          if (Config.Diagnostic)
                                                                          {
                                                                              Logger.Log("Processing: " + x.FullName);
                                                                          }
                                                                          return CSVFileProcessor.ProcessFile(x.FullName, Config.Delimiter, Config.Quote);
                                                                      });

                foreach(ParallelQuery<TokenizedRow> rowQuery in query)
                {
                    AddTokens(rowQuery);
                }

                _watcher.Path = dir.FullName;
                _watcher.EnableRaisingEvents = true;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
                DTE.StatusBar.Progress(false);
                return;
            }
        }

        /// <summary>Attempts to retrieve a token by its key.</summary>
        /// <param name="key">The key to search for.</param>
        /// <param name="token">
        /// When this method returns, contains the token associated with the specified key, if found; otherwise, null.
        /// </param>
        /// <returns>true if the token was foundn; otherwise, false.</returns>
        /// <remarks>
        /// This method first checks a resolved token cache for quick lookups.  If not cached,
        /// it searches the main token dictionary.  If not faound nd fallback suffixes are configured
        /// it tries each suffix in order until a match is found or all suffixes are exhausted.
        ///
        /// Both successful and failed lookups are cached to avoid repeated work.  The cache is
        /// cleared when CSV viles are modified to ensure fresh lookups reflect file changes.
        ///
        /// Example: If key is "ABILITY_NAME" and fallback suffixes are ["_M", "_F"] then seearch order is:
        ///
        /// 1. Check cache for "ABILITY_NAME"
        /// 2. Try "ABILITY_NAME" in token dictionary
        /// 3. Try "ABILITY_NAME_M" in token dictionary
        /// 4. Try "ABILITY_NAME_F" in token dictionary
        /// 5. Cache result (or null) and return
        /// </remarks>
        public bool TryGetToken(string key, out Token token)
        {
            if(_isDisposed)
            {
                token = null;
                return false;
            }

            // Check cache first
            if(_resolvedTokenCache.TryGetValue(key, out token))
            {
                return token != null;
            }

            // Try direct lookup
            if(_tokens.TryGetValue(key, out token))
            {
                _resolvedTokenCache.TryAdd(key, token);
                return true;
            }

            // Try fallback suffixes if configured
            if(Config?.FallbackSuffixes != null && Config.FallbackSuffixes.Count > 0)
            {
                foreach(string suffix in Config.FallbackSuffixes)
                {
                    string fallbackKey = key + suffix;
                    if(_tokens.TryGetValue(fallbackKey, out token))
                    {
                        // Cache successful fallback lookup using the original key as cache key
                        _resolvedTokenCache.TryAdd(key, token);

                        if (Config.Diagnostic)
                        {
                            Logger.Log($"Token Lookup: '{key}' resolved via fallback '{fallbackKey}'");
                        }

                        return true;
                    }
                }
            }

            // Cache negative result to avoid repeated failed looups
            _resolvedTokenCache.TryAdd(key, null);
            token = null;
            return false;
        }

        private async Task RetryFileOperationsAsync(Action operation, CancellationToken cancellationToken)
        {
            int attempt = 0;
            while(attempt < MaxRetryAttempts)
            {
                try
                {
                    operation();
                    return;
                }
                catch(IOException) when (attempt < MaxRetryAttempts - 1)
                {
                    // File might be locked, wait and retry
                    attempt++;
                    await Task.Delay(RetryDelayMilliseconds * attempt, cancellationToken);
                }
                catch(UnauthorizedAccessException) when (attempt < MaxRetryAttempts - 1)
                {
                    // File might be locked, wait and retry
                    attempt++;
                    await Task.Delay(RetryDelayMilliseconds * attempt, cancellationToken);
                }
            }
        }

        private async void ProcessFileChangeAsync(string filePath, string removeOldPath = null)
        {
            if(_isDisposed)
            {
                return;
            }

            // Cancel any existing operations fo rthis file
            if(_fileChangeDebounce.TryGetValue(filePath, out var existingCts))
            {
                existingCts.Cancel();
                existingCts.Dispose();
            }

            var cts = new CancellationTokenSource();
            _fileChangeDebounce[filePath] = cts;

            try
            {
                // Debounce, wait for file operations to settle
                await Task.Delay(DebounceDelayMilliseconds, cts.Token);

                // Perform the file processsing with retry logic
                await RetryFileOperationsAsync(() =>
                {
                    if (!string.IsNullOrEmpty(removeOldPath))
                    {
                        RemoveTokensByFileName(removeOldPath);
                    }

                    RemoveTokensByFileName(filePath);

                    if (File.Exists(filePath))
                    {
                        ParallelQuery<TokenizedRow> rows = CSVFileProcessor.ProcessFile(filePath, Config.Delimiter, Config.Quote);
                        AddTokens(rows);
                    }
                }, cts.Token);

                if(Config.Diagnostic)
                {
                    Logger.Log($"Successfully processed file change: {filePath}");
                }
            }
            catch(OperationCanceledException)
            {
                // Operation was cancelled by a newer change, this is expected
                if(Config.Diagnostic)
                {
                    Logger.Log($"File change processing cancelled for: {filePath}");
                }
            }
            catch(Exception ex)
            {
                Logger.Log($"Error processing file change for {filePath}: {ex.Message}");
                Logger.Log(ex);
            }
            finally
            {
                _fileChangeDebounce.TryRemove(filePath, out _);
                cts.Dispose();
            }
        }

        private bool IsDuplicateEvent(string filePath)
        {
            DateTime now = DateTime.UtcNow;

            if(_lastEventTime.TryGetValue(filePath, out DateTime lastTime))
            {
                double timeSinceLastEvent = (now - lastTime).TotalMilliseconds;
                if(timeSinceLastEvent < DuplicateEventWindowMilliseconds)
                {
                    // This is likely a duplicated event
                    if(Config.Diagnostic)
                    {
                        Logger.Log($"Ignoring duplicate event for {filePath} (oly {timeSinceLastEvent:F0}ms since last event");
                    }
                    return true;
                }
            }

            // Updat elast event time
            _lastEventTime[filePath] = now;
            return false;
        }

        /// <summary>
        /// Triggered when a watched CSV file is deleted.  All token keywords associated with that file are removed
        /// from the items collection.
        /// </summary>
        private void CSVDeleted(object sender, FileSystemEventArgs e)
        {
            if(_isDisposed)
            {
                return;
            }

            // Filter out duplicate events (many editors trigger Changed multiple times)
            if(IsDuplicateEvent(e.FullPath))
            {
                return;
            }

            Logger.Log($"{e.FullPath} was deleted, removing all entries associated with that file");
            RemoveTokensByFileName(e.FullPath);
        }

        /// <summary>
        /// Triggered when a csv file in the watched directory is changed.  All token keywords associated with that
        /// file are first removed and then the new tokens added back.
        /// </summary>
        private void CSVChanged(object sender, FileSystemEventArgs e)
        {
            if (_isDisposed || !File.Exists(e.FullPath) || !PathHelper.HasExtension(e.FullPath, ".csv"))
            {
                return;
            }

            // Filter out duplicate events (many editors trigger Changed multiple times)
            if (IsDuplicateEvent(e.FullPath))
            {
                return;
            }

            Logger.Log($"'{e.FullPath}' was changed, updating entries...");
            ProcessFileChangeAsync(e.FullPath);
        }

        /// <summary>
        /// Triggered when a csv file in the watched directory is created.  All token keywords in the CSV file are
        /// added to the internal token collection.
        /// </summary>
        private void CSVCreated(object sender, FileSystemEventArgs e)
        {
            if (_isDisposed || !File.Exists(e.FullPath) || !PathHelper.HasExtension(e.FullPath, ".csv"))
            {
                return;
            }

            Logger.Log($"'{e.FullPath}' was created, updating entries...");
            ProcessFileChangeAsync(e.FullPath);
        }

        /// <summary>
        /// Triggered when a csv file in the watched directory is renamed.  All token keywors from the old filepath
        /// are removed and tokens from the new filepath are added.
        /// </summary>
        private void CSVRenamed(object sender, RenamedEventArgs e)
        {
            if (_isDisposed || !File.Exists(e.FullPath) || !PathHelper.HasExtension(e.FullPath, ".csv"))
            {
                return;
            }

            Logger.Log($"'{e.OldFullPath}' was renamed, updating filepath for all entities associated with that file");
            ProcessFileChangeAsync(e.FullPath, e.OldFullPath);
        }

        private void AddTokens(ParallelQuery<TokenizedRow> rows)
        {
            foreach(TokenizedRow row in rows)
            {
                // Row must have at minimum 2 tokens, a key and value
                if(row.Tokens.Length < 2)
                {
                    continue;
                }

                Token key = row.Tokens[0];
                Token value = row.Tokens[1];

                // Ensure key
                if(string.IsNullOrEmpty(key.Content))
                {
                    continue;
                }

                // Skip if this is the 'key':'en' row
                if(key.Content.Equals("key", StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                // Skip if this is a key only row, this means it's a comment row
                if(string.IsNullOrEmpty(value.Content))
                {
                    continue;
                }

                if(Config.Diagnostic)
                {
                    Logger.Log($"Adding Token: {key.Content}:{value.Content}");
                }

                _tokens.AddOrUpdate(key.Content, value, (k, v) => value);
            }
        }

        private void RemoveTokensByFileName(string fileName)
        {
            ParallelQuery<string> query = _tokens.AsParallel()
                                                 .WithDegreeOfParallelism(Environment.ProcessorCount)
                                                 .Where(kvp => kvp.Value.FileName == fileName)
                                                 .Select(kvp => kvp.Key);

            foreach (string key in query)
            {
                _tokens.TryRemove(key, out _);
            }
        }

        private static void ShowError(string message)
        {
            MessageBox.Show
            (
                message,
                Vsix.Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.ServiceNotification
            );
        }

        public void Dispose()
        {
            if(_isDisposed)
            {
                return;
            }

            if(_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Changed -= CSVChanged;
                _watcher.Created -= CSVCreated;
                _watcher.Deleted -= CSVDeleted;
                _watcher.Renamed -= CSVRenamed;
                _watcher.Dispose();
                _watcher = null;
            }

            foreach(var kvp in _fileChangeDebounce)
            {
                kvp.Value.Cancel();
                kvp.Value.Dispose();
            }
            _fileChangeDebounce.Clear();
            _lastEventTime.Clear();

            _tokens.Clear();
            _resolvedTokenCache.Clear();
            _isDisposed = true;
        }
    }
}

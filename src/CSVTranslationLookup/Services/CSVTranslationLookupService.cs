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
using CSVTranslationLookup.Utilities;
using Microsoft.VisualStudio.Shell.Interop;

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
        private bool _isDisposed;

        public static Config Config { get; private set; }

        private void EnsureWatcher()
        {
            if (_watcher != null)
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
        public async Task ProcessConfigAsync(string configFile)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(CSVTranslationLookupService));
            }

            // If we have already loaded a configuration file previously either during the initialization of this
            // extension or after one was created in a project, and this new configuration fiel is not hte same
            // file as the one we're already using, then we ignore.  Only use one configuration file.
            if (Config != null && PathHelper.ArePathsEqual(Config.FileName, configFile))
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
                if (Config == null)
                {
                    string errorMessage = "Failed to load configuration file.";
                    if (validationResult != null && validationResult.Errors.Count > 0)
                    {
                        errorMessage = validationResult.GetFormattedMessage();
                    }

                    await ErrorHandler.HandleAsync(
                        context: "loading configuration file",
                        userMessage: "Configuration file could not be loaded",
                        suggestion: $"{errorMessage}\n\nCheck csvconfig.json for errors.",
                        severity: ErrorHandler.ErrorSeverity.Error
                    );

                    return;
                }

                // Check validation result for errors
                if (validationResult != null && !validationResult.IsValid)
                {
                    await ErrorHandler.HandleAsync(
                        context: "validating configuration file",
                        userMessage: "Configuration file has errors",
                        suggestion: $"{validationResult.GetFormattedMessage()}\n\nPlease fix these issues in {PathHelper.SafeGetFileName(configFile)}",
                        severity: ErrorHandler.ErrorSeverity.Error
                    );
                }

                // Check validation result for warnings (non-blocking)
                if (validationResult != null && validationResult.Warnings.Count > 0)
                {
                    await ErrorHandler.ShowWarningAsync(
                        message: $"Configuration loaded with {validationResult.Warnings.Count} warning(s)",
                        suggestion: validationResult.GetFormattedMessage(),
                        showDialog: false
                    );
                }


                if (Config.Diagnostic)
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
            catch (Exception ex)
            {
                await ErrorHandler.HandleAsync(
                    context: "loading configuration file",
                    exception: ex
                );
                return;
            }

            // Process CSV files
            DirectoryInfo dir = null;
            FileInfo[] csvFiles = null;

            try
            {
                _tokens.Clear();
                _resolvedTokenCache.Clear();

                if (Config?.Diagnostic ?? false)
                {
                    Logger.Log("Token caches cleared");
                }

                // Get the watch directory            
                dir = Config.GetAbsoluteWatchDirectory();

                // Get CSV files
                csvFiles = dir.GetFiles("*.csv");

                if (csvFiles.Length == 0)
                {
                    await ErrorHandler.ShowWarningAsync(
                        message: $"No CSV files found in {dir.FullName}",
                        suggestion: "Add CSV files to the watch directory or check the watchPath configuration.",
                        showDialog: false
                    );

                    _watcher.Path = dir.FullName;
                    _watcher.EnableRaisingEvents = true;
                    return;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                await ErrorHandler.HandleAsync(
                    context: "accessing CSV directory",
                    exception: ex,
                    suggestion: "Try running Visual Studio as administrator or check folder permissions."
                );
                return;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleAsync(
                    context: "resolving CSV watch directory",
                    exception: ex,
                    suggestion: "Check that the watchPath in csvconfig.json is correct."
                );
                return;
            }

            // Process files with progress reporting
            using (var progress = new ProgressReporter("Processing CSV files", csvFiles.Length, logProgress: Config.Diagnostic))
            {
                try
                {
                    ParallelQuery<(FileInfo file, ParallelQuery<TokenizedRow> rows)> query =
                        csvFiles.AsParallel()
                                .WithDegreeOfParallelism(Environment.ProcessorCount)
                                .Select(file =>
                                {
                                    var rows = CSVFileProcessor.ProcessFile(file.FullName, Config.Delimiter, Config.Quote);
                                    progress.ReportProgress(file.Name);
                                    return (file, rows);
                                });

                    foreach (var (file, rowQuery) in query)
                    {
                        AddTokens(rowQuery);
                    }

                    _watcher.Path = dir.FullName;
                    _watcher.EnableRaisingEvents = true;

                    progress.Complete();

                    await ErrorHandler.ShowSuccessAsync(
                        message: $"Successfully loaded {_tokens.Count} translation keys from {csvFiles.Length}CSV file{(csvFiles.Length != 1 ? "s" : string.Empty)}",
                        showDialog: false
                    );
                }
                catch (Exception ex)
                {
                    progress.Cancel();

                    await ErrorHandler.HandleAsync(
                        context: "processing CSV files",
                        exception: ex,
                        suggestion: "Check that CSV files are not locked by another application and use valid format."
                    );
                }
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
            if (_isDisposed)
            {
                token = null;
                return false;
            }

            // Check cache first
            if (_resolvedTokenCache.TryGetValue(key, out token))
            {
                return token != null;
            }

            // Try direct lookup
            if (_tokens.TryGetValue(key, out token))
            {
                _resolvedTokenCache.TryAdd(key, token);
                return true;
            }

            // Try fallback suffixes if configured
            if (Config?.FallbackSuffixes != null && Config.FallbackSuffixes.Count > 0)
            {
                foreach (string suffix in Config.FallbackSuffixes)
                {
                    string fallbackKey = key + suffix;
                    if (_tokens.TryGetValue(fallbackKey, out token))
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
            while (attempt < MaxRetryAttempts)
            {
                try
                {
                    operation();
                    return;
                }
                catch (IOException) when (attempt < MaxRetryAttempts - 1)
                {
                    // File might be locked, wait and retry
                    attempt++;
                    await Task.Delay(RetryDelayMilliseconds * attempt, cancellationToken);
                }
                catch (UnauthorizedAccessException) when (attempt < MaxRetryAttempts - 1)
                {
                    // File might be locked, wait and retry
                    attempt++;
                    await Task.Delay(RetryDelayMilliseconds * attempt, cancellationToken);
                }
            }
        }

        private async void ProcessFileChangeAsync(string filePath, string removeOldPath = null)
        {
            if (_isDisposed)
            {
                return;
            }

            string fileName = PathHelper.SafeGetFileName(filePath);

            // Cancel any existing operations fo rthis file
            if (_fileChangeDebounce.TryGetValue(filePath, out var existingCts))
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

                await CSVTranslationLookupPackage.StatusTextAsync($"Reloading {fileName}...");

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

                await CSVTranslationLookupPackage.StatusTextAsync($"Reloaed {fileName}");

                if (Config.Diagnostic)
                {
                    Logger.Log($"Successfully processed file change: {filePath}");
                }
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled by a newer change, this is expected
                if (Config.Diagnostic)
                {
                    Logger.Log($"File change processing cancelled for: {filePath}");
                }
            }
            catch(IOException)
            {
                // File might be locked
                if(Config.Diagnostic)
                {
                    Logger.Log($"File templorarily locked, will retry on next change: {fileName}");
                }

                await ErrorHandler.ShowWarningAsync(
                    message: $"Could not reloat {fileName}; file may be locked.",
                    suggestion: "The file will be reloaded automatically when it beocmes avaialble.",
                    showDialog: false
                );
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleAsync(
                    context: $"reloading CSV file '{fileName}'",
                    exception: ex,
                    suggestion: "Check that the file is not locked by another application and uses valid CSV format.",
                    showDialog: false
                );
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

            if (_lastEventTime.TryGetValue(filePath, out DateTime lastTime))
            {
                double timeSinceLastEvent = (now - lastTime).TotalMilliseconds;
                if (timeSinceLastEvent < DuplicateEventWindowMilliseconds)
                {
                    // This is likely a duplicated event
                    if (Config.Diagnostic)
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
            if (_isDisposed)
            {
                return;
            }

            // Filter out duplicate events (many editors trigger Changed multiple times)
            if (IsDuplicateEvent(e.FullPath))
            {
                return;
            }

            string fileName = PathHelper.SafeGetFileName(e.FullPath);

            Logger.Log($"{e.FullPath} was deleted, removing all entries associated with that file");

            RemoveTokensByFileName(e.FullPath);

            _ = CSVTranslationLookupPackage.StatusTextAsync($"Removed tokens from deleted file: {fileName}");
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

            string fileName = PathHelper.SafeGetFileName(e.FullPath);

            Logger.Log($"'{fileName}' was changed, updating entries...");

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

            string fileName = PathHelper.SafeGetFileName(e.FullPath);

            Logger.Log($"'{fileName}' was created, updating entries...");

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

            string oldFileName = PathHelper.SafeGetFileName(e.OldFullPath);
            string newFileName = PathHelper.SafeGetFileName(e.FullPath);

            Logger.Log($"File renamed: {oldFileName} → {newFileName}, updating entries...");

            ProcessFileChangeAsync(e.FullPath, e.OldFullPath);
        }

        private void AddTokens(ParallelQuery<TokenizedRow> rows)
        {
            foreach (TokenizedRow row in rows)
            {
                // Row must have at minimum 2 tokens, a key and value
                if (row.Tokens.Length < 2)
                {
                    continue;
                }

                Token key = row.Tokens[0];
                Token value = row.Tokens[1];

                // Ensure key
                if (string.IsNullOrEmpty(key.Content))
                {
                    continue;
                }

                // Skip if this is the 'key':'en' row
                if (key.Content.Equals("key", StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                // Skip if this is a key only row, this means it's a comment row
                if (string.IsNullOrEmpty(value.Content))
                {
                    continue;
                }

                if (Config.Diagnostic)
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
                                                 .Where(kvp => PathHelper.ArePathsEqual(kvp.Value.FileName, fileName))
                                                 .Select(kvp => kvp.Key);

            int count = 0;
            foreach (string key in query)
            {
                _tokens.TryRemove(key, out _);
                _resolvedTokenCache.TryRemove(key, out _);
                count++;
            }

            if(Config?.Diagnostic ?? false)
            {
                Logger.Log($"Cleared {count} tokens from cache for file: {PathHelper.SafeGetFileName(fileName)}");
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Changed -= CSVChanged;
                _watcher.Created -= CSVCreated;
                _watcher.Deleted -= CSVDeleted;
                _watcher.Renamed -= CSVRenamed;
                _watcher.Dispose();
                _watcher = null;
            }

            foreach (var kvp in _fileChangeDebounce)
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

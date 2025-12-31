// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSVTranslationLookup.Common.IO;
using CSVTranslationLookup.Common.Tokens;
using CSVTranslationLookup.Common.Utilities;
using CSVTranslationLookup.Configuration;
using CSVTranslationLookup.Utilities;

namespace CSVTranslationLookup.Services
{
    /// <summary>
    /// Manages CSV translation token lookups with file system monitoring and caching.
    /// </summary>
    /// <remarks>
    /// This service watches a configured directory for CSV file changes and maintains an in-memory
    /// dictionary of translation tokens.
    /// </remarks>
    public sealed class CSVTranslationLookupService : IDisposable
    {
        // Wait time before processing a file change.
        private const int DebounceDelayMilliseconds = 500;

        // Number of times to retry on file lock.
        private const int MaxRetryAttempts = 3;

        // Base delay between retries (gets multiplied by attempt number for exponential back off).
        private const int RetryDelayMilliseconds = 100;

        // Ignore duplicate events within this window.
        private const int DuplicateEventWindowMilliseconds = 50;

        /// <summary>
        /// Main token dictionary mapping translation keys to their resolved values.
        /// </summary>
        /// <remarks>
        /// Initialized with concurrency level equal to processor count and initial capacity of 31
        /// for performance.
        /// </remarks>
        private readonly ConcurrentDictionary<string, Token> _tokens = new ConcurrentDictionary<string, Token>(Environment.ProcessorCount, 31);

        /// <summary>
        /// Tracks cancellation tokens for ongoing file change operations to enable debouncing.
        /// </summary>
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _fileChangeDebounce = new ConcurrentDictionary<string, CancellationTokenSource>();

        /// <summary>
        /// Records the last time a file system event was processed for each file path to filter duplicate events.
        /// </summary>
        private readonly ConcurrentDictionary<string, DateTime> _lastEventTime = new ConcurrentDictionary<string, DateTime>();

        /// <summary>
        /// Caches resolved token lookups including fallback suffix resolution to avoid repeated searches.
        /// </summary>
        /// <remarks>
        /// Maps original lookup keys to their resolved tokens.  Null values are cached to avoid repeatedly
        /// searching for non-existent keys.
        /// </remarks>
        private readonly ConcurrentDictionary<string, Token> _resolvedTokenCache = new ConcurrentDictionary<string, Token>();

        /// <summary>
        /// Monitors the configured directory for CSVfile changes.
        /// </summary>
        private FileSystemWatcher _watcher;

        /// <summary>
        /// Tracks whether this instance has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Gets the currently loaded configuration.
        /// </summary>
        public static Config Config { get; private set; }

        /// <summary>
        /// Initializes the file system watcher if not already created.
        /// </summary>
        /// <remarks>
        /// The watcher is configured to monitor CSV files for creating, modification, deletion, and rename events.
        /// Events are not raised until explicitly enabled after configuration is loaded.
        /// </remarks>
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

        /// <summary>
        /// Processes a configuration file and initializes the CSV translation lookup service.
        /// </summary>
        /// <param name="configFile">The path to the configuration file to process.</param>
        /// <returns>The result of this asynchronous operation.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed of.</exception>
        /// <remarks>
        /// If a configuration has already been loaded and the specified file is the same,
        /// the method returns early.  Only one configuration file is active at a time.
        /// This method validates the configuration, loads all CSV files from the watch directory,
        /// and enables file system monitoring.
        /// </remarks>
        public async Task ProcessConfigAsync(string configFile)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(CSVTranslationLookupService));
            }

            // If we have already loaded a configuration file previously either during the initialization of this
            // extension or after one was created in a project, and this new configuration file is not the same
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
                    Logger.LogBatch(
                        "Configuration file loaded with the following values:",
                        $"  {nameof(Config.WatchPath)}: {Config.WatchPath ?? "(current directory)"}",
                        $"  {nameof(Config.OpenWith)}: {Config.OpenWith ?? "(default application)"}",
                        $"  {nameof(Config.Arguments)}: {Config.Arguments ?? "(none)"}",
                        $"  {nameof(Config.FallbackSuffixes)}: [{string.Join(", ", Config.FallbackSuffixes ?? new List<string>())}]",
                        $"  {nameof(Config.Delimiter)}: '{Config.Delimiter}'" +
                        $"  {nameof(Config.Quote)}: '{Config.Quote}'",
                        $"  {nameof(Config.Diagnostic)}: {Config.Diagnostic}"
                    );
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

                    // Enable watching even if no files exist yet, so new files can be detected.
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

        /// <summary>
        /// Attempts to retrieve a token by its key.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <param name="token">
        /// When this method returns, contains the token associated with the specified key, if found; otherwise, <see langword="null"/>.
        /// </param>
        /// <returns><see langword="true"/> if the token was found; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// This method first checks a resolved token cache for quick lookups. If not cached,
        /// it searches the main token dictionary. If not found and fallback suffixes are configured,
        /// it tries each suffix in order until a match is found or all suffixes are exhausted.
        /// Both successful and failed lookups are cached to avoid repeated work. The cache is
        /// cleared when CSV files are modified to ensure fresh lookups reflect file changes.
        /// </remarks>
        /// <example>
        /// If key is "ABILITY_NAME" and fallback suffixes are ["_M", "_F"], the search order is:
        /// <list type="number">
        /// <item>Check cache for "ABILITY_NAME"</item>
        /// <item>Try "ABILITY_NAME" in token dictionary</item>
        /// <item>Try "ABILITY_NAME_M" in token dictionary</item>
        /// <item>Try "ABILITY_NAME_F" in token dictionary</item>
        /// <item>Cache result (or null) and return</item>
        /// </list>
        /// </example>
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

            // Cache negative result to avoid repeated failed lookups
            _resolvedTokenCache.TryAdd(key, null);
            token = null;
            return false;
        }

        /// <summary>
        /// Retries a file operation with exponential back off on lock or access errors.
        /// </summary>
        /// <param name="operation">The file operation to execute.</param>
        /// <param name="cancellationToken">Token to cancel the retry attempts.</param>
        /// <remarks>
        /// Retries up to <see cref="MaxRetryAttempts"/> times with increasing delays.
        /// The delay increases linearly: attempt 1 waits <see cref="RetryDelayMilliseconds"/>,
        /// attempt 2 waits 2× that amount, and so on.
        /// </remarks>
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

        /// <summary>
        /// Processes a CSV file change with debouncing and retry logic.
        /// </summary>
        /// <param name="filePath">The path to the changed CSV file.</param>
        /// <param name="removeOldPath">Optional path of a renamed file to remove before processing the new file.</param>
        /// <remarks>
        /// This method debounces rapid file changes by waiting <see cref="DebounceDelayMilliseconds"/>
        /// before processing. If another change occurs during the wait, the previous operation is cancelled.
        /// File operations are retried on lock conflicts using exponential back off.
        /// </remarks>
        private async void ProcessFileChangeAsync(string filePath, string removeOldPath = null)
        {
            if (_isDisposed)
            {
                return;
            }

            string fileName = PathHelper.SafeGetFileName(filePath);

            // Cancel any existing operations for this file
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

                // Perform the file processing with retry logic
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

                await CSVTranslationLookupPackage.StatusTextAsync($"Reloaded {fileName}");

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
            catch (IOException)
            {
                // File might be locked
                if (Config.Diagnostic)
                {
                    Logger.Log($"File temporarily locked, will retry on next change: {fileName}");
                }

                await ErrorHandler.ShowWarningAsync(
                    message: $"Could not reload {fileName}; file may be locked.",
                    suggestion: "The file will be reloaded automatically when it becomes available.",
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

        /// <summary>
        /// Determines if a file system event is a duplicate within the configured time window.
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        /// <returns>
        /// <see langword="true"/> if this event occurred within <see cref="DuplicateEventWindowMilliseconds"/>
        /// of the last event for the same file; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Many text editors trigger multiple file system events for a single save operation.
        /// This method filters out duplicates to avoid unnecessary reprocessing.
        /// </remarks>
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
                        Logger.Log($"Ignoring duplicate event for {filePath} (only {timeSinceLastEvent:F0}ms since last event");
                    }
                    return true;
                }
            }

            // Update last event time
            _lastEventTime[filePath] = now;
            return false;
        }

        /// <summary>
        /// Handles the deletion of a watched CSV file by removing all associated tokens.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">File system event data.</param>
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
        /// Handles changes to a watched CSV file by removing old tokens and reloading from the file.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">File system event data.</param>
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
        /// Handles creation of a new CSV file in the watched directory by loading its tokens.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">File system event data.</param>
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
        /// Handles renaming of a watched CSV file by removing tokens from the old path and loading from the new path.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Renamed event data containing both old and new file paths.</param>
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

        /// <summary>
        /// Adds tokens from processed CSV rows to the internal dictionary.
        /// </summary>
        /// <param name="rows">The tokenized rows to process.</param>
        /// <remarks>
        /// Rows must have at least two tokens (key and value). Rows with empty keys, header rows
        /// (key equals "key"), and comment rows (key-only with no value) are skipped.
        /// </remarks>
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

        /// <summary>
        /// Removes all tokens associated with a specific CSV file.
        /// </summary>
        /// <param name="fileName">The file path whose tokens should be removed.</param>
        /// <remarks>
        /// Clears tokens from both the main dictionary and the resolved token cache.
        /// </remarks>
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

            if (Config?.Diagnostic ?? false)
            {
                Logger.Log($"Cleared {count} tokens from cache for file: {PathHelper.SafeGetFileName(fileName)}");
            }
        }

        /// <summary>
        /// Releases all resources used by the service.
        /// </summary>s
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

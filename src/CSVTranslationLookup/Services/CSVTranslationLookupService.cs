// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CSVTranslationLookup.Common.IO;
using CSVTranslationLookup.Common.Text;
using CSVTranslationLookup.Common.Tokens;
using CSVTranslationLookup.Configuration;
using EnvDTE80;

namespace CSVTranslationLookup.Services
{
    public sealed class CSVTranslationLookupService : IDisposable
    {
        private readonly ConcurrentDictionary<string, Token> _tokens = new ConcurrentDictionary<string, Token>(Environment.ProcessorCount, 31);
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
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName,
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
            if (Config is not null && Config.FileName.Equals(Path.GetFileName(configFile), StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            EnsureWatcher();
            _watcher.EnableRaisingEvents = false;

            // Attempt to load the configuration from file.  This should only ever throw an exception if the JSON
            // in the configuration file is malformed
            try
            {
                Config = Config.FromFile(configFile);
                if(Config.Diagnostic)
                {
                    StringBuilder builder = StringBuilderCache.Get();
                    builder.AppendLine("Configuration file loaded with the following values:");
                    builder.Append(nameof(Config.WatchPath)).Append(": ").AppendLine(Config.WatchPath);
                    builder.Append(nameof(Config.OpenWith)).Append(": ").AppendLine(Config.OpenWith);
                    builder.Append(nameof(Config.Arguments)).Append(": ").AppendLine(Config.Arguments);
                    if (Config.FallbackSuffixes.Count > 0)
                    {
                        builder.Append(nameof(Config.FallbackSuffixes)).Append(": [").Append(string.Join(", ", Config.FallbackSuffixes)).AppendLine("]");
                    }
                    else
                    {
                        builder.Append(nameof(Config.FallbackSuffixes)).Append(": [ ]").AppendLine();
                    }
                    builder.Append(nameof(Config.Delimiter)).Append(": ").AppendLine(Config.Delimiter.ToString());
                    builder.Append(nameof(Config.Quote)).Append(": ").AppendLine(Config.Quote.ToString());
                    builder.Append(nameof(Config.Diagnostic)).Append(": ").AppendLine(Config.Diagnostic.ToString());
                    Logger.Log(builder.GetStringAndRecycle());
                }
            }
            catch(Exception ex)
            {
                string message = "There was an error loading the configuration file.  See CSVTranslationLookup in Output Panel for details.";
                ShowError(message);
                CSVTranslationLookupPackage.StatusText(message);
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
        public bool TryGetToken(string key, out Token token)
        {
            if(_isDisposed)
            {
                token = null;
                return false;
            }

            return _tokens.TryGetValue(key, out token);
        }

        /// <summary>
        /// Triggered when a watched CSV file is deleted.  All token keywords associated with that file are rmeoved
        /// from the items collection.
        /// </summary>
        private void CSVDeleted(object sender, FileSystemEventArgs e)
        {
            if(_isDisposed)
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
            if(_isDisposed || !File.Exists(e.FullPath) || Path.GetExtension(e.FullPath) != ".csv")
            {
                return;
            }

            Logger.Log($"'{e.FullPath}' was changed, updating entries...");
            RemoveTokensByFileName(e.FullPath);
            ParallelQuery<TokenizedRow> rows = CSVFileProcessor.ProcessFile(e.FullPath, Config.Delimiter, Config.Quote);
            AddTokens(rows);
            Logger.Log("Update finished");
        }

        /// <summary>
        /// Triggered when a csv file in the watched directory is created.  All token keywords in the CSV file are
        /// added to the internal token collection.
        /// </summary>
        private void CSVCreated(object sender, FileSystemEventArgs e)
        {
            if (_isDisposed || !File.Exists(e.FullPath) || Path.GetExtension(e.FullPath) != ".csv")
            {
                return;
            }

            Logger.Log($"'{e.FullPath}' was created, updating entries...");
            ParallelQuery<TokenizedRow> rows = CSVFileProcessor.ProcessFile(e.FullPath, Config.Delimiter, Config.Quote);
            AddTokens(rows);
            Logger.Log("Update finished");
        }

        /// <summary>
        /// Triggered when a csv file in the watched directory is renamed.  All token keywors from the old filepath
        /// are removed and tokens from the new filepath are added.
        /// </summary>
        private void CSVRenamed(object sender, RenamedEventArgs e)
        {
            if (_isDisposed || !File.Exists(e.FullPath) || Path.GetExtension(e.FullPath) != ".csv")
            {
                return;
            }

            Logger.Log($"'{e.OldFullPath}' was renamed, updating filepath for all entities associated with that file");
            RemoveTokensByFileName(e.OldFullPath);
            ParallelQuery<TokenizedRow> rows = CSVFileProcessor.ProcessFile(e.FullPath, Config.Delimiter, Config.Quote);
            AddTokens(rows);
            Logger.Log("Update finished");
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

            _tokens.Clear();
            _isDisposed = true;
        }
    }
}

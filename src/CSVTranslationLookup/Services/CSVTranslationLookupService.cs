// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using CSVTranslationLookup.Common.IO;
using CSVTranslationLookup.Common.Tokens;
using CSVTranslationLookup.Configuration;
using CSVTranslationLookup.CSV;
using EnvDTE80;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace CSVTranslationLookup.Services
{
    internal static class CSVTranslationLookupService
    {
        private readonly static ConcurrentDictionary<string, Token> _tokens = new ConcurrentDictionary<string, Token>(Environment.ProcessorCount, 31);
        private static readonly FileSystemWatcher _watcher;
        private static DTE2 DTE => CSVTranslationLookupPackage.DTE;

        public static Config Config { get; private set; }

        static CSVTranslationLookupService()
        {
            _watcher = new FileSystemWatcher();
            _watcher.Path = Path.GetDirectoryName(DTE.Solution.FullName);
            _watcher.Filter = "*.csv";
            _watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName;
            _watcher.Changed += CSVChanged;
            _watcher.Created += CSVCreated;
            _watcher.Deleted += CSVDeleted;
            _watcher.Renamed += CSVRenamed;
            _watcher.EnableRaisingEvents = false;
        }

        public static void ProcessConfig(string configFile)
        {
            //  If we have already loaded a configuration file previously either during the initialization of this
            //  extension or after one was created in a project, and this new configuraiton file is not the same
            //  file as the one we're already using, then we ignore.  Only use one configuration file.
            if (Config is not null && Config.FileName.Equals(Path.GetFileName(configFile), StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            _watcher.EnableRaisingEvents = false;

            try
            {

                Config = Config.FromFile(configFile);
                _tokens.Clear();

                DirectoryInfo dir = Config.GetAbsoluteWatchDirectory();

                ParallelQuery<ParallelQuery<TokenizedRow>> query = dir.GetFiles("*.csv")
                                                                      .AsParallel()
                                                                      .WithDegreeOfParallelism(Environment.ProcessorCount)
                                                                      .Select(x => CSVFileProcessor.ProcessFile(x.FullName, Config.Delimiter, Config.Quote));

                foreach (ParallelQuery<TokenizedRow> rowQuery in query)
                {
                    AddTokens(rowQuery);
                }

                _watcher.Path = dir.FullName;
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                string message = $"{Vsix.Name} could not find the configuration file at {configFile}";
                Logger.Log(message);
                CSVTranslationLookupPackage.StatusText(message);
                DTE.StatusBar.Progress(false);
                return;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                ShowError(configFile);
                DTE.StatusBar.Progress(false);
                return;
            }
            finally
            {
                DTE.StatusBar.Progress(false);
            }
        }

        public static bool TryGetToken(string key, out Token token) => _tokens.TryGetValue(key, out token);


        /// <summary>
        /// Triggered when a watched CSV file is deleted.  All token keywords associated with that file are rmeoved
        /// from the items collection.
        /// </summary>
        private static void CSVDeleted(object sender, FileSystemEventArgs e)
        {
            Logger.Log($"{e.FullPath} was deleted, removing all entries associated with that file");
            RemoveTokensByFileName(e.FullPath);
        }

        /// <summary>
        /// Triggered when a csv file in the watched directory is changed.  All token keywords associated with that
        /// file are first removed and then the new tokens added back.
        /// </summary>
        private static void CSVChanged(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(e.FullPath) || Path.GetExtension(e.FullPath) != ".csv")
            {
                return;
            }
            Logger.Log($"'{e.FullPath}' was changed, updating entries");
            RemoveTokensByFileName(e.FullPath);
            ParallelQuery<TokenizedRow> rows = CSVFileProcessor.ProcessFile(e.FullPath, Config.Delimiter, Config.Quote);
            AddTokens(rows);
        }

        /// <summary>
        /// Triggered when a csv file in the watched directory is created.  All token keywords in the CSV file are
        /// added to the internal token collection.
        /// </summary>
        private static void CSVCreated(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(e.FullPath) || Path.GetExtension(e.FullPath) != ".csv")
            {
                return;
            }
            Logger.Log($"'{e.FullPath}' was created, updating entries");
            ParallelQuery<TokenizedRow> rows = CSVFileProcessor.ProcessFile(e.FullPath, Config.Delimiter, Config.Quote);
            AddTokens(rows);
        }

        /// <summary>
        /// Triggered when a csv file in the watched directory is renamed.  All token keywors from the old filepath
        /// are removed and tokens from the new filepath are added.
        /// </summary>
        private static void CSVRenamed(object sender, RenamedEventArgs e)
        {
            if (!File.Exists(e.FullPath) || Path.GetExtension(e.FullPath) != ".csv")
            {
                return;
            }
            Logger.Log($"'{e.OldFullPath}' was renamed, updating filepath for all entities associted with that file");
            RemoveTokensByFileName(e.OldFullPath);
            ParallelQuery<TokenizedRow> rows = CSVFileProcessor.ProcessFile(e.FullPath, Config.Delimiter, Config.Quote);
            AddTokens(rows);
        }


        private static void AddTokens(ParallelQuery<TokenizedRow> rows)
        {
            foreach (TokenizedRow row in rows)
            {
                //  Row must have at minimum 2 tokens, a key and value
                if (row.Tokens.Length < 2)
                {
                    continue;
                }

                Token key = row.Tokens[0];
                Token value = row.Tokens[1];

                _tokens.AddOrUpdate(key.Content, value, (k, v) => value);
            }
        }

        private static void RemoveTokensByFileName(string fileName)
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

        private static void ShowError(string configFile)
        {
            MessageBox.Show
            (
                $"There is an error in the {Config.ConfigurationFilename} file.",
                Vsix.Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.ServiceNotification
            );
        }
    }
}

// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using CSVTranslationLookup.Configuration;
using CSVTranslationLookup.CSV;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;

namespace CSVTranslationLookup
{
    internal static class CSVTranslationLookupService
    {
        private static Config s_config;
        private static DTE2 _dte;

        private static ConfigFileProcessor _configProcessor;
        private static CSVProcessor _csvProcessor;
        private static FileSystemWatcher _csvWatcher;

        private static ConfigFileProcessor ConfigProcessor
        {
            get
            {
                if (_configProcessor is null)
                {
                    _configProcessor = new ConfigFileProcessor();
                    _configProcessor.ConfigProcessed += ConfigProcessed;
                }

                return _configProcessor;
            }
        }

        private static CSVProcessor CSVProcessor
        {
            get
            {
                if(_csvProcessor is null)
                {
                    _csvProcessor = new CSVProcessor();
                    _csvProcessor.CSVProcessed += CSVProcessed;
                }

                return _csvProcessor;
            }
        }

        public static Dictionary<string, CSVItem> Items { get; } = new Dictionary<string, CSVItem>();

        public static Config Config => s_config;
        


        private static void CSVProcessed(object sender, CSVProcessedEventArgs e)
        {
            foreach(var kvp in e.Items)
            {
                if (Items.ContainsKey(kvp.Key))
                {
                    //  Will overwrite the item if the key is duplicated in multiple files.
                    Items[kvp.Key] = kvp.Value;
                }
                else
                {
                    Items.Add(kvp.Key, kvp.Value);
                }
            }
        }

        static CSVTranslationLookupService()
        {
            _dte = CSVTranslationLookupPackage.DTE;
        }

        public static void ProcessConfig(string configFile)
        {
            //  If we have already loaded a configuration file previously either during the initialization of this
            //  extension or after one was created in a project, and this new configuraiton file is not the same
            //  file as the one we're already using, then we ignore.  Only use one configuration file.
            if(Config is not null && Config.FileName.Equals(Path.GetFileName(configFile), StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    ConfigProcessor.Process(configFile);
                }
                catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
                {
                    string message = $"{Vsix.Name} cound not find configuration file at '{configFile}'.";
                    Logger.Log(message);
                    CSVTranslationLookupPackage.StatusText(message);
                    _dte.StatusBar.Progress(false);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    ShowError(configFile);
                    _dte.StatusBar.Progress(false);
                    CSVTranslationLookupPackage.StatusText($"{Vsix.Name} couldn't process configuration successfully");
                }
                finally
                {
                    _dte.StatusBar.Progress(false);
                }
            });
        }

        private static void ConfigProcessed(object sender, ConfigProcessedEventArgs e)
        {
            //  Tell the CSV Proessor to process the CSV files at this point.
            CSVTranslationLookupPackage.StatusText("Config file processed");
            s_config = e.Config;

            DirectoryInfo dir = e.Config.GetAbsoluteWatchDirectory();
            if (_csvWatcher is null)
            {
                _csvWatcher = new FileSystemWatcher(dir.FullName, "*.csv");
                _csvWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName;
                _csvWatcher.Changed += CSVChanged;
                _csvWatcher.Created += CSVCreated;
                _csvWatcher.Deleted += CSVDeleted;
                _csvWatcher.Renamed += CSVRenamed;
                _csvWatcher.EnableRaisingEvents = true;
            }
            FileInfo[] csvFiles = dir.GetFiles("*.csv", SearchOption.AllDirectories);
            for (int i = 0; i < csvFiles.Length; i++)
            {
                Logger.LogProgress(true, $"Processing CSV Files {i + 1}/{csvFiles.Length}", i, csvFiles.Length);
                CSVProcessor.Process(csvFiles[i].FullName);
            }
            Logger.LogProgress(false);
        }

        private static void CSVDeleted(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Deleted)
            {
                return;
            }

            Logger.Log($"'{e.FullPath}' was deleted, removing all entries associted with that file");

            IList<string> keysToRemove = Items.Where(kvp => kvp.Value.FilePath == e.FullPath)
                                              .Select(kvp => kvp.Key)
                                              .ToList();

            foreach(string key in keysToRemove)
            {
                Items.Remove(key);
            }
        }

        private static void CSVChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }

            FileInfo file = new FileInfo(e.FullPath);
            if (!file.Exists)
            {
                return;
            }

            CSVProcessor.Process(e.FullPath);
            Logger.Log($"'{e.FullPath}' was updated, updating entries");
        }

        private static void CSVCreated(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Created)
            {
                return;
            }

            FileInfo file = new FileInfo(e.FullPath);
            if (!file.Exists)
            {
                return;
            }

            CSVProcessor.Process(e.FullPath);
            Logger.Log($"'{e.FullPath}' was created, updating entries");
        }

        private static void CSVRenamed(object sender, RenamedEventArgs e)
        {
            if(e.ChangeType != WatcherChangeTypes.Renamed)
            {
                return;
            }

            FileInfo file = new FileInfo(e.FullPath);
            if(!file.Exists)
            {
                return;
            }

            Logger.Log($"'{e.OldFullPath}' was renamed, updating filepath for all entities associted with that file");

            IList<string> keysToChange = Items.Where(kvp => kvp.Value.FilePath == e.OldFullPath)
                                              .Select(kvp => kvp.Key)
                                              .ToList();

            foreach (string key in keysToChange)
            {
                Items[key].FilePath = e.FullPath;
            }



        }
        private static void ShowError(string configFile)
        {
            MessageBox.Show
            (
                $"There is an error in the {Constants.CONFIGURATION_FILENAME} file.",
                Vsix.Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.ServiceNotification
            );
        }
    }
}

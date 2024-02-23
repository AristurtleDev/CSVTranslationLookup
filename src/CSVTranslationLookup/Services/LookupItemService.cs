// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Community.VisualStudio.Toolkit;
using CSVTranslationLookup.Configuration;
using Newtonsoft.Json;

namespace CSVTranslationLookup.Services
{
    internal class LookupItemService
    {
        private CSVTranslationLookupSettings _settings;
        public bool _started;
        private Dictionary<string, LookupItem> _lookup;
        private FileSystemWatcher _csvWatcher;
        private FileSystemWatcher _settingsWatcher;
        private string _solutionDirectory;

        public CSVTranslationLookupSettings Settings => _settings;


        public LookupItemService(CSVTranslationLookupSettings settings, string solutionDirectory)
        {
            _solutionDirectory = solutionDirectory;

            _settings = settings ?? new CSVTranslationLookupSettings();
            if (string.IsNullOrEmpty(_settings.WatchPath) || !Directory.Exists(_settings.WatchPath))
            {
                _settings.WatchPath = _solutionDirectory;
            }

            if (string.IsNullOrEmpty(_settings.Delimiter))
            {
                _settings.Delimiter = ",";
            }

            _settingsWatcher = new FileSystemWatcher(Path.GetDirectoryName(_settings.SettingsFilePath), CSVTranslationLookupSettings.FileName);
            _settingsWatcher.IncludeSubdirectories = false;
            _settingsWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite;
            _settingsWatcher.Changed += ReloadSettings;
            _settingsWatcher.Created += ReloadSettings;
            _settingsWatcher.EnableRaisingEvents = true;
        }

        private void ReloadSettings(object sender, FileSystemEventArgs e)
        {
            string oldWatchPath = _settings.WatchPath;
            string json = File.ReadAllText(e.FullPath);
            _settings = JsonConvert.DeserializeObject<CSVTranslationLookupSettings>(json);

            if (_settings is null)
            {
                throw new InvalidOperationException($"There was an issue reading the {CSVTranslationLookupSettings.FileName} settings file");
            }

            if (string.IsNullOrEmpty(_settings.WatchPath) || !Directory.Exists(_settings.WatchPath))
            {
                _settings.WatchPath = _solutionDirectory;
            }

            if (string.IsNullOrEmpty(_settings.Delimiter))
            {
                _settings.Delimiter = ",";
            }

            //  If the watch path changes, we need to regenerate the lookup map :/
            if (!oldWatchPath.Equals(_settings.WatchPath, StringComparison.CurrentCultureIgnoreCase))
            {
                _csvWatcher.Path = _settings.WatchPath;
                GenerateMap();
            }
        }

        /// <summary>
        /// Starts the service
        /// </summary>
        public bool Start()
        {
            if (_started)
            {
                return false;
            }

            GenerateMap();

            _csvWatcher = new FileSystemWatcher(_settings.WatchPath, "*.csv");
            _csvWatcher.IncludeSubdirectories = true;
            _csvWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite;
            _csvWatcher.Changed += OnCsvFileChanged;
            _csvWatcher.Created += OnCsvFileCreated;
            _csvWatcher.EnableRaisingEvents = true;

            return true;
        }


        /// <summary>
        /// Called whenever a new CSV file is created within the directory being watched.
        /// </summary>
        private void OnCsvFileCreated(object sender, FileSystemEventArgs e)
        {
            List<LookupItem> items = ReadFile(e.FullPath);
            foreach (LookupItem item in items)
            {
                AddItemToLookup(item);
            }
        }

        /// <summary>
        /// Called whenever a CSV file is changed within the diretory being watched.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCsvFileChanged(object sender, FileSystemEventArgs e)
        {
            List<LookupItem> items = ReadFile(e.FullPath);
            foreach (LookupItem item in items)
            {
                AddItemToLookup(item);
            }

        }

        /// <summary>
        /// Generates the lookup map from the CSV files in the watch directory.
        /// </summary>
        private void GenerateMap()
        {
            _lookup = new Dictionary<string, LookupItem>();

            string[] csvFiles = Directory.GetFiles(_settings.WatchPath, "*.csv", SearchOption.AllDirectories);

            for (int i = 0; i < csvFiles.Length; i++)
            {
                _ = VS.StatusBar.ShowProgressAsync($"Mapping CSV Key Files {i + 1} of {csvFiles.Length}", i + 1, csvFiles.Length);
                string path = csvFiles[i];
                List<LookupItem> items = ReadFile(path);

                foreach (LookupItem item in items)
                {
                    AddItemToLookup(item);
                }
            }
        }

        /// <summary>
        /// Adds a new item to the lookup map.
        /// </summary>
        private void AddItemToLookup(LookupItem item)
        {
            if (_lookup.ContainsKey(item.Key))
            {
                //  TODO: Could probably detect here if the key is already added but from
                //  a different file and warn the user so they know they have the key
                //  defined in two separate files.
                _lookup[item.Key] = item;
            }
            else
            {
                _lookup.Add(item.Key, item);
            }
        }

        /// <summary>
        /// Reads the contents of a csv file and generates a collection of lookup items to be added to
        /// the lookup map.
        /// </summary>
        private List<LookupItem> ReadFile(string path)
        {
            List<LookupItem> items = new List<LookupItem>();

            string[] lines = File.ReadAllLines(path);

            //  Start at index 1, skipping the first line as it's assumed to have the table headers.
            for (int i = 0; i < lines.Length; i++)
            {
                string[] columns = lines[i].Split(_settings.Delimiter[0]);
                LookupItem item = null;

                //  First column is key, second column is value, there ust be 2 columns minimum
                if (columns.Length >= 2)
                {
                    string key = columns[0].Trim();
                    string value = columns[1].Trim();

                    //  Sometimes there is a key with no value.  These are to be treated as comments
                    //  in the csv file and ignored
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        item = new LookupItem(key, value, i, path);
                        items.Add(item);
                    }
                }
            }

            return items;
        }


        public bool TryGetItem(string key, out LookupItem item)
        {
            bool found = _lookup.TryGetValue(key, out item);

            if (!found && Settings.FallbackSuffixes.Count > 0)
            {
                for (int i = 0; i < Settings.FallbackSuffixes.Count; i++)
                {
                    found = _lookup.TryGetValue($"{key}{Settings.FallbackSuffixes[i]}", out item);
                    if (found)
                    {
                        break;
                    }
                }
            }

            return found;
        }
    }
}

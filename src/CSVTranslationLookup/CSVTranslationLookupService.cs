// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using CSVTranslationLookup.Configuration;
using CSVTranslationLookup.CSV;
using EnvDTE80;

namespace CSVTranslationLookup
{
    internal static class CSVTranslationLookupService
    {
        private static string s_configFile;
        private static DTE2 _dte;

        private static ConfigFileProcessor _configProcessor;
        private static CSVProcessor _csvProcessor;

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

        private static void CSVProcessed(object sender, CSVProcessedEventArgs e)
        {
            foreach(var kvp in e.Items)
            {
                if (Items.ContainsKey(kvp.Key))
                {
                    if (!Items[kvp.Key].FilePath.Equals(kvp.Value.FilePath, StringComparison.InvariantCultureIgnoreCase))
                    {
                        Logger.Log($"Duplicate key '{kvp.Key}' found in '{kvp.Value.FilePath}' that exists in another file '{Items[kvp.Key].FilePath}'.  Value will be overridden with newest key.");
                    }

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
            if(!string.IsNullOrEmpty(s_configFile) && !s_configFile.Equals(configFile, StringComparison.InvariantCultureIgnoreCase))
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
            s_configFile = e.Config.FileName;

            DirectoryInfo dir = e.Config.GetAbsoluteWatchDirectory();
            FileInfo[] csvFiles = dir.GetFiles("*.csv", SearchOption.AllDirectories);
            for(int i =0; i < csvFiles.Length; i++)
            {
                Logger.LogProgress(true, $"Processing CSV Files {i+1}/{csvFiles.Length}", i, csvFiles.Length);
                CSVProcessor.Process(csvFiles[i].FullName, e.Config);
            }
            Logger.LogProgress(false);
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

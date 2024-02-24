// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using CSVTranslationLookup.Configuration;
using EnvDTE80;

namespace CSVTranslationLookup
{
    internal static class CSVTranslationLookupService
    {
        private static DTE2 _dte;

        private static ConfigFileProcessor _configProcessor;

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

        static CSVTranslationLookupService()
        {
            _dte = CSVTranslationLookupPackage.DTE;
        }

        public static void ProcessConfig(string configFile, bool force = false)
        {
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

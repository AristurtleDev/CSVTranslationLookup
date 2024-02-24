﻿// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CSVTranslationLookup.Configuration
{
    internal class ConfigFileProcessor
    {
        /// <summary>
        /// Triggeredwhen a config file has been processed.
        /// </summary>
        public event EventHandler<ConfigProcessedEventArgs> ConfigProcessed;

        public void Process(string configFile)
        {
            try
            {
                FileInfo file = new FileInfo(configFile);
                if (!file.Exists)
                {
                    return;
                }

                string json = File.ReadAllText(configFile);
                Config config = JsonConvert.DeserializeObject<Config>(json);
                config.FileName = configFile;
                OnConfigProcessed(config);
            }
            catch (Exception ex) { Logger.Log(ex); }
        }

        private void OnConfigProcessed(Config config)
        {
            if(ConfigProcessed is not null)
            {
                ConfigProcessed(this, new ConfigProcessedEventArgs(config));
            }
        }
    }
}

// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSVTranslationLookup.Configuration
{
    internal class ConfigProcessedEventArgs
    {
        /// <summary>
        /// Gets the config object that was created when processed.
        /// </summary>
        public Config Config { get; }


        public ConfigProcessedEventArgs(Config config)
        {
            Config = config;
        }
    }
}

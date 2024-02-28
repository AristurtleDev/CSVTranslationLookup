// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSVTranslationLookup.Common.Tokens;

namespace CSVTranslationLookup.Service
{
    public class CSVTranslationLookupService : ICSVTranslationLookupService
    {

        public CSVTranslationLookupService()
        {

        }

        public ParallelQuery<TokenizedRow> FindToken(string key)
        {
            
        }
    }
}

// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using CSVTranslationLookup.Common.Text;

namespace CSVTranslationLookup.Configuration
{
    public class ConfigValidationResult
    {
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _warnings = new List<string>();

        public bool IsValid => _errors.Count == 0;
        public IReadOnlyList<string> Errors => _errors;
        public IReadOnlyList<string> Warnings => _warnings;

        public void AddError(string error)
        {
            _errors.Add(error);
        }

        public void AddWarning(string warning)
        {
            _warnings.Add(warning);
        }

        public string GetFormattedMessage()
        {
            StringBuilder lines = StringBuilderCache.Get();


            if(_errors.Count > 0)
            {
                lines.AppendLine("Configuration Errors:");
                foreach(string error in _errors)
                {
                    lines.AppendLine($"    - {error}");
                }

                lines.AppendLine();
            }

            if(_warnings.Count > 0)
            {
                lines.AppendLine("Configuration Warnings:");
                foreach(string warning in _warnings)
                {
                    lines.AppendLine($"    - {warning}");
                }
            }

            return lines.GetStringAndRecycle();
        }

        public string GetSummary()
        {
            if (IsValid)
            {
                if(_warnings.Count > 0)
                {
                    return $"Configuration valid with {_warnings.Count} warning(s)";
                }
                return "Configuration valid";
            }

            return $"Configuration invalid: {_errors.Count} error(s), {_warnings.Count} warning(s)";
        }
    }
}

// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using CSVTranslationLookup.Common.Text;

namespace CSVTranslationLookup.Configuration
{
    /// <summary>
    /// Represents the result of configuration validation, containing errors and warnings.
    /// </summary>
    /// <remarks>
    /// Errors indicate critical issues that prevent proper operation, while warnings indicate
    /// potential problems or suboptimal configurations. The configuration is considered invalid
    /// if any errors are present, regardless of warnings.
    /// </remarks>
    public class ConfigValidationResult
    {
        /// <summary>
        /// Collection of validation errors.
        /// </summary>
        private readonly List<string> _errors = new List<string>();

        /// <summary>
        /// Collection of validation warnings.
        /// </summary>
        private readonly List<string> _warnings = new List<string>();

        /// <summary>
        /// Gets whether the configuration is valid.
        /// </summary>
        public bool IsValid => _errors.Count == 0;

        /// <summary>
        /// Gets the collection of validation errors.
        /// </summary>
        public IReadOnlyList<string> Errors => _errors;

        /// <summary>
        /// Gets the collection of validation warnings.
        /// </summary>
        public IReadOnlyList<string> Warnings => _warnings;

        /// <summary>
        /// Adds a validation error.
        /// </summary>
        /// <param name="error">The error message to add.</param>
        /// <remarks>
        /// Errors indicate critical problems that prevent proper operation.
        /// Adding an error sets <see cref="IsValid"/> to <see langword="false"/>.
        /// </remarks>
        public void AddError(string error)
        {
            _errors.Add(error);
        }

        /// <summary>
        /// Adds a validation warning.
        /// </summary>
        /// <param name="warning">The warning message to add.</param>
        /// <remarks>
        /// Warnings indicate potential issues or suboptimal configurations that don't
        /// prevent operation. Adding warnings does not affect <see cref="IsValid"/>.
        /// </remarks>
        public void AddWarning(string warning)
        {
            _warnings.Add(warning);
        }

        /// <summary>
        /// Gets a formatted message containing all errors and warnings.
        /// </summary>
        /// <returns>
        /// A multi-line string with errors and warnings formatted under separate headings,
        /// or an empty string if there are no errors or warnings.
        /// </returns>
        /// <remarks>
        /// The format is:
        /// <code>
        /// Configuration Errors:
        ///     - [error 1]
        ///     - [error 2]
        /// 
        /// Configuration Warnings:
        ///     - [warning 1]
        ///     - [warning 2]
        /// </code>
        /// Sections are omitted if no errors or warnings are present.
        /// </remarks>
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

        /// <summary>
        /// Gets a brief summary of the validation result.
        /// </summary>
        /// <returns>
        /// A single-line summary indicating whether the configuration is valid and the count
        /// of errors and/or warnings.
        /// </returns>
        /// <remarks>
        /// Example return values:
        /// <list type="bullet">
        /// <item>"Configuration valid" - No errors or warnings</item>
        /// <item>"Configuration valid with 2 warning(s)" - Valid but has warnings</item>
        /// <item>"Configuration invalid: 1 error(s), 0 warning(s)" - Has errors</item>
        /// </list>
        /// </remarks>
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

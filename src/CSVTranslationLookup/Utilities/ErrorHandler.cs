// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CSVTranslationLookup.Common.Text;

namespace CSVTranslationLookup.Utilities
{
    /// <summary>
    /// Provides centralized error handling with user friendly messages and logging.
    /// </summary>
    internal static class ErrorHandler
    {
        /// <summary>
        /// Represents teh severity level of an error.
        /// </summary>
        public enum ErrorSeverity
        {
            /// <summary>
            /// Informational message (not an error).
            /// </summary>
            Info,

            /// <summary>
            /// Warning (operation continues but user should be aware).
            /// </summary>
            Warning,

            /// <summary>
            /// Error (operation failed but extension continues).
            /// </summary>
            Error,

            /// <summary>
            /// Critical (extension functinality severely impaired).
            /// </summary>
            Critical
        }

        /// <summary>
        /// Handles an error with user friendly messaging and logging.
        /// </summary>
        /// <param name="context">What was being attempted when the error occured.</param>
        /// <param name="exception">The exception that occured.</param>
        /// <param name="userMessage">Optional user friendly message (will be generated if not provided).</param>
        /// <param name="suggestion">Optional suggestion for how to fix the problem (will be genereated if not provided).</param>
        /// <param name="severity">The severity level of the error.</param>
        /// <param name="showDialog">Whether to show a dialog to the user.</param>
        public static async Task HandleAsync(string context, Exception exception, string userMessage = null, string suggestion = null, ErrorSeverity severity = ErrorSeverity.Error, bool showDialog = true)
        {
            // Generate user message if not provided
            if (string.IsNullOrEmpty(userMessage))
            {
                userMessage = GenerateUserMessage(context, exception);
            }

            // Generate suggestion if not provided
            if (string.IsNullOrEmpty(suggestion))
            {
                suggestion = GenerateSuggestion(exception);
            }

            LogDetailedError(context, exception, userMessage, suggestion);
            await UpdateStatusBarAsync(userMessage, severity);

            if (showDialog)
            {
                ShowErrorDialog(userMessage, suggestion, context, exception, severity);
            }
        }

        /// <summary>
        /// Handles an error without an execption.
        /// </summary>
        /// <param name="context">What was being attempted.</param>
        /// <param name="userMessage">User firendly message.</param>
        /// <param name="suggestion">Optional suggesetion for how to fix the problem.</param>
        /// <param name="severity">The severity level of the error.</param>
        /// <param name="showDialog">Whether to show a dialog to the user.</param>
        public static async Task HandleAsync(string context, string userMessage, string suggestion = null, ErrorSeverity severity = ErrorSeverity.Error, bool showDialog = true)
        {
            LogDetailedError(context, null, userMessage, suggestion);
            await UpdateStatusBarAsync(userMessage, severity);

            if (showDialog)
            {
                ShowErrorDialog(userMessage, suggestion, context, null, severity);
            }
        }

        /// <summary>
        /// Shows a success message to the user.
        /// </summary>
        /// <param name="message">The success message.</param>
        /// <param name="showDialog">Whether to show a dialog to the user.</param>
        public static async Task ShowSuccessAsync(string message, bool showDialog = false)
        {
            Logger.Log($"SUCCESS: {message}");
            await CSVTranslationLookupPackage.StatusTextAsync(message);

            if (showDialog)
            {
                MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Shows a warning to the user.
        /// </summary>
        /// <param name="message">The warning message.</param>
        /// <param name="suggestion">Optional suggesetion for how to fix the problem.</param>
        /// <param name="showDialog">Whether to show a dialog to the user.</param>
        public static async Task ShowWarningAsync(string message, string suggestion = null, bool showDialog = false)
        {
            string fullMessage;

            if (string.IsNullOrEmpty(suggestion))
            {
                fullMessage = message;
            }
            else
            {
                fullMessage = $"{message}\n\nSuggestion: {suggestion}";
            }

            Logger.Log($"WARNING: {fullMessage}");
            await CSVTranslationLookupPackage.StatusTextAsync(message);

            if (showDialog)
            {
                MessageBox.Show(fullMessage, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Generates a user friendly message based on exception.
        /// </summary>
        private static string GenerateUserMessage(string context, Exception exception)
        {
            if (exception == null)
            {
                return $"An error occured while {context}.";
            }

            switch (exception)
            {
                case UnauthorizedAccessException:
                    return $"Access denied while {context}.  The file or folder may be read-only or require administrator permissions.";

                case FileNotFoundException:
                    return $"A required file was not found while {context}.";

                case DirectoryNotFoundException:
                    return $"A required directory was not found while {context}.";

                case IOException ioEx:
                    return $"An I/O error occured while {context}: {ioEx.Message}";

                case FormatException:
                    return $"Invalid format encountered while {context}.  The data may be corrupted or in an unexpected format.";

                case ArgumentException argEx:
                    return $"Invalid input while {context}: {argEx.Message}";

                case InvalidOperationException opEx:
                    return $"Operation failed while {context}: {opEx.Message}";

                case OutOfMemoryException:
                    return $"Out of memory while {context}.  Try closing other applications or processing fewer files.";

                case TimeoutException:
                    return $"Operation timed out while {context}.  The file may be locked by another application.";

                default:
                    return $"An unexpected error occured while {context}: {exception.Message}";
            }
        }

        /// <summary>
        /// Generates a suggeestion based on the exception type.
        /// </summary>
        private static string GenerateSuggestion(Exception exception)
        {
            if (exception == null)
            {
                return "Check the Output window for more details.";
            }

            switch (exception)
            {
                case UnauthorizedAccessException:
                    return "Try running Visual Studio as administrator, or check that the file/folder is not read-only.";

                case FileNotFoundException fnf:
                    return $"Ensure the file exists at the expected location:\n{fnf.FileName}";

                case DirectoryNotFoundException:
                    return "Check that the configured watch directory exists and the path is correct in csvconfig.json.";

                case IOException:
                    return "The file may be locked by another application. Close any programs that might be using the file and try again.";

                case FormatException:
                    return "Check that your CSV files use the correct delimiter and quote characters as configured in csvconfig.json.";

                case ArgumentException:
                    return "Check your configuration in csvconfig.json for invalid values.";

                case OutOfMemoryException:
                    return "Close other applications to free up memory, or try processing files in smaller batches.";

                case TimeoutException:
                    return "Close any applications that might have the file open, then try again.";

                default:
                    return "Check the Output window (View → Output → CSV Translation Lookup) for detailed error information.";
            }
        }

        private static void LogDetailedError(string context, Exception exception, string userMessage, string suggestion)
        {
            StringBuilder logMessage = StringBuilderCache.Get();
            logMessage.AppendLine("===========================================================");
            logMessage.AppendLine($"ERROR: {context}");
            logMessage.AppendLine("===========================================================");
            logMessage.AppendLine();
            logMessage.AppendLine("User Message:");
            logMessage.AppendLine(userMessage);
            logMessage.AppendLine();

            if (!string.IsNullOrEmpty(suggestion))
            {
                logMessage.AppendLine("Suggestion:");
                logMessage.AppendLine(suggestion);
                logMessage.AppendLine();
            }

            if (exception != null)
            {
                logMessage.AppendLine("Technical Details:");
                logMessage.AppendLine($"Exception Type: {exception.GetType().FullName}");
                logMessage.AppendLine($"Message: {exception.Message}");

                if (exception.InnerException != null)
                {
                    logMessage.AppendLine($"Inner Exception: {exception.InnerException.GetType().FullName}");
                    logMessage.AppendLine($"Inner Message: {exception.InnerException.Message}");
                }

                logMessage.AppendLine();
                logMessage.AppendLine("Stack Trace:");
                logMessage.AppendLine(exception.StackTrace);
            }

            logMessage.AppendLine("===========================================================");

            Logger.Log(logMessage.GetStringAndRecycle());
        }


        private static async Task UpdateStatusBarAsync(string message, ErrorSeverity severity)
        {
            string prefix = severity.ToString() + " ";
            string statusMessage = prefix + message;

            // Truncate log messages for status bar
            if (statusMessage.Length > 100)
            {
                statusMessage = statusMessage.Substring(0, 97) + "...";
            }

            await CSVTranslationLookupPackage.StatusTextAsync(statusMessage);

        }

        private static void ShowErrorDialog(string userMessage, string suggestion, string context, Exception exception, ErrorSeverity severity)
        {
            StringBuilder messageBuilder = StringBuilderCache.Get();
            messageBuilder.AppendLine(userMessage);
            messageBuilder.AppendLine();

            if (!string.IsNullOrEmpty(suggestion))
            {
                messageBuilder.AppendLine("Suggestion:");
                messageBuilder.AppendLine(suggestion);
                messageBuilder.AppendLine();
            }

            messageBuilder.AppendLine("Check the Ouptut window for detailed error information:");
            messageBuilder.AppendLine("View -> Output -> Select 'CSV Translation Lookup");

            string title;
            MessageBoxImage icon;
            switch (severity)
            {
                case ErrorSeverity.Info:
                    title = "Information";
                    icon = MessageBoxImage.Information;
                    break;

                case ErrorSeverity.Warning:
                    title = "Warning";
                    icon = MessageBoxImage.Warning;
                    break;

                case ErrorSeverity.Error:
                    title = "Error";
                    icon = MessageBoxImage.Error;
                    break;

                case ErrorSeverity.Critical:
                    title = "Critical Error";
                    icon = MessageBoxImage.Error;
                    break;

                default:
                    title = "Error";
                    icon = MessageBoxImage.Error;
                    break;
            }

            MessageBox.Show(messageBuilder.GetStringAndRecycle(), $"CSV Translation Lookup - {title}", MessageBoxButton.OK, icon);
        }

        /// <summary>
        /// Wraps an operation with error handling.
        /// </summary>
        /// <param name="context">Description of the operation.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="onError">Optional callback when error occurs.</param>
        /// <returns>true if operation succeeded, false if error occured.</returns>
        public static async Task<bool> TryExecuteAsync(string context, Action operation, Action<Exception> onError = null)
        {
            try
            {
                operation();
                return true;
            }
            catch (Exception ex)
            {
                await HandleAsync(context, ex);
                onError?.Invoke(ex);
                return false;
            }
        }

        /// <summary>
        /// Wraps an operation with error handling and returns a result.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="context">Description of the operation.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="defaultvalue">Default value to return on error.</param>
        /// <param name="onError">Optional callback when error occurs.</param>
        /// <returns>The operation rsult, or default value on error.</returns>
        public static async Task<T> TryExecuteAsync<T>(string context, Func<T> operation, T defaultvalue = default, Action<Exception> onError = null)
        {
            try
            {
                return operation();
            }
            catch (Exception ex)
            {
                await HandleAsync(context, ex);
                onError?.Invoke(ex);
                return defaultvalue;
            }
        }
    }
}

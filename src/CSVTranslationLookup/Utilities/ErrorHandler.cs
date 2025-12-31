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
    /// Provides centralized error handling with user-friendly messages and logging.
    /// </summary>
    /// <remarks>
    /// This handler automatically generates appropriate error messages and suggestions based on
    /// exception types, logs detailed diagnostics to the output window, updates the status bar,
    /// and optionally displays user-friendly dialogs. All methods are thread-safe and can be
    /// called from any context.
    /// </remarks>
    internal static class ErrorHandler
    {
        /// <summary>
        /// Represents the severity level of an error.
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
            /// Critical (extension functionality severely impaired).
            /// </summary>
            Critical
        }

        /// <summary>
        /// Handles an error with user-friendly messaging and logging.
        /// </summary>
        /// <param name="context">What was being attempted when the error occurred.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="userMessage">Optional user-friendly message. If not provided, a message will be generated based on the exception type.</param>
        /// <param name="suggestion">Optional suggestion for how to fix the problem. If not provided, a suggestion will be generated based on the exception type.</param>
        /// <param name="severity">The severity level of the error.</param>
        /// <param name="showDialog">Whether to show a dialog to the user. Default is <see langword="true"/>.</param>
        /// <remarks>
        /// This method performs three actions:
        /// <list type="number">
        /// <item>Logs detailed error information including stack trace to the output window</item>
        /// <item>Updates the status bar with a brief error message</item>
        /// <item>Optionally displays a user-friendly dialog with suggestions</item>
        /// </list>
        /// Messages and suggestions are automatically generated for common exception types
        /// (FileNotFoundException, UnauthorizedAccessException, etc.) if not explicitly provided.
        /// </remarks>
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
        /// Handles an error without an exception.
        /// </summary>
        /// <param name="context">What was being attempted.</param>
        /// <param name="userMessage">User-friendly message.</param>
        /// <param name="suggestion">Optional suggestion for how to fix the problem.</param>
        /// <param name="severity">The severity level of the error.</param>
        /// <param name="showDialog">Whether to show a dialog to the user. Default is <see langword="true"/>.</param>
        /// <remarks>
        /// Use this overload when an error condition is detected without an exception being thrown,
        /// such as validation failures or configuration errors. The error will be logged and
        /// displayed to the user based on the showDialog parameter.
        /// </remarks>
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
        /// <param name="showDialog">Whether to show a dialog to the user. Default is <see langword="false"/>.</param>
        /// <remarks>
        /// Success messages are logged and displayed in the status bar. Dialogs are typically
        /// not shown for success messages unless explicitly requested.
        /// </remarks>
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
        /// <param name="suggestion">Optional suggestion for how to fix the problem.</param>
        /// <param name="showDialog">Whether to show a dialog to the user. Default is <see langword="false"/>.</param>
        /// <remarks>
        /// Warnings indicate potential issues that don't prevent the operation from continuing.
        /// If a suggestion is provided, it will be appended to the message in both the log and dialog.
        /// </remarks>
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
        /// Generates a user-friendly message based on the exception type.
        /// </summary>
        /// <param name="context">The operation context.</param>
        /// <param name="exception">The exception to generate a message for.</param>
        /// <returns>A user-friendly error message appropriate for the exception type.</returns>
        /// <remarks>
        /// Provides specialized messages for common exception types including file I/O errors,
        /// access denied, format errors, and memory issues. Falls back to the exception's message
        /// for unknown exception types.
        /// </remarks>
        private static string GenerateUserMessage(string context, Exception exception)
        {
            if (exception == null)
            {
                return $"An error occurred while {context}.";
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
                    return $"An I/O error occurred while {context}: {ioEx.Message}";

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
                    return $"An unexpected error occurred while {context}: {exception.Message}";
            }
        }

        /// <summary>
        /// Generates a suggestion for resolving the error based on the exception type.
        /// </summary>
        /// <param name="exception">The exception to generate a suggestion for.</param>
        /// <returns>A helpful suggestion appropriate for the exception type.</returns>
        /// <remarks>
        /// Provides actionable guidance for resolving common issues such as file locks,
        /// permission problems, and configuration errors. Directs users to the output
        /// window for detailed diagnostics when specific guidance isn't available.
        /// </remarks>
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

        /// <summary>
        /// Logs error details to the output window.
        /// </summary>
        /// <param name="context">The operation context.</param>
        /// <param name="exception">The exception, if any.</param>
        /// <param name="userMessage">The user-friendly message.</param>
        /// <param name="suggestion">The suggested resolution.</param>
        /// <remarks>
        /// Logs include a formatted header, user message, suggestion, exception type and message,
        /// inner exception details, and full stack trace. The format is designed for easy reading
        /// and debugging.
        /// </remarks>
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

        /// <summary>
        /// Updates the Visual Studio status bar with an error message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="severity">The severity level, which is prepended to the message.</param>
        /// <remarks>
        /// Messages longer than 100 characters are truncated with ellipsis to fit the status bar.
        /// The severity level is displayed as a prefix (e.g., "Error: ...").
        /// </remarks>
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

        /// <summary>
        /// Displays an error dialog to the user with formatted message and suggestions.
        /// </summary>
        /// <param name="userMessage">The main error message.</param>
        /// <param name="suggestion">The suggested resolution.</param>
        /// <param name="context">The operation context.</param>
        /// <param name="exception">The exception, if any.</param>
        /// <param name="severity">The severity level, which determines the dialog title and icon.</param>
        /// <remarks>
        /// The dialog includes the user message, suggestion, and instructions for viewing detailed
        /// error information in the output window. Icon and title are chosen based on severity level.
        /// </remarks>
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

            messageBuilder.AppendLine("Check the Output window for detailed error information:");
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
        /// Wraps an operation with automatic error handling.
        /// </summary>
        /// <param name="context">Description of the operation.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="onError">Optional callback invoked when an error occurs, receiving the exception.</param>
        /// <returns><see langword="true"/> if the operation succeeded; <see langword="false"/> if an error occurred.</returns>
        /// <remarks>
        /// If an exception occurs, it is automatically handled via <see cref="HandleAsync(string, Exception, string, string, ErrorSeverity, bool)"/>,
        /// which logs the error, updates the status bar, and displays a dialog. The onError callback is invoked
        /// after standard error handling completes.
        /// </remarks>
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
        /// Wraps an operation with automatic error handling and returns a result.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="context">Description of the operation.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="defaultvalue">Default value to return if an error occurs. Defaults to <see langword="default"/>(<typeparamref name="T"/>).</param>
        /// <param name="onError">Optional callback invoked when an error occurs, receiving the exception.</param>
        /// <returns>The operation result, or <paramref name="defaultvalue"/> if an error occurred.</returns>
        /// <remarks>
        /// If an exception occurs, it is automatically handled via <see cref="HandleAsync(string, Exception, string, string, ErrorSeverity, bool)"/>,
        /// which logs the error, updates the status bar, and displays a dialog. The onError callback is invoked
        /// after standard error handling completes, and the default value is returned.
        /// </remarks>
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

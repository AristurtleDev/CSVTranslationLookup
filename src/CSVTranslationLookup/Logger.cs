// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using CSVTranslationLookup.Common.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CSVTranslationLookup
{
    internal static class Logger
    {
        private static IVsOutputWindowPane s_pane;
        private static IVsStatusbar s_statusBar;
        private static IServiceProvider s_provider;
        private static string s_name;
        private static bool s_initialized;
        private static bool s_paneCreationFailed;
        private static bool s_statusBarCreationFailed;

        public static async Task InitializeAsync(Microsoft.VisualStudio.Shell.Package provider, string name)
        {
            s_provider = provider;
            s_name = name;
            s_initialized = true;
            s_paneCreationFailed = false;
            s_statusBarCreationFailed = false;

            await LogAsync("Logger Initialized");
        }

        public static async Task LogAsync(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (EnsurePaneLocked())
                {
                    string timestamp = DateTime.Now.ToString("MM/dd/yyyy h:mm:ss tt");
                    string formattedMessage = $"{timestamp}: {message}{Environment.NewLine}";
                    s_pane.OutputString(formattedMessage);
                }
            }
            catch (Exception ex)
            {
                // Last resort: Write to debug output if output pane fails
                Debug.WriteLine($"Logger failed to write message: {ex.Message}");
                Debug.WriteLine($"Original message: {message}");
            }
        }

        public static async Task LogBatchAsync(params string[] messages)
        {
            if (messages == null || messages.Length == 0)
            {
                return;
            }

            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (EnsurePaneLocked())
                {
                    StringBuilder batch = StringBuilderCache.Get();
                    string timestamp = DateTime.Now.ToString("MM/dd/yyyy h:mm:ss tt");

                    foreach (string message in messages)
                    {
                        if (!string.IsNullOrEmpty(message))
                        {
                            batch.Append(timestamp).Append(": ").AppendLine(message);
                        }
                    }

                    if (batch.Length > 0)
                    {
                        s_pane.OutputString(batch.GetStringAndRecycle());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Logger failed to write batch: {ex.Message}");
            }
        }

        public static async Task LogAsync(Exception ex)
        {
            if (ex is null)
            {
                return;
            }

            StringBuilder sb = StringBuilderCache.Get();
            sb.Append("Exception: ").AppendLine(ex.GetType().FullName);
            sb.Append("Message: ").AppendLine(ex.Message);

            if (ex.InnerException != null)
            {
                sb.Append("Inner Exception: ").AppendLine(ex.InnerException.GetType().FullName);
                sb.Append("Inner Message: ").AppendLine(ex.InnerException.Message);
            }

            sb.AppendLine("Stack Trace:");
            sb.AppendLine(ex.StackTrace ?? "(no stack trace available)");

            await LogAsync(sb.GetStringAndRecycle());
        }

        public static async Task LogAsync(string message, Exception ex)
        {
            if (ex is null)
            {
                await LogAsync(message);
                return;
            }

            StringBuilder sb = StringBuilderCache.Get();
            sb.AppendLine(message);
            sb.Append("Exception: ").AppendLine(ex.GetType().FullName);
            sb.Append("Message: ").AppendLine(ex.Message);

            if (ex.InnerException != null)
            {
                sb.Append("Inner Exception: ").AppendLine(ex.InnerException.GetType().FullName);
                sb.Append("Inner Message: ").AppendLine(ex.InnerException.Message);
            }

            sb.AppendLine("Stack Trace:");
            sb.AppendLine(ex.StackTrace ?? "(no stack trace available)");

            await LogAsync(sb.GetStringAndRecycle());
        }

        public static async Task LogProgressAsync(bool inProgress, string label = "", int completed = 0, int total = 0)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (!EnsureStatusBarLocked())
                {
                    return;
                }

                if (!inProgress)
                {
                    // Clear progress indicator
                    uint cookie = 0;
                    s_statusBar.Progress(ref cookie, 0, "", 0, 0);
                }
                else
                {
                    // Show progress with label
                    uint cookie = 0;
                    s_statusBar.Progress(ref cookie, 1, label, (uint)completed, (uint)total);

                    if (!string.IsNullOrEmpty(label))
                    {
                        await LogAsync(label);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LogProgress failed: {ex.Message}");
            }
        }

        public static async Task ClearAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (EnsurePaneLocked())
                {
                    s_pane.Clear();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Logger.Clear failed: {ex.Message}");
            }
        }

        public static async Task ActivateAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (EnsurePaneLocked())
                {
                    s_pane.Activate();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Logger.Activate failed: {ex.Message}");
            }
        }

        private static bool EnsurePaneLocked()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!s_initialized)
            {
                return false;
            }

            // If we already failed to create pane, don't keep trying
            if (s_paneCreationFailed)
            {
                return false;
            }

            // Panel already exists
            if (s_pane != null)
            {
                return true;
            }

            // Try to create pane
            // Note: Caller must already be on UI thread
            try
            {
                Guid guid = Guid.NewGuid();
                IVsOutputWindow output = (IVsOutputWindow)s_provider.GetService(typeof(SVsOutputWindow));

                if (output == null)
                {
                    s_paneCreationFailed = true;
                    Debug.WriteLine("Logger: Failed to get IVsOutputWindow service");
                    return false;
                }

                int hr1 = output.CreatePane(ref guid, s_name, 1, 1);
                if (hr1 != 0)
                {
                    s_paneCreationFailed = true;
                    Debug.WriteLine($"Logger: CreatePane failed with HRESULT: 0x{hr1:X8}");
                    return false;
                }

                int hr2 = output.GetPane(ref guid, out s_pane);
                if (hr2 != 0)
                {
                    s_paneCreationFailed = true;
                    Debug.WriteLine($"Logger: GetPane failed with HRESULT: 0x{hr2:X8}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                s_paneCreationFailed = true;
                Debug.WriteLine($"Logger: Exception creating output pane: {ex.Message}");
                return false;
            }
        }

        private static bool EnsureStatusBarLocked()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!s_initialized)
            {
                return false;
            }

            // If we already failed to create status bar, don't keep trying
            if (s_statusBarCreationFailed)
            {
                return false;
            }

            // Status bar already exists
            if (s_statusBar != null)
            {
                return true;
            }

            // Try to get status bar service
            // Note: Caller must already be on UI thread
            try
            {
                s_statusBar = (IVsStatusbar)s_provider.GetService(typeof(SVsStatusbar));

                if (s_statusBar == null)
                {
                    s_statusBarCreationFailed = true;
                    Debug.WriteLine("Logger: Failed to get IVsStatusbar service");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                s_statusBarCreationFailed = true;
                Debug.WriteLine($"Logger: Exception getting status bar: {ex.Message}");
                return false;
            }
        }
    }
}

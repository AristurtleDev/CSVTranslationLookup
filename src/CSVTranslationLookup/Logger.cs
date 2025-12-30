// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Windows.Markup;
using CSVTranslationLookup.Common.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CSVTranslationLookup
{
    internal static class Logger
    {
        private static readonly object s_lock = new object();
        private static IVsOutputWindowPane s_pane;
        private static IServiceProvider s_provider;
        private static string s_name;
        private static bool s_initialzed;
        private static bool s_paneCreationFailed;

        public static void Initialize(Microsoft.VisualStudio.Shell.Package provider, string name)
        {
            lock(s_lock)
            {
                s_provider = provider;
                s_name = name;
                s_initialzed = true;
                s_paneCreationFailed = false;
            }

            Log("Logger Initialized");
        }

        public static void Log(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                lock (s_lock)
                {
                    if (EnsurePaneLocked())
                    {
                        string timestamp = DateTime.Now.ToString("MM/dd/yyyyy h:mm:sstt");
                        string formattedMessage = $"{timestamp}: {message}{Environment.NewLine}";
                        s_pane.OutputString(formattedMessage);
                    }
                }
            }
            catch(Exception ex)
            {
                // Last resor: Write to debug output if output pane fails
                Debug.WriteLine($"Logger failed to write message: {ex.Message}");
                Debug.WriteLine($"Original message: {message}");
            }
        }

        public static void LogBatch(params string[] messages)
        {
            if(messages == null || messages.Length == 0)
            {
                return;
            }

            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                lock(s_lock)
                {
                    if(EnsurePaneLocked())
                    {
                        StringBuilder batch = StringBuilderCache.Get();
                        string timestamp = DateTime.Now.ToString("MM/dd/yyyy h:mm:sstt");

                        foreach(string message in messages)
                        {
                            if(!string.IsNullOrEmpty(message))
                            {
                                batch.Append(timestamp).Append(": ").AppendLine(message);
                            }
                        }

                        if(batch.Length > 0)
                        {
                            s_pane.OutputString(batch.GetStringAndRecycle());
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Logger failed to write batch: {ex.Message}");
            }
        }

        public static void Log(Exception ex)
        {
            if (ex is null)
            {
                return;
            }

            StringBuilder sb = StringBuilderCache.Get();
            sb.Append("Exception: ").AppendLine(ex.GetType().FullName);
            sb.Append("Message: ").AppendLine(ex.Message);

            if(ex.InnerException != null)
            {
                sb.Append("Inner Exception: ").AppendLine(ex.InnerException.GetType().FullName);
                sb.Append("Inner Message: ").AppendLine(ex.InnerException.Message);
            }

            sb.AppendLine("Stack Trace:");
            sb.AppendLine(ex.StackTrace ?? "(no stack trace avaialble");

            Log(sb.GetStringAndRecycle());
        }

        public static void Log(string message, Exception ex)
        {
            if(ex is null)
            {
                Log(message);
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
            sb.AppendLine(ex.StackTrace ?? "(no stack trace avaialble");

            Log(sb.GetStringAndRecycle());
        }

        public static void LogProgress(bool inProgress, string label = "", int completed = 0, int total = 0)
        {
            if (CSVTranslationLookupPackage.DTE is null)
            {
                return;
            }

            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (!inProgress)
                {
                    CSVTranslationLookupPackage.DTE.StatusBar.Progress(false);
                }
                else
                {
                    CSVTranslationLookupPackage.DTE.StatusBar.Progress(inProgress, label, completed, total);
                    if (!string.IsNullOrEmpty(label))
                    {
                        Log(label);
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"LogProgress failed; {ex.Message}");
            }
        }

        public static void Clear()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                lock(s_lock)
                {
                    if(EnsurePaneLocked())
                    {
                        s_pane.Clear();
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Logger.Clear failed: {ex.Message}");
            }
        }

        public static void Activate()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                lock(s_lock)
                {
                    if(EnsurePaneLocked())
                    {
                        s_pane.Activate();
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Logger.Activate failed: {ex.Message}");
            }
        }

        private static bool EnsurePaneLocked()
        {
            if(!s_initialzed)
            {
                return false;
            }

            // If we already failed to create pane, don't keep trying
            if (s_paneCreationFailed)
            {
                return false;
            }

            // Panel already exists
            if(s_pane != null)
            {
                return true;
            }

            // Try to create pane
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                Guid guid = Guid.NewGuid();
                IVsOutputWindow output = (IVsOutputWindow)s_provider.GetService(typeof(SVsOutputWindow));

                if(output == null)
                {
                    s_paneCreationFailed = true;
                    Debug.WriteLine("Logger: Failed to get IVsOutputWindow service");
                    return false;
                }

                int hr1 = output.CreatePane(ref guid, s_name, 1, 1);
                if(hr1 != 0)
                {
                    s_paneCreationFailed = true;
                    Debug.WriteLine($"Logger: CraePane failed with HRESULT: ox{hr1:X8}");
                    return false;
                }

                int hr2 = output.GetPane(ref guid, out s_pane);
                if(hr2 != 0)
                {
                    s_paneCreationFailed = true;
                    Debug.WriteLine($"Logger: GetPane failed with HRESULT: 0x{hr2:X8}");
                    return false;
                }

                return true;
            }
            catch(Exception ex)
            {
                s_paneCreationFailed = true;
                Debug.WriteLine($"Logger: Exception creating output pane: {ex.Message}");
                return false;
            }
        }
    }
}

// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace CSVTranslationLookup
{
    internal static class Logger
    {
        private static IVsOutputWindowPane s_pane;
        private static IServiceProvider s_provider;
        private static string s_name;

        public static void Initialize(Microsoft.VisualStudio.Shell.Package provider, string name)
        {
            s_provider = provider;
            s_name = name;
        }

        public static void Log(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            try
            {
                if (EnsurePane())
                {
                    s_pane.OutputString($"{DateTime.Now}:{message}{Environment.NewLine}");
                }
            }
            catch { /* do nothing */ }
        }

        public static void Log(Exception ex)
        {
            if (ex is null)
            {
                return;
            }

            Log(ex.ToString());
        }

        private static bool EnsurePane()
        {
            if (s_pane is null)
            {
                Guid guid = Guid.NewGuid();
                IVsOutputWindow output = (IVsOutputWindow)s_provider.GetService(typeof(SVsOutputWindow));
                output.CreatePane(ref guid, s_name, 1, 1);
                output.GetPane(ref guid, out s_pane);
            }

            return s_pane is not null;
        }
    }
}

// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace CSVTranslationLookup.Helpers
{
    internal static class ProjectHelpers
    {
        public static string GetConfigFile(this Project project)
        {
            string dir = project.GetRootFoler();
            if(string.IsNullOrEmpty(dir))
            {
                return null;
            }

            return Path.Combine(dir, Constants.CONFIGURATION_FILENAME);
        }

        public static string GetRootFoler(this Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if(project is null || string.IsNullOrEmpty(project.FullName))
            {
                return null;
            }

            string fullPath;

            try
            {
                fullPath = project.Properties.Item("FullPath").Value as string;
            }
            catch(ArgumentException)
            {
                fullPath = project.Properties.Item("ProjectPath").Value as string;
            }

            if(string.IsNullOrEmpty(fullPath))
            {
                return File.Exists(project.FullName) ? Path.GetDirectoryName(project.FullName) : null;
            }

            if(Directory.Exists(fullPath))
            {
                return fullPath;
            }

            if(File.Exists(fullPath))
            {
                return Path.GetDirectoryName(fullPath);
            }

            return null;
        }
    }
}

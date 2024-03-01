// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CSVTranslationLookup.Configuration;
using EnvDTE;
using EnvDTE80;

namespace CSVTranslationLookup.Helpers
{
    internal static class SolutionHelpers
    {
        public static bool TryGetExistingConfigFile(out string configFile)
        {
            Logger.Log($"Searching for '{Config.ConfigurationFilename}' configuration file in solution");

            configFile = null;

            if (CSVTranslationLookupPackage.DTE is DTE2 dte)
            {
                foreach (Project project in dte.Solution.Projects)
                {
                    if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                    {
                        configFile = SearchConfigFile(project);

                        if (!string.IsNullOrEmpty(configFile))
                        {
                            Logger.Log($"Found configuration file: {configFile}");
                            return true;
                        }
                    }
                }
            }

            Logger.Log($"Unable to locate {Config.ConfigurationFilename} in solution.  Please create one manually");
            return false;
        }

        private static string SearchConfigFile(Project solutionFolder)
        {
            foreach (ProjectItem item in solutionFolder.ProjectItems)
            {
                if (item.Name.Equals(Config.ConfigurationFilename))
                {
                    return item.FileNames[1];
                }

                if (item.SubProject is Project subProj && subProj.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    string configFile = SearchConfigFile(subProj);
                    if (!string.IsNullOrEmpty(configFile))
                    {
                        return configFile;
                    }
                }
            }

            return null;
        }
    }
}

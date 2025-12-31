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
        /// <summary>
        /// Searches the solution for an existing configuration file.
        /// </summary>
        /// <param name="configFile">
        /// When this method returns, contains the full path to the configuration file if found;
        /// otherwise, <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/>  if the configuration file was found; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Only searches within solution folders.  Project that are not solution folders are skipped.
        /// </remarks>
        public static bool TryGetExistingConfigFile(out string configFile)
        {
            Logger.Log($"Searching for '{Config.ConfigurationFilename}' configuration file in solution");

            configFile = null;

            if (CSVTranslationLookupPackage.DTE is DTE2 dte)
            {
                foreach (Project project in dte.Solution.Projects)
                {
                    // Only search within solution folders, not regular projects.
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

        /// <summary>
        /// Recursively searches a solution folder and its  nested folders for the configuration file.
        /// </summary>
        /// <param name="solutionFolder">The solution folder to search.</param>
        /// <returns>
        /// The full path to the configuration file if found; otherwise, <see langword="null"/>.
        /// </returns>
        private static string SearchConfigFile(Project solutionFolder)
        {
            foreach (ProjectItem item in solutionFolder.ProjectItems)
            {
                if (item.Name.Equals(Config.ConfigurationFilename))
                {
                    // FileNames[1] returns the full path of the project item.
                    return item.FileNames[1];
                }

                // Recursively search nested solution folders.
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

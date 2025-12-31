// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using CSVTranslationLookup.Configuration;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CSVTranslationLookup.Helpers
{
    /// <summary>
    /// Provides helper methods for working with Visual Studio solutions.
    /// </summary>
    internal static class SolutionHelpers
    {
        /// <summary>
        /// Searches the solution for an existing configuration file.
        /// </summary>
        /// <returns>
        /// A tuple containing a boolean indicating whether the file was found and the file path.
        /// If found, returns (true, filePath); otherwise returns (false, null).
        /// </returns>
        /// <remarks>
        /// Searches all projects and solution items in the currently loaded solution.
        /// This method must be called on the UI thread.
        /// </remarks>
        public static async Task<(bool found, string configFile)> TryGetExistingConfigFileAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            await Logger.LogAsync($"Searching for '{Config.ConfigurationFilename}' configuration file in solution");

            string configFile = null;

            try
            {
                // Get the solution service
                IVsSolution solution = await GetSolutionServiceAsync();
                if (solution == null)
                {
                    await Logger.LogAsync("Unable to get IVsSolution service");
                    return (false, null);
                }

                // Check if a solution is loaded
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(
                    solution.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object isOpen));

                if (!(isOpen is bool isSolutionOpen) || !isSolutionOpen)
                {
                    await Logger.LogAsync("No solution is currently open");
                    return (false, null);
                }

                // Enumerate all projects in the solution
                Guid guid = Guid.Empty;
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(
                    solution.GetProjectEnum(
                        (uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION,
                        ref guid,
                        out IEnumHierarchies hierarchyEnumerator));

                if (hierarchyEnumerator == null)
                {
                    await Logger.LogAsync("Failed to enumerate projects");
                    return (false, null);
                }

                // Iterate through all hierarchies
                IVsHierarchy[] hierarchies = new IVsHierarchy[1];
                while (true)
                {
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(
                        hierarchyEnumerator.Next(1, hierarchies, out uint fetched));

                    if (fetched == 0)
                    {
                        break;
                    }

                    IVsHierarchy hierarchy = hierarchies[0];

                    // Search this hierarchy for the config file
                    configFile = SearchHierarchyForConfigFile(hierarchy);
                    if (!string.IsNullOrEmpty(configFile))
                    {
                        await Logger.LogAsync($"Found configuration file: {configFile}");
                        return (true, configFile);
                    }
                }

                await Logger.LogAsync($"Unable to locate {Config.ConfigurationFilename} in solution. Please create one manually");
                return (false, null);
            }
            catch (Exception ex)
            {
                await Logger.LogAsync($"Error searching for configuration file: {ex.Message}", ex);
                return (false, null);
            }
        }

        /// <summary>
        /// Gets the Visual Studio solution service.
        /// </summary>
        /// <returns>The <see cref="IVsSolution"/> service instance.</returns>
        private static async Task<IVsSolution> GetSolutionServiceAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            return await CSVTranslationLookupPackage.Package
                ?.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
        }

        /// <summary>
        /// Recursively searches a hierarchy for the configuration file.
        /// </summary>
        /// <param name="hierarchy">The hierarchy to search.</param>
        /// <returns>
        /// The full path to the configuration file if found; otherwise, <see langword="null"/>.
        /// </returns>
        private static string SearchHierarchyForConfigFile(IVsHierarchy hierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (hierarchy == null)
            {
                return null;
            }

            // Start with the root node
            return SearchHierarchyNode(hierarchy, VSConstants.VSITEMID_ROOT);
        }

        /// <summary>
        /// Recursively searches a hierarchy node and its children for the configuration file.
        /// </summary>
        /// <param name="hierarchy">The hierarchy containing the node.</param>
        /// <param name="itemId">The item ID of the node to search.</param>
        /// <returns>
        /// The full path to the configuration file if found; otherwise, <see langword="null"/>.
        /// </returns>
        private static string SearchHierarchyNode(IVsHierarchy hierarchy, uint itemId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Get the canonical name (file path) for this item
            int hr = hierarchy.GetCanonicalName(itemId, out string itemPath);
            if (Microsoft.VisualStudio.ErrorHandler.Succeeded(hr) && !string.IsNullOrEmpty(itemPath))
            {
                string fileName = Path.GetFileName(itemPath);
                if (fileName != null && fileName.Equals(
                    Config.ConfigurationFilename,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return itemPath;
                }
            }

            // Get the first child
            hr = hierarchy.GetProperty(
                itemId,
                (int)__VSHPROPID.VSHPROPID_FirstChild,
                out object firstChildObj);

            if (Microsoft.VisualStudio.ErrorHandler.Failed(hr) || !(firstChildObj is int))
            {
                return null;
            }

            uint firstChild = (uint)(int)firstChildObj;

            // Recursively search children
            uint currentChild = firstChild;
            while (currentChild != VSConstants.VSITEMID_NIL)
            {
                // Search this child node
                string configFile = SearchHierarchyNode(hierarchy, currentChild);
                if (!string.IsNullOrEmpty(configFile))
                {
                    return configFile;
                }

                // Get the next sibling
                hr = hierarchy.GetProperty(
                    currentChild,
                    (int)__VSHPROPID.VSHPROPID_NextSibling,
                    out object nextSiblingObj);

                if (Microsoft.VisualStudio.ErrorHandler.Failed(hr) || !(nextSiblingObj is int))
                {
                    break;
                }

                currentChild = (uint)(int)nextSiblingObj;
            }

            return null;
        }
    }
}

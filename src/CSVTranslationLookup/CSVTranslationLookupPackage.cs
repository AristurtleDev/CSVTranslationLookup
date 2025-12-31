// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using CSVTranslationLookup.Helpers;
using CSVTranslationLookup.Services;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using ShellSolutionEvents = Microsoft.VisualStudio.Shell.Events.SolutionEvents;


namespace CSVTranslationLookup
{
    /// <summary>
    /// Main package class for the CSV Translation Lookup Visual Studio extension.
    /// </summary>
    /// <remarks>
    /// This package initializes the translation lookup service, monitors solution open events,
    /// and provides shared access to Visual Studio services. It automatically loads when a solution
    /// is opened (including when no solution is loaded) and searches for configuration files in
    /// solution folders
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.CSVTranslationLookupString)]
    public sealed class CSVTranslationLookupPackage : ToolkitPackage
    {
        /// <summary>
        /// Cached reference to the Visual Studio DTE automation object.
        /// </summary>
        private static DTE2 s_dte;

        /// <summary>
        /// Cached reference to the Visual Studio status bar service.
        /// </summary>
        private static IVsStatusbar s_vsStatusBar;

        /// <summary>
        /// The translation lookup service that manages CSV file monitoring and token lookups.
        /// </summary>
        private CSVTranslationLookupService _lookupService;

        /// <summary>
        /// Gets the singleton instance of this package.
        /// </summary>
        public static CSVTranslationLookupPackage Package { get; private set; }

        /// <summary>
        /// Gets the Visual Studio DTE automation object.
        /// </summary>
        public static DTE2 DTE
        {
            get
            {
                if (s_dte == null)
                {
                    s_dte = GetGlobalService(typeof(DTE)) as DTE2;
                }

                return s_dte;
            }
        }

        /// <summary>
        /// Gets the CSV translation lookup service.
        /// </summary>
        public CSVTranslationLookupService LookupService
        {
            get
            {
                return _lookupService;
            }
        }

        /// <summary>
        /// Initializes the package asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <param name="progress">Progress reporter for package initialization.</param>
        /// <remarks>
        /// This method creates the lookup service, checks if a solution is already loaded,
        /// and sets up event handlers to detect when solutions are opened. If a solution is
        /// already loaded, it immediately searches for and processes any existing configuration files.
        /// The logger is also initialized during this phase.
        /// </remarks>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Package = this;

            _lookupService = new CSVTranslationLookupService();

            bool isSolutionLoaded = await IsSolutionLoadedAsync();
            if (isSolutionLoaded)
            {
                await HandleOpenSolutionAsync();
            }

            ShellSolutionEvents.OnAfterOpenSolution += (sender, args) => JoinableTaskFactory.RunAsync(() => HandleOpenSolutionAsync(sender, args));
            Logger.Initialize(this, Vsix.Name);
        }

        /// <summary>
        /// Releases resources used by the package.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _lookupService?.Dispose();
                _lookupService = null;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Determines whether a solution is currently loaded in Visual Studio.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if a solution is loaded; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// This method must be called on the UI thread to access the solution service.
        /// </remarks>
        private async Task<bool> IsSolutionLoadedAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            IVsSolution solutionService = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            ErrorHandler.ThrowOnFailure(solutionService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object value));
            return value is bool isSolutionOpen && isSolutionOpen;
        }

        /// <summary>
        /// Handles solution open events by searching for and processing configuration files.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">Solution event arguments.</param>
        /// <remarks>
        /// When a solution is opened, this method searches all solution folders for a configuration file.
        /// If found, it processes the configuration file to initialize CSV file monitoring and token lookups.
        /// </remarks>
        private async Task HandleOpenSolutionAsync(object sender = null, OpenSolutionEventArgs e = null)
        {
            //  Search for an existing configuration file in any projects within the solution.
            //  If one is found, process it to begin with.
            if (SolutionHelpers.TryGetExistingConfigFile(out string configFile))
            {
                await _lookupService?.ProcessConfigAsync(configFile);
            }
        }

        /// <summary>
        /// Updates the Visual Studio status bar with a message.
        /// </summary>
        /// <param name="message">The message to display in the status bar.</param>
        /// <remarks>
        /// This method switches to the UI thread if necessary, lazily initializes the status bar service,
        /// and updates the status text. If the package is not initialized, the method returns silently.
        /// Thread-safe and can be called from any context.
        /// </remarks>
        public static async Task StatusTextAsync(string message)
        {
            if (Package == null)
            {
                return;
            }

            await Package.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (s_vsStatusBar == null)
            {
                s_vsStatusBar = await Package.GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
            }

            if (s_vsStatusBar != null)
            {
                s_vsStatusBar.SetText(message);
            }
        }
    }
}

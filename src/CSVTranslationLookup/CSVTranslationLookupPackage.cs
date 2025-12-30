using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using CSVTranslationLookup.Helpers;
using CSVTranslationLookup.Services;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using static CSVTranslationLookup.Utilities.ErrorHandler;
using ShellSolutionEvents = Microsoft.VisualStudio.Shell.Events.SolutionEvents;


namespace CSVTranslationLookup
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.CSVTranslationLookupString)]
    public sealed class CSVTranslationLookupPackage : ToolkitPackage
    {
        private static DTE2 s_dte;
        private static IVsStatusbar s_vsStatusBar;

        private CSVTranslationLookupService _lookupService;

        public static CSVTranslationLookupPackage Package { get; private set; }

        public static DTE2 DTE
        {
            get
            {
                if(s_dte == null)
                {
                    s_dte = GetGlobalService(typeof(DTE)) as DTE2;
                }

                return s_dte;
            }
        }

        public CSVTranslationLookupService LookupService
        {
            get
            {
                return _lookupService;
            }
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Package = this;

            _lookupService = new CSVTranslationLookupService();

            bool isSolutionLoaded = await IsSolutionLoadedAsync();
            if(isSolutionLoaded)
            {
                await HandleOpenSolutionAsync();
            }

            ShellSolutionEvents.OnAfterOpenSolution += (sender, args) => JoinableTaskFactory.RunAsync(() => HandleOpenSolutionAsync(sender, args));
            Logger.Initialize(this, Vsix.Name);
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                _lookupService?.Dispose();
                _lookupService = null;
            }

            base.Dispose(disposing);
        }

        private async Task<bool> IsSolutionLoadedAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            IVsSolution solutionService = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            ErrorHandler.ThrowOnFailure(solutionService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object value));
            return value is bool isSolutionOpen && isSolutionOpen;
        }

        private async Task HandleOpenSolutionAsync(object sender = null, OpenSolutionEventArgs e = null)
        {
            //  Search for an existing configuration file in any projects within the solution.
            //  If one is found, process it to begin with.
            if (SolutionHelpers.TryGetExistingConfigFile(out string configFile))
            {
                await _lookupService?.ProcessConfigAsync(configFile);
            }
        }

        public static async Task StatusTextAsync(string message)
        {
            if(Package == null)
            {
                return;
            }

            await Package.JoinableTaskFactory.SwitchToMainThreadAsync();

            if(s_vsStatusBar == null)
            {
                s_vsStatusBar = await Package.GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
            }

            if(s_vsStatusBar != null)
            {
                s_vsStatusBar.SetText(message);
            }
        }
    }
}

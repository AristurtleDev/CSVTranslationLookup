using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Community.VisualStudio.Toolkit;
using CSVTranslationLookup.Helpers;
using CSVTranslationLookup.Services;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;

namespace CSVTranslationLookup
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.CSVTranslationLookupString)]
    public sealed class CSVTranslationLookupPackage : ToolkitPackage
    {
        private static Dispatcher s_dispatcher;
        private static DTE2 s_dte;
        public static DTE2 DTE => s_dte ?? (s_dte = GetGlobalService(typeof(DTE)) as DTE2);
        public static Dispatcher Dispatcher => s_dispatcher ?? (Dispatcher.CurrentDispatcher);
        public static Package Package;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            bool isSolutionLoaded = await IsSolutionLoadedAsync();
            if(isSolutionLoaded)
            {
                await HandleOpenSolutionAsync();
            }

            Microsoft.VisualStudio.Shell.Events.SolutionEvents.OnAfterOpenSolution += (sender, args) => JoinableTaskFactory.RunAsync(() => HandleOpenSolutionAsync(sender, args));

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            s_dispatcher = Dispatcher.CurrentDispatcher;
            Package = this;
            Logger.Initialize(this, Vsix.Name);   
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
                CSVTranslationLookupService.ProcessConfig(configFile);
            }
        }

        public static void StatusText(string message)
        {
            Dispatcher?.BeginInvoke(() =>
            {
                s_dte.StatusBar.Text = message;
            }, DispatcherPriority.ApplicationIdle, null);
        }
    }
}

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;

namespace CSVTranslationLookup
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.CSVTranslationLookupString)]
    public sealed class CSVTranslationLookupPackage : ToolkitPackage
    {
        private static Dispatcher s_dispatcher;
        private static DTE2 s_dte;
        public static DTE2 DTE => s_dte ?? (s_dte = GetGlobalService(typeof(DTE)) as DTE2);
        public static Package Package;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            s_dispatcher = Dispatcher.CurrentDispatcher;

            Package = this;

            Logger.Initialize(this, Vsix.Name);
        }

        public static void StatusText(string message)
        {
            s_dispatcher.BeginInvoke(() =>
            {
                s_dte.StatusBar.Text = message;
            }, DispatcherPriority.ApplicationIdle, null);
        }
    }
}

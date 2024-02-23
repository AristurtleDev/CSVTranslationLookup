using Community.VisualStudio.Toolkit;
using CSVTranslationLookup.Configuration;
using CSVTranslationLookup.Services;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace CSVTranslationLookup
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.CSVTranslationLookupString)]
    public sealed class CSVTranslationLookupPackage : ToolkitPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            //---------------------------------------------------------------------------------------------------------
            //  Using this method to check for the solution to load before handling the solution load event. This i
            //  the recommended way of doing this per the solution load events sample linked below.
            //  
            //  https://github.com/madskristensen/VSSDK-Extensibility-Samples/tree/master/SolutionLoadEvents#the-new-pattern
            //---------------------------------------------------------------------------------------------------------
            bool isSolutionLoaded = await IsSolutionLoadedAsync();
            if (isSolutionLoaded)
            {
                await HandleOpenSolutionAsync();
            }

            Microsoft.VisualStudio.Shell.Events.SolutionEvents.OnAfterOpenSolution += (sender, args) => JoinableTaskFactory.RunAsync(() => HandleOpenSolutionAsync(sender, args));

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
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
            object service = await CreateLookupItemServiceAsync();
            ((IServiceContainer)this).AddService(typeof(LookupItemService), service, true);
        }

        private async Task<object> CreateLookupItemServiceAsync()
        {
            LookupItemService service = null;

            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            DTE dte = (DTE)ServiceProvider.GlobalProvider.GetService(typeof(DTE));
            if (dte?.Solution is EnvDTE.Solution solution && !string.IsNullOrEmpty(solution.FullName))
            {
                string solutionDir = Path.GetDirectoryName(dte.Solution.FullName);
                string[] potentialSettingFiles = Directory.GetFiles(solutionDir, CSVTranslationLookupSettings.FileName, SearchOption.AllDirectories);
                string settingsFilePath = string.Empty;
                if (potentialSettingFiles.Length > 0)
                {
                    settingsFilePath = potentialSettingFiles[0];
                }

                CSVTranslationLookupSettings settings;

                if (File.Exists(settingsFilePath))
                {
                    string json = File.ReadAllText(settingsFilePath);
                    settings = JsonConvert.DeserializeObject<CSVTranslationLookupSettings>(json);
                }
                else
                {
                    settings = CSVTranslationLookupSettings.Default;
                    string json = JsonConvert.SerializeObject(settings);
                    File.WriteAllText(settingsFilePath, json);
                }

                settings.SettingsFilePath = settingsFilePath;

                service = new LookupItemService(settings, solutionDir);

            }

            service?.Start();

            return service;
        }
    }
}

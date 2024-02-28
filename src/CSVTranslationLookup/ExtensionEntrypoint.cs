// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.ProjectSystem.Query;

namespace CSVTranslationLookup
{
    /// <summary>
    /// Extension entrypoint for the VisualStudio.Extensibility extension.
    /// </summary>
    [VisualStudioContribution]
    internal class ExtensionEntrypoint : Extension
    {
        /// <inheritdoc/>
        public override ExtensionConfiguration ExtensionConfiguration => new()
        {
            Metadata = new(
                    id: "CSVTranslationLookup.35118709-98e4-4b16-ae74-546e16f67905",
                    version: this.ExtensionAssemblyVersion,
                    publisherName: "Publisher name",
                    displayName: "CSVTranslationLookup",
                    description: "Extension description"),
            LoadedWhen = ActivationConstraint.SolutionState(SolutionState.FullyLoaded)
        };

        protected override async Task OnInitializedAsync(VisualStudioExtensibility extensibility, CancellationToken cancellationToken)
        {
            WorkspacesExtensibility workspace = extensibility.Workspaces();
            var result = await workspace.QuerySolutionAsync((solution) =>
            {
                return solution.Get(x => x.SolutionFolders)
                               .With(folder => folder.Name)
                               .With(folder => folder.VisualPath);
            }, CancellationToken.None);
            await base.OnInitializedAsync(extensibility, cancellationToken);
        }

        /// <inheritdoc />
        protected override async void InitializeServices(IServiceCollection serviceCollection)
        {
            base.InitializeServices(serviceCollection);
            

            // You can configure dependency injection here by adding services to the serviceCollection.
        }
    }
}

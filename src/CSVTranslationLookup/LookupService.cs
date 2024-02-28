// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.ProjectSystem.Query;

namespace CSVTranslationLookup
{
    public class LookupService
    {
        private readonly VisualStudioExtensibility _extensibility;
        private readonly Task _initializationTask;

        public LookupService(VisualStudioExtensibility extensibility)
        {
            _extensibility = extensibility;
            _initializationTask = Task.Run(InitializeAsync);
        }

        private async Task InitializeAsync()
        {
            WorkspacesExtensibility workspace = _extensibility.Workspaces();
            var result = await workspace.QuerySolutionAsync((solution) =>
            {
                return solution.Get(x => x.SolutionFolders)
                               .With(folder => folder.Name)
                               .With(folder => folder.VisualPath);
            }, CancellationToken.None);

        }
    }
}

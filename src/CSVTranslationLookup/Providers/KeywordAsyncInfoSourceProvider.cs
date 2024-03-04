// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;
using CSVTranslationLookup.Sources;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace CSVTranslationLookup.Providers
{
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("Keyword Async Info Source Provider")]
    [ContentType("any")]
    [Order]
    internal class KeywordAsyncInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new KeywordAsyncInfoSource(this, textBuffer));
        }
    }
}

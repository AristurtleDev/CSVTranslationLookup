//////// Copyright (c) Christopher Whitley. All rights reserved.
//////// Licensed under the MIT license.
//////// See LICENSE file in the project root for full license information.

//////using System.ComponentModel.Composition;
//////using CSVTranslationLookup.Sources;
//////using Microsoft.VisualStudio.Language.Intellisense;
//////using Microsoft.VisualStudio.Text;
//////using Microsoft.VisualStudio.Text.Operations;
//////using Microsoft.VisualStudio.Utilities;

//////namespace CSVTranslationLookup.Providers
//////{
//////    [Export(typeof(IQuickInfoSourceProvider))]
//////    [Name("CVS Translation Lookup Keyword Info Source Provider")]
//////    [Order(Before = "Default Quick Info Presenter")]
//////    [ContentType("text")]
//////    internal class KeywordInfoSourceProvider : IQuickInfoSourceProvider
//////    {
//////        [Import]
//////        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

//////        [Import]
//////        internal ITextBufferFactoryService TextBufferFactoryService { get; set; }

//////        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
//////        {
//////            return new KeywordInfoSource(this, textBuffer);
//////        }
//////    }
//////}

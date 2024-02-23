// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using CSVTranslationLookup.Controllers;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace CSVTranslationLookup.Providers
{
    [Export(typeof(IIntellisenseControllerProvider))]
    [Name("CVS Translation Lookup Keyword Info Source Controller")]
    [ContentType("text")]
    internal class KeywordInfoControllerProvider : IIntellisenseControllerProvider
    {
        [Import]
        internal IQuickInfoBroker QuickInfoBroker { get; set; }

        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            return new KeywordInfoController(textView, subjectBuffers, this);
        }
    }
}

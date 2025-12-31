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
    /// <summary>
    /// Provides asynchronous Quick Info sources for keyword translation lookups.
    /// </summary>
    /// <remarks>
    /// This provider is registered for all content types and creates Quick Info sources
    /// that display CSV translation data when hovering over keywords.
    /// </remarks>
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("Keyword Async Info Source Provider")]
    [ContentType("any")]
    [Order]
    internal class KeywordAsyncInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        /// <summary>
        /// Gets or Sets the text structure navigator service used for word navigation and selection.
        /// </summary>
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        /// <summary>
        /// Creates or retrieves an existing Quick Info source for the specified text buffer.
        /// </summary>
        /// <param name="textBuffer">The text buffer for which to create the Quick Info source.</param>
        /// <returns>
        /// An <see cref="IAsyncQuickInfoSource"/> instance associated with the text buffer.
        /// </returns>
        /// <remarks>
        /// Returns a singleton instance per text buffer to ensure consistent state across Quick Info sessions.
        /// </remarks>
        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new KeywordAsyncInfoSource(this, textBuffer));
        }
    }
}

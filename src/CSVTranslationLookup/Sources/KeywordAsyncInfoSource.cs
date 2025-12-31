// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CSVTranslationLookup.Common.Tokens;
using CSVTranslationLookup.Common.Utilities;
using CSVTranslationLookup.Providers;
using CSVTranslationLookup.Services;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Operations;

namespace CSVTranslationLookup.Sources
{
    /// <summary>
    /// Provides asynchronous Quick info tooltips for translation keyword lookups
    /// </summary>
    /// <remarks>
    /// When a user hovers over a translation key in the editor, this source queries the
    /// <see cref="CSVTranslationLookupService"/> to retrieve the associated translation value
    /// and displays it in a Quick info tooltip with options to open the source CSV file.
    /// </remarks>
    internal sealed class KeywordAsyncInfoSource : IAsyncQuickInfoSource
    {
        /// <summary>
        /// Icon displayed next to the translation key in the Quick info tooltip.
        /// </summary>
        private static readonly ImageId _keyIcon = KnownMonikers.Key.ToImageId();

        /// <summary>
        /// Icon displayed next to the translation value in the Quick Info tooltip.
        /// </summary>
        private static readonly ImageId _messageBubbleIcon = KnownMonikers.MessageBubble.ToImageId();

        /// <summary>
        /// Icon displayed next to the "OpenContaining CSV" link int he Quick Info tooltip.
        /// </summary>
        private static readonly ImageId _tableIcon = KnownMonikers.Table.ToImageId();

        /// <summary>
        /// The provider that created this source, used to access shared services.
        /// </summary>
        private readonly KeywordAsyncInfoSourceProvider _provider;

        /// <summary>
        /// The text buffer for which this source provides Quick Info.
        /// </summary>
        private readonly ITextBuffer _textBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeywordAsyncInfoSource"/> class.
        /// </summary>
        /// <param name="provider">The provider that created this source.</param>
        /// <param name="textBuffer">The text buffer for which to provide Quick Info.</param>
        public KeywordAsyncInfoSource(KeywordAsyncInfoSourceProvider provider, ITextBuffer textBuffer)
        {
            _textBuffer = textBuffer;
            _provider = provider;
        }

        /// <summary>
        /// Retrieves Quick Info content for the word at the current trigger point.
        /// </summary>
        /// <param name="session">The Quick Info session containing trigger point information.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation request.</param>
        /// <returns>
        /// A <see cref="QuickInfoItem"/> containing the translation key, value, and CSV file link;
        /// or <see langword="null"/> if no translation is found for the word at the rigger point.
        /// </returns>
        /// <remarks>
        /// The Quick Info tooltip displays three elements:
        /// <list type="number">
        /// <item>The translation key with a key icon</item>
        /// <item>The translation value with a message bubble icon</item>
        /// <item>A clickable link to open the source CSV file with a table icon</item>
        /// </list>
        /// The link opens the CSV file using the configured application from <see cref="CSVTranslationLookupService.Config"/>,
        /// or the system default if no application is configured. Line number substitution is supported
        /// via the {linenum} placeholder in the configured arguments.
        /// </remarks>
        public Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            SnapshotPoint? triggerPoint = session.GetTriggerPoint(_textBuffer.CurrentSnapshot);


            if (triggerPoint == null)
            {
                return Task.FromResult<QuickInfoItem>(null);
            }

            ITextSnapshotLine line = triggerPoint.Value.GetContainingLine();
            ITextSnapshot snapShot = triggerPoint.Value.Snapshot;
            SnapshotSpan span = new SnapshotSpan(triggerPoint.Value, 0);
            ITextStructureNavigator navigator = _provider.NavigatorService.GetTextStructureNavigator(_textBuffer);
            TextExtent extent = navigator.GetExtentOfWord(triggerPoint.Value);


            // Remove surrounding quotations to support both quoted and unquoted keywords
            // e.g., both "ABILITY_NAME" and ABILITY_NAME should match
            string searchText = extent.Span.GetText().Replace("\"", "");

            CSVTranslationLookupService service = CSVTranslationLookupPackage.Package?.LookupService;
            if (service == null || !service.TryGetToken(searchText, out Token token))
            {
                return Task.FromResult<QuickInfoItem>(null);
            }

            // Create UI element showing the translation key
            ContainerElement keyElement =
                new ContainerElement(
                    ContainerElementStyle.Wrapped,
                    new ImageElement(_keyIcon),
                    ClassifiedTextElement.CreatePlainText(searchText)
                    );

            // Create UI element showing the translation value
            ContainerElement valueElement =
                new ContainerElement(
                    ContainerElementStyle.Wrapped,
                    new ImageElement(_messageBubbleIcon),
                    ClassifiedTextElement.CreatePlainText(token.Content)
                    );

            // Create hyperlink to open the CSV file containing this translation
            ContainerElement linkElement =
                new ContainerElement(
                    ContainerElementStyle.Wrapped,
                    new ImageElement(_tableIcon),
                    ClassifiedTextElement.CreateHyperlink("Open Containing CSV", token.FileName, () =>
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo();

                        // Use custom application if configured, otherwise use system default
                        if (!string.IsNullOrEmpty(CSVTranslationLookupService.Config.OpenWith))
                        {
                            string workingDir = PathHelper.GetWorkingDirectoryForExecutable(CSVTranslationLookupService.Config.OpenWith);
                            if (!string.IsNullOrEmpty(workingDir))
                            {
                                startInfo.WorkingDirectory = workingDir;
                            }

                            startInfo.FileName = CSVTranslationLookupService.Config.OpenWith;
                            startInfo.Arguments = token.FileName;

                            // Apply additional arguments with line number substitution
                            string additionalArguments = CSVTranslationLookupService.Config.Arguments;
                            if (!string.IsNullOrEmpty(additionalArguments))
                            {
                                // Replace {linenum} placeholder with actual line number from token
                                additionalArguments = additionalArguments.Replace("{linenum}", $"{token.LineNumber}");
                                startInfo.Arguments += $" {additionalArguments}";
                            }
                        }
                        else
                        {
                            // No custom application configured, open with system default
                            startInfo.FileName = token.FileName;
                        }

                        Process process = new Process();
                        process.StartInfo = startInfo;
                        process.Start();
                    }));

            // Stack all elements vertically in the Quick Info tooltip
            ContainerElement container = new ContainerElement(ContainerElementStyle.Stacked, keyElement, valueElement, linkElement);

            // Create tracking span so the Quick Info follows the text if edits occur
            ITrackingSpan trackingSpan = snapShot.CreateTrackingSpan(extent.Span.Start, searchText.Length, SpanTrackingMode.EdgeInclusive);
            return Task.FromResult(new QuickInfoItem(trackingSpan, container));
        }

        /// <inheritdoc />
        public void Dispose() { }

    }
}

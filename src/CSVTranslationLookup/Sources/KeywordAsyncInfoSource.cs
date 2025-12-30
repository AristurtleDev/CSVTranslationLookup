// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
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
    internal sealed class KeywordAsyncInfoSource : IAsyncQuickInfoSource
    {
        private static readonly ImageId _keyIcon = KnownMonikers.Key.ToImageId();
        private static readonly ImageId _messageBubbleIcon = KnownMonikers.MessageBubble.ToImageId();
        private static readonly ImageId _tableIcon = KnownMonikers.Table.ToImageId();
        private readonly KeywordAsyncInfoSourceProvider _provider;
        private readonly ITextBuffer _textBuffer;

        public KeywordAsyncInfoSource(KeywordAsyncInfoSourceProvider provider, ITextBuffer textBuffer)
        {
            _textBuffer = textBuffer;
            _provider = provider;
        }


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


            //  Replace any surrouding quotations so this works with and without quoted keywords
            string searchText = extent.Span.GetText().Replace("\"", "");

            CSVTranslationLookupService service = CSVTranslationLookupPackage.Package?.LookupService;
            if(service == null || !service.TryGetToken(searchText, out Token token))
            {
                return Task.FromResult<QuickInfoItem>(null);
            }           

            ContainerElement keyElement =
                new ContainerElement(
                    ContainerElementStyle.Wrapped,
                    new ImageElement(_keyIcon),
                    ClassifiedTextElement.CreatePlainText(searchText)
                    );

            ContainerElement valueElement =
                new ContainerElement(
                    ContainerElementStyle.Wrapped,
                    new ImageElement(_messageBubbleIcon),
                    ClassifiedTextElement.CreatePlainText(token.Content)
                    );

            ContainerElement linkElement =
                new ContainerElement(
                    ContainerElementStyle.Wrapped,
                    new ImageElement(_tableIcon),
                    ClassifiedTextElement.CreateHyperlink("Open Containing CSV", token.FileName, () =>
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        if (!string.IsNullOrEmpty(CSVTranslationLookupService.Config.OpenWith))
                        {
                            string workingDir = PathHelper.GetWorkingDirectoryForExecutable(CSVTranslationLookupService.Config.OpenWith);
                            if(!string.IsNullOrEmpty(workingDir))
                            {
                                startInfo.WorkingDirectory = workingDir;
                            }
                            
                            startInfo.FileName = CSVTranslationLookupService.Config.OpenWith;
                            startInfo.Arguments = token.FileName;

                            string additionalArguments = CSVTranslationLookupService.Config.Arguments;
                            if (!string.IsNullOrEmpty(additionalArguments))
                            {
                                additionalArguments = additionalArguments.Replace("{linenum}", $"{token.LineNumber}");
                                startInfo.Arguments += $" {additionalArguments}";
                            }
                        }
                        else
                        {
                            startInfo.FileName = token.FileName;
                        }

                        Process process = new Process();
                        process.StartInfo = startInfo;
                        process.Start();
                    }));

            ContainerElement container = new ContainerElement(ContainerElementStyle.Stacked, keyElement, valueElement, linkElement);
            ITrackingSpan trackingSpan = snapShot.CreateTrackingSpan(extent.Span.Start, searchText.Length, SpanTrackingMode.EdgeInclusive);
            return Task.FromResult(new QuickInfoItem(trackingSpan, container));
        }

        /// <inheritdoc />
        public void Dispose() { }

    }
}

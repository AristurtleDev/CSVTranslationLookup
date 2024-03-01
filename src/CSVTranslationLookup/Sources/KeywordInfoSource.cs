// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CSVTranslationLookup.Common.Tokens;
using CSVTranslationLookup.CSV;
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
    internal class KeywordInfoSource : IQuickInfoSource
    {
        private static readonly ImageId _keyIcon = KnownMonikers.Key.ToImageId();
        private static readonly ImageId _messageBubbleIcon = KnownMonikers.MessageBubble.ToImageId();
        private static readonly ImageId _tableIcon = KnownMonikers.Table.ToImageId();
        private readonly KeywordInfoSourceProvider _provider;
        private readonly ITextBuffer _subjectBuffer;
        private bool _isDisposed;

        public KeywordInfoSource(KeywordInfoSourceProvider provider, ITextBuffer subjectBuffer)
        {
            _provider = provider;
            _subjectBuffer = subjectBuffer;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            // Map the trigger point down to our buffer.
            SnapshotPoint? subjectTriggerPoint = session.GetTriggerPoint(_subjectBuffer.CurrentSnapshot);

            if (subjectTriggerPoint is not null)
            {
                ITextSnapshot currentSnapshot = subjectTriggerPoint.Value.Snapshot;
                SnapshotSpan querySpan = new SnapshotSpan(subjectTriggerPoint.Value, 0);


                ITextStructureNavigator navigator = _provider.NavigatorService.GetTextStructureNavigator(_subjectBuffer);
                TextExtent extent = navigator.GetExtentOfWord(subjectTriggerPoint.Value);

                //  Replace any surrounding quotations so this works with and without quoted keywords
                string searchText = extent.Span.GetText().Replace("\"", "");

                if(CSVTranslationLookupService.TryGetToken(searchText, out Token token))
                {
                    applicableToSpan = currentSnapshot.CreateTrackingSpan(extent.Span.Start, searchText.Length, SpanTrackingMode.EdgeInclusive);

                    var keyElement =
                        new ContainerElement(
                            ContainerElementStyle.Wrapped,
                            new ImageElement(_keyIcon),
                            ClassifiedTextElement.CreatePlainText(searchText)
                        );

                    var valueElement =
                          new ContainerElement(
                              ContainerElementStyle.Wrapped,
                              new ImageElement(_messageBubbleIcon),
                              ClassifiedTextElement.CreatePlainText(token.Content)
                          );


                    var link =
                        new ContainerElement(
                            ContainerElementStyle.Wrapped,
                            new ImageElement(_tableIcon),
                            ClassifiedTextElement.CreateHyperlink("Open Containing CSV", token.FileName, () =>
                            {
                                ProcessStartInfo startInfo = new ProcessStartInfo();
                                if (!string.IsNullOrEmpty(CSVTranslationLookupService.Config.OpenWith))
                                {
                                    startInfo.WorkingDirectory = Path.GetDirectoryName(CSVTranslationLookupService.Config.OpenWith);
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


                    ContainerElement container = new ContainerElement(ContainerElementStyle.Stacked, keyElement, valueElement, link);
                    qiContent.Add(container);
                }
                else
                {
                    qiContent.Add("");
                }
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                GC.SuppressFinalize(this);
                _isDisposed = true;
            }
        }
    }
}

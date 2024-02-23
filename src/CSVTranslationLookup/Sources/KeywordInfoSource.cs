// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using CSVTranslationLookup.Providers;
using CSVTranslationLookup.Services;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
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

        //-------------------------------------------------------------------------------------------------------------
        //  Somtimes the service isn't avaialbel in the packages global services for a few cycles, which can lead
        //  to it always being null if a word is hovered for the first time and the service has not registerd.  To
        //  prevent that, this method was created.
        //-------------------------------------------------------------------------------------------------------------
        private bool TryGetLookupService(out LookupItemService service)
        {
            service = Package.GetGlobalService(typeof(LookupItemService)) as LookupItemService;
            return service is not null;
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

                if (TryGetLookupService(out LookupItemService lookupService) && lookupService.TryGetItem(searchText, out LookupItem item))
                {
                    applicableToSpan = currentSnapshot.CreateTrackingSpan(extent.Span.Start, searchText.Length, SpanTrackingMode.EdgeInclusive);

                    var keyElement =
                        new ContainerElement(
                            ContainerElementStyle.Wrapped,
                            new ImageElement(_keyIcon),
                            ClassifiedTextElement.CreatePlainText(item.Key)
                        );

                    var valueElement =
                          new ContainerElement(
                              ContainerElementStyle.Wrapped,
                              new ImageElement(_messageBubbleIcon),
                              ClassifiedTextElement.CreatePlainText(item.Value)
                          );


                    var link =
                        new ContainerElement(
                            ContainerElementStyle.Wrapped,
                            new ImageElement(_tableIcon),
                            ClassifiedTextElement.CreateHyperlink("Open Containing CSV", item.FilePath, () =>
                            {
                                string cwd = Path.GetDirectoryName(lookupService.Settings.OpenWith);
                                var process = new System.Diagnostics.Process();
                                process.StartInfo.WorkingDirectory = cwd;
                                process.StartInfo.FileName = lookupService.Settings.OpenWith;
                                process.StartInfo.Arguments = string.Format("{0} {1}", item.FilePath, lookupService.Settings.Arguments);
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

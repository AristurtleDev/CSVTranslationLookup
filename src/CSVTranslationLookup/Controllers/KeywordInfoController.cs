// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using CSVTranslationLookup.Providers;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace CSVTranslationLookup.Controllers
{
    internal class KeywordInfoController : IIntellisenseController
    {
        private readonly IList<ITextBuffer> _subjectBuffers;
        private readonly KeywordInfoControllerProvider _provider;
        private ITextView _textView;
        private IQuickInfoSession _session;

        internal KeywordInfoController(ITextView textView, IList<ITextBuffer> subjectBuffers, KeywordInfoControllerProvider provider)
        {
            _textView = textView;
            _subjectBuffers = subjectBuffers;
            _provider = provider;

            _textView.MouseHover += this.OnTextViewMouseHover;
        }

        private void OnTextViewMouseHover(object sender, MouseHoverEventArgs e)
        {
            //  Find the mouse position
            SnapshotPoint? point =
                _textView.BufferGraph.MapDownToFirstMatch
                (
                    new SnapshotPoint(_textView.TextSnapshot, e.Position),
                    PointTrackingMode.Positive,
                    snapshot => _subjectBuffers.Contains(snapshot.TextBuffer),
                    PositionAffinity.Predecessor
                );

            if (point != null)
            {
                ITrackingPoint triggerPoint = point.Value.Snapshot.CreateTrackingPoint(point.Value.Position,
                PointTrackingMode.Positive);

                if (!_provider.QuickInfoBroker.IsQuickInfoActive(_textView))
                {
                    _session = _provider.QuickInfoBroker.TriggerQuickInfo(_textView, triggerPoint, true);
                }
            }
        }

        public void Detach(ITextView textView)
        {
            if (_textView == textView)
            {
                _textView.MouseHover -= this.OnTextViewMouseHover;
                _textView = null;
            }
        }

        //-------------------------------------------------------------------------------------------------------------
        //  Following methods needed for IIntellisenseController interface but not for what we are doing, so they do
        //  nothing.
        //-------------------------------------------------------------------------------------------------------------
        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer) { }
        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer) { }
    }
}

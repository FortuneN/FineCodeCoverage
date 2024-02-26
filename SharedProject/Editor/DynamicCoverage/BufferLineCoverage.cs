using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class BufferLineCoverage : IBufferLineCoverage, IListener<NewCoverageLinesMessage>
    {
        private readonly TextInfo textInfo;
        private readonly IEventAggregator eventAggregator;
        private readonly ITrackedLinesFactory trackedLinesFactory;
        private readonly IDynamicCoverageStore dynamicCoverageStore;
        private readonly Language language;
        private readonly ITextBuffer2 textBuffer;
        private ITrackedLines trackedLines;
        public BufferLineCoverage(
            IFileLineCoverage fileLineCoverage,
            TextInfo textInfo,
            IEventAggregator eventAggregator,
            ITrackedLinesFactory trackedLinesFactory,
            IDynamicCoverageStore dynamicCoverageStore
        )
        {
            language = SupportedContentTypeLanguages.GetLanguage(textInfo.TextBuffer.ContentType.TypeName);
            this.textBuffer = textInfo.TextBuffer;
            this.textInfo = textInfo;
            this.eventAggregator = eventAggregator;
            this.trackedLinesFactory = trackedLinesFactory;
            this.dynamicCoverageStore = dynamicCoverageStore;
            if (fileLineCoverage != null)
            {
                CreateTrackedLines(fileLineCoverage, true);
            }
            eventAggregator.AddListener(this);
            textBuffer.ChangedOnBackground += TextBuffer_ChangedOnBackground;
            void textViewClosedHandler(object s, EventArgs e)
            {
                if(trackedLines != null)
                {
                    dynamicCoverageStore.SaveSerializedCoverage(textInfo.FilePath, trackedLines.GetAllLines());
                }
                textBuffer.Changed -= TextBuffer_ChangedOnBackground;
                textInfo.TextView.Closed -= textViewClosedHandler;
                eventAggregator.RemoveListener(this);
            }

            textInfo.TextView.Closed += textViewClosedHandler;
        }

        private void CreateTrackedLines(IFileLineCoverage fileLineCoverage,bool initial)
        {
            var currentSnapshot = textBuffer.CurrentSnapshot;
            if (initial)
            {
                var serializedCoverage = dynamicCoverageStore.GetSerializedCoverage(textInfo.FilePath);
                //if(serializedCoverage != null)
                //{
                //    trackedLines = trackedLinesFactory.Create(serializedCoverage, currentSnapshot, language);
                //    return;
                //}
            }
            
            
            var numLines = currentSnapshot.LineCount;
            var lines = fileLineCoverage.GetLines(textInfo.FilePath, 1, numLines + 1).ToList();
            trackedLines = trackedLinesFactory.Create(lines, currentSnapshot, language);
        }

        private void TextBuffer_ChangedOnBackground(object sender, TextContentChangedEventArgs e)
        {
            if (trackedLines != null)
            {
                var changed = trackedLines.Changed(e.After, e.Changes.Select(change => change.NewSpan).ToList());
                if (changed)
                {
                    SendCoverageChangedMessage();
                }
            }
        }

        private void SendCoverageChangedMessage()
        {
            eventAggregator.SendMessage(new CoverageChangedMessage(this, textInfo.FilePath));
        }

        public IEnumerable<IDynamicLine> GetLines(int startLineNumber, int endLineNumber)
        {
            if (trackedLines == null)
            {
                return Enumerable.Empty<IDynamicLine>();
            }
            return trackedLines.GetLines(startLineNumber, endLineNumber);
        }

        public void Handle(NewCoverageLinesMessage message)
        {
            if (message.CoverageLines == null)
            {
                trackedLines = null;
            }
            else
            {
                CreateTrackedLines(message.CoverageLines,false);
            }

            SendCoverageChangedMessage();
        }
    }
}

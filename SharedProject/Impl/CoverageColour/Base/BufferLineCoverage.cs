﻿using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Impl
{
    class BufferLineCoverage : IBufferLineCoverage, IListener<NewCoverageLinesMessage>
    {
        private readonly IEventAggregator eventAggregator;
        private readonly ITrackedLinesFactory trackedLinesFactory;
        private readonly string filePath;
        private readonly Language language;
        private readonly ITextBuffer textBuffer;
        private ITrackedLines trackedLines;
        public BufferLineCoverage(
            IFileLineCoverage fileLineCoverage,
            TextInfo textInfo,
            IEventAggregator eventAggregator,
            ITrackedLinesFactory trackedLinesFactory
        )
        {
            this.filePath = textInfo.FilePath;
            language = SupportedContentTypeLanguages.GetLanguage(textInfo.TextBuffer.ContentType.TypeName);
            this.textBuffer = textInfo.TextBuffer;
            this.eventAggregator = eventAggregator;
            this.trackedLinesFactory = trackedLinesFactory;
            if (fileLineCoverage != null)
            {
                CreateTrackedLines(fileLineCoverage);
            }
            eventAggregator.AddListener(this);

            textBuffer.Changed += TextBuffer_Changed;
            void textViewClosedHandler(object s, EventArgs e)
            {
                textBuffer.Changed -= TextBuffer_Changed;
                textInfo.TextView.Closed -= textViewClosedHandler;
                eventAggregator.RemoveListener(this);
            }

            textInfo.TextView.Closed += textViewClosedHandler;
        }

        private void CreateTrackedLines(IFileLineCoverage fileLineCoverage)
        {
            var currentSnapshot = textBuffer.CurrentSnapshot;
            var numLines = currentSnapshot.LineCount;
            var lines = fileLineCoverage.GetLines(filePath, 1, numLines + 1).ToList();
            trackedLines = trackedLinesFactory.Create(lines, currentSnapshot, language);
        }

        private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
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
            eventAggregator.SendMessage(new CoverageChangedMessage(this, filePath));
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
                CreateTrackedLines(message.CoverageLines);
            }

            SendCoverageChangedMessage();
        }
    }
}
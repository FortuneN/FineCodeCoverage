using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Text.Tagging;
using System.ComponentModel.Composition;
using FineCodeCoverage.Core.Initialization;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace FineCodeCoverage.Impl
{
    internal class CoverageChangedMessage
    {
        public IBufferLineCoverage CoverageLines { get; }
        public string AppliesTo { get; }

        public CoverageChangedMessage(IBufferLineCoverage coverageLines, string appliesTo)
        {
            CoverageLines = coverageLines;
            AppliesTo = appliesTo;
        }
    }
   
    interface IDynamicCoverageManager
    {
        IBufferLineCoverage Manage(ITextBuffer buffer, string filePath);
    }

    class TrackedLineLine : ILine
    {
        public TrackedLineLine(ILine line)
        {
            Number = line.Number;
            CoverageType = line.CoverageType;
        }

        public int Number { get; set; }

        public CoverageType CoverageType { get; }
    }
    class TrackedLine
    {
        public TrackedLineLine Line { get; }
        public ITrackingSpan TrackingSpan { get; }

        public TrackedLine(ILine line, ITrackingSpan trackingSpan)
        {
            Line = new TrackedLineLine(line);
            TrackingSpan = trackingSpan;
        }
    }

    interface IBufferLineCoverage
    {
        IEnumerable<ILine> GetLines(int startLineNumber, int endLineNumber);
    }
    

    class BufferLineCoverage : IBufferLineCoverage, IListener<NewCoverageLinesMessage>
    {
        private readonly IEventAggregator eventAggregator;
        private readonly string filePath;
        private readonly ITextBuffer textBuffer;
        private List<TrackedLine> trackedLines;
        private FileLineCoverage flc = new FileLineCoverage();

        // file line coverage can be null
        public BufferLineCoverage(IFileLineCoverage fileLineCoverage,ITextBuffer textBuffer, string filePath, IEventAggregator eventAggregator)
        {
            this.filePath = filePath;
            this.textBuffer = textBuffer;
            if(fileLineCoverage != null)
            {
                PrepareForChanges(fileLineCoverage);
            }
            eventAggregator.AddListener(this, false);
            this.eventAggregator = eventAggregator;
            textBuffer.Changed += TextBuffer_Changed;
        }

        private void PrepareForChanges(IFileLineCoverage fileLineCoverage)
        {
            var numLines = textBuffer.CurrentSnapshot.LineCount;
            var lines = fileLineCoverage.GetLines(filePath, 1, numLines+1).ToList();
            trackedLines = lines.Select(l =>
            {
                var span = textBuffer.CurrentSnapshot.GetLineFromLineNumber(l.Number - 1).Extent;
                return new TrackedLine(l, textBuffer.CurrentSnapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive));
            }).ToList();
            SetCoverage(lines);
        }

        private void SetCoverage(List<ILine> lines)
        {
            this.flc = new FileLineCoverage();
            flc.Add(filePath, lines);
        }

        private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            var currentSnapshot = textBuffer.CurrentSnapshot;
            if(trackedLines != null)
            {
                var changed = false;
                var removals = new List<TrackedLine>();
                foreach(var trackedLine in trackedLines)
                {
                    var newSnapshotSpan = trackedLine.TrackingSpan.GetSpan(currentSnapshot);
                    if (newSnapshotSpan.IsEmpty)
                    {
                        changed = true;
                        removals.Add(trackedLine);
                    }
                    var newLineNumber = currentSnapshot.GetLineNumberFromPosition(newSnapshotSpan.Start) + 1;
                    if(!changed && newLineNumber != trackedLine.Line.Number)
                    {
                        changed = true;
                    }
                    trackedLine.Line.Number = newLineNumber;
                }
                removals.ForEach(r => trackedLines.Remove(r));
                if (changed)
                {
                    SetCoverage(trackedLines.Select(tl => tl.Line as ILine).ToList());
                    eventAggregator.SendMessage(new CoverageChangedMessage(this, filePath));
                }
            }
        }

        public IEnumerable<ILine> GetLines(int startLineNumber, int endLineNumber)
        {
           return flc.GetLines(filePath, startLineNumber, endLineNumber);
        }

        public void Handle(NewCoverageLinesMessage message)
        {
            if (message.CoverageLines == null)
            {
                flc = new FileLineCoverage();
                trackedLines = null;
            }
            else
            {
                PrepareForChanges(message.CoverageLines);
            }
            eventAggregator.SendMessage(new CoverageChangedMessage(this, filePath));
        }
    }

    [Export(typeof(IInitializable))]
    [Export(typeof(IDynamicCoverageManager))]
    internal class DynamicCoverageManager : IDynamicCoverageManager, IListener<NewCoverageLinesMessage>, IInitializable
    {
        private readonly IEventAggregator eventAggregator;
        private IFileLineCoverage lastCoverageLines;

        [ImportingConstructor]
        public DynamicCoverageManager(IEventAggregator eventAggregator)
        {
            eventAggregator.AddListener(this);
            this.eventAggregator = eventAggregator;
        }
        public void Handle(NewCoverageLinesMessage message)
        {
            lastCoverageLines = message.CoverageLines;
            
        }

        public IBufferLineCoverage Manage(ITextBuffer textBuffer, string filePath)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty<IBufferLineCoverage>(() =>
            {
                return new BufferLineCoverage(lastCoverageLines, textBuffer, filePath, eventAggregator);
            });
        }
    }


    [Export(typeof(ICoverageTaggerProviderFactory))]
    internal class CoverageTaggerProviderFactory : ICoverageTaggerProviderFactory
    {
        private readonly IEventAggregator eventAggregator;
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly ILineSpanLogic lineSpanLogic;
        private readonly IDynamicCoverageManager dynamicCoverageManager;

        [ImportingConstructor]
        public CoverageTaggerProviderFactory(
            IEventAggregator eventAggregator,
            IAppOptionsProvider appOptionsProvider,
            ILineSpanLogic lineSpanLogic,
            IDynamicCoverageManager dynamicCoverageManager
        )
        {
            this.eventAggregator = eventAggregator;
            this.appOptionsProvider = appOptionsProvider;
            this.lineSpanLogic = lineSpanLogic;
            this.dynamicCoverageManager = dynamicCoverageManager;
        }
        public ICoverageTaggerProvider<TTag> Create<TTag, TCoverageTypeFilter>(ILineSpanTagger<TTag> tagger)
            where TTag : ITag
            where TCoverageTypeFilter : ICoverageTypeFilter, new()
        {
            return new CoverageTaggerProvider<TCoverageTypeFilter, TTag>(
                eventAggregator, appOptionsProvider, lineSpanLogic, tagger, dynamicCoverageManager
            );
        }

    }

}

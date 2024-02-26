using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class ContainingCodeTracker : IContainingCodeTracker
    {
        private readonly ITrackingLine trackingLine;
        private readonly ITrackingSpanRange trackingSpanRange;
        private ITrackedCoverageLines trackedCoverageLines;
        private readonly IDirtyLineFactory dirtyLineFactory;
        private IDirtyLine dirtyLine;

        public ContainingCodeTracker(
            ITrackedCoverageLines trackedCoverageLines, 
            IDirtyLineFactory dirtyLineFactory,
            ITrackingSpanRange trackingSpanRange = null)
        {
            this.trackingSpanRange = trackingSpanRange;
            this.trackedCoverageLines = trackedCoverageLines;
            this.dirtyLineFactory = dirtyLineFactory;
        }

        public ContainingCodeTracker(
            ITrackingLine trackingLine,
            ITrackingSpanRange trackingSpanRange
        )
        {
            this.trackingLine = trackingLine;
            this.trackingSpanRange = trackingSpanRange;
        }

        private TrackingSpanRangeProcessResult ProcessTrackingSpanRangeChanges(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanAndLineRanges)
        {
            if (trackingSpanRange == null) return new TrackingSpanRangeProcessResult(newSpanAndLineRanges,false,false);

            return trackingSpanRange.Process(currentSnapshot, newSpanAndLineRanges);
        }

        private bool CreateDirtyLineIfRequired(
            List<SpanAndLineRange> newSpanChanges, 
            List<SpanAndLineRange> nonIntersecting,
            bool textChanged,
            ITextSnapshot currentSnapshot)
        {
            var createdDirtyLine = false;
            if (RequiresDirtyLine() && textChanged && Intersected(newSpanChanges,nonIntersecting))
            {
                CreateDirtyLine(currentSnapshot);
                createdDirtyLine = true;
            }
            return createdDirtyLine;
        }

        private void CreateDirtyLine(ITextSnapshot currentSnapshot)
        {
            var firstTrackingSpan = trackingSpanRange.GetFirstTrackingSpan();
            dirtyLine = dirtyLineFactory.Create(firstTrackingSpan, currentSnapshot);
            trackedCoverageLines = null;
        }

        private bool RequiresDirtyLine()
        {
            return trackingLine == null && dirtyLine == null && trackedCoverageLines.Lines.Any();
        }

        private bool Intersected(
            List<SpanAndLineRange> newSpanChanges,
            List<SpanAndLineRange> nonIntersecting)
        {
            return nonIntersecting.Count < newSpanChanges.Count;
        }

        public IContainingCodeTrackerProcessResult ProcessChanges(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanAndLIneRanges)
        {
            var trackingSpanRangeProcessResult = ProcessTrackingSpanRangeChanges(currentSnapshot, newSpanAndLIneRanges);
            var nonIntersectingSpans = trackingSpanRangeProcessResult.NonIntersectingSpans;
            if (trackingSpanRangeProcessResult.IsEmpty)
            {
                // todo - determine changed parameter
                return new ContainingCodeTrackerProcessResult(true, nonIntersectingSpans,true);
            }

            var createdDirtyLine = CreateDirtyLineIfRequired(newSpanAndLIneRanges, nonIntersectingSpans,trackingSpanRangeProcessResult.TextChanged,currentSnapshot);
            var result = new ContainingCodeTrackerProcessResult(createdDirtyLine, nonIntersectingSpans, false);
            if (!createdDirtyLine)
            {
                var linesChanged = UpdateLines(currentSnapshot);
                result.Changed = result.Changed || linesChanged;
            }
            
            return result;
        }

        private bool UpdateLines(ITextSnapshot currentSnapshot)
        {
            if(trackingLine != null)
            {
                return trackingLine.Update(currentSnapshot);
            }
            else if (dirtyLine != null)
            {
               return dirtyLine.Update(currentSnapshot);
            }
            else
            {
                return trackedCoverageLines.Update(currentSnapshot);
            }
        }

        private IDynamicLine PossibleSingleLine => trackingLine != null ? trackingLine.Line : dirtyLine?.Line;

        public IEnumerable<IDynamicLine> Lines => PossibleSingleLine != null ? new List<IDynamicLine> { PossibleSingleLine } :  trackedCoverageLines.Lines;
    }
    
}

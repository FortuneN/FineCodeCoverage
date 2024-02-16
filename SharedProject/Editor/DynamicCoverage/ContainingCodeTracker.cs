using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class ContainingCodeTracker : IContainingCodeTracker
    {
        private bool isEmpty;
        private readonly ITrackingSpanRange trackingSpanRange;
        private readonly ITrackedCoverageLines trackedCoverageLines;
        private DirtyLine dirtyLine;

        public ContainingCodeTracker(ITrackedCoverageLines trackedCoverageLines, ITrackingSpanRange trackingSpanRange = null)
        {
            this.trackingSpanRange = trackingSpanRange;
            this.trackedCoverageLines = trackedCoverageLines;
        }

        private NonIntersectingResult ProcessTrackingSpanRangeChanges(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanChanges)
        {
            if (trackingSpanRange == null) return new NonIntersectingResult(newSpanChanges,false,false);

            return trackingSpanRange.GetNonIntersecting(currentSnapshot, newSpanChanges);
        }

        private bool ProcessChanged(List<SpanAndLineRange> newSpanChanges, List<SpanAndLineRange> nonIntersecting,bool textChanged,ITextSnapshot currentSnapshot)
        {
            var trackingSpanRangeChanged = nonIntersecting.Count < newSpanChanges.Count;
            var changed = false;
            if (textChanged && trackingSpanRangeChanged && trackedCoverageLines.Lines.Any() & dirtyLine == null)
            {
                var firstTrackingSpan= trackingSpanRange.GetFirstTrackingSpan();
                dirtyLine = new DirtyLine(firstTrackingSpan, currentSnapshot);
                changed = true;
            }
            return changed;
        }

        public IContainingCodeTrackerProcessResult ProcessChanges(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanChanges)
        {
            if(isEmpty)
            {
                return new ContainingCodeTrackerProcessResult(false, newSpanChanges,true);
            }
            var nonIntersectingResult = ProcessTrackingSpanRangeChanges(currentSnapshot, newSpanChanges);
            var nonIntersectingSpans = nonIntersectingResult.NonIntersectingSpans;
            if (nonIntersectingResult.IsEmpty)
            {
                isEmpty = true;
                return new ContainingCodeTrackerProcessResult(true, nonIntersectingSpans,true);
            }
            var changed = ProcessChanged(newSpanChanges, nonIntersectingSpans,nonIntersectingResult.TextChanged,currentSnapshot);
            var result = new ContainingCodeTrackerProcessResult(changed, nonIntersectingSpans, false);
            if (!changed)
            {
                if (dirtyLine != null)
                {
                    bool dirtyLinesChanged = dirtyLine.Update(currentSnapshot);
                    if(dirtyLinesChanged)
                    {
                        result.Changed = true;
                    }
                }
                else
                {
                    // todo - if have a solitary line......
                    var coverageLinesChanged = trackedCoverageLines.Update(currentSnapshot);
                    if (coverageLinesChanged)
                    {
                        result.Changed = true;
                    }
                }
            }
            
            return result;
        }

        public IEnumerable<IDynamicLine> Lines => isEmpty ?  Enumerable.Empty<IDynamicLine>() :  
            dirtyLine != null ? new List<IDynamicLine> { dirtyLine.Line } :  trackedCoverageLines.Lines;
    }
    
}

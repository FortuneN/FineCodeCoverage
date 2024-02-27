using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class CoverageCodeTracker : IUpdatableDynamicLines
    {
        private ITrackedCoverageLines trackedCoverageLines;
        private readonly IDirtyLineFactory dirtyLineFactory;
        private IDirtyLine dirtyLine;

        public CoverageCodeTracker(
            ITrackedCoverageLines trackedCoverageLines, 
            IDirtyLineFactory dirtyLineFactory
        )
        {
            this.trackedCoverageLines = trackedCoverageLines;
            this.dirtyLineFactory = dirtyLineFactory;
        }

        private bool CreateDirtyLineIfRequired(
            List<SpanAndLineRange> newSpanChanges, 
            List<SpanAndLineRange> nonIntersecting,
            bool textChanged,
            ITextSnapshot currentSnapshot,
            ITrackingSpanRange trackingSpanRange)
        {
            var createdDirtyLine = false;
            if (dirtyLine == null && textChanged && Intersected(newSpanChanges,nonIntersecting))
            {
                CreateDirtyLine(currentSnapshot, trackingSpanRange);
                createdDirtyLine = true;
            }
            return createdDirtyLine;
        }

        private void CreateDirtyLine(ITextSnapshot currentSnapshot, ITrackingSpanRange trackingSpanRange)
        {
            var firstTrackingSpan = trackingSpanRange.GetFirstTrackingSpan();
            dirtyLine = dirtyLineFactory.Create(firstTrackingSpan, currentSnapshot);
            trackedCoverageLines = null;
        }

        private bool Intersected(
            List<SpanAndLineRange> newSpanChanges,
            List<SpanAndLineRange> nonIntersecting)
        {
            return nonIntersecting.Count < newSpanChanges.Count;
        }

        public bool Update(TrackingSpanRangeProcessResult trackingSpanRangeProcessResult, ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanAndLIneRanges)
        {
            var createdDirtyLine = CreateDirtyLineIfRequired(
                    newSpanAndLIneRanges,
                    trackingSpanRangeProcessResult.NonIntersectingSpans,
                    trackingSpanRangeProcessResult.TextChanged,
                    currentSnapshot,
                    trackingSpanRangeProcessResult.TrackingSpanRange
                );
            var changed = createdDirtyLine;
            if (!createdDirtyLine)
            {
                changed = UpdateLines(currentSnapshot);
            }
            return changed;

        }

        private bool UpdateLines(ITextSnapshot currentSnapshot)
        {
            if (dirtyLine != null)
            {
               return dirtyLine.Update(currentSnapshot);
            }
            else
            {
                return trackedCoverageLines.Update(currentSnapshot);
            }
        }


        public IEnumerable<IDynamicLine> Lines => dirtyLine != null ? new List<IDynamicLine> { dirtyLine.Line } :  trackedCoverageLines.Lines;
    }
    
}

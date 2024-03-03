using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class CoverageCodeTracker : IUpdatableDynamicLines
    {
        private ITrackedCoverageLines trackedCoverageLines;
        private readonly IDirtyLineFactory dirtyLineFactory;
        private ITrackingLine dirtyLine;

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
            bool createdDirtyLine = false;
            if (this.dirtyLine == null && textChanged && this.Intersected(newSpanChanges, nonIntersecting))
            {
                this.CreateDirtyLine(currentSnapshot, trackingSpanRange);
                createdDirtyLine = true;
            }

            return createdDirtyLine;
        }

        private void CreateDirtyLine(ITextSnapshot currentSnapshot, ITrackingSpanRange trackingSpanRange)
        {
            ITrackingSpan firstTrackingSpan = trackingSpanRange.GetFirstTrackingSpan();
            this.dirtyLine = this.dirtyLineFactory.Create(firstTrackingSpan, currentSnapshot);
            this.trackedCoverageLines = null;
        }

        private bool Intersected(
            List<SpanAndLineRange> newSpanChanges,
            List<SpanAndLineRange> nonIntersecting
        ) => nonIntersecting.Count < newSpanChanges.Count;

        public bool Update(TrackingSpanRangeProcessResult trackingSpanRangeProcessResult, ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanAndLIneRanges)
        {
            bool createdDirtyLine = this.CreateDirtyLineIfRequired(
                    newSpanAndLIneRanges,
                    trackingSpanRangeProcessResult.NonIntersectingSpans,
                    trackingSpanRangeProcessResult.TextChanged,
                    currentSnapshot,
                    trackingSpanRangeProcessResult.TrackingSpanRange
                );
            bool changed = createdDirtyLine;
            if (!createdDirtyLine)
            {
                changed = this.UpdateLines(currentSnapshot);
            }

            return changed;
        }

        private bool UpdateLines(ITextSnapshot currentSnapshot)
            => this.dirtyLine != null ? this.dirtyLine.Update(currentSnapshot) : this.trackedCoverageLines.Update(currentSnapshot);

        public IEnumerable<IDynamicLine> Lines => this.dirtyLine != null ? new List<IDynamicLine> { this.dirtyLine.Line } : this.trackedCoverageLines.Lines;

        public ContainingCodeTrackerType Type => ContainingCodeTrackerType.CoverageLines;
    }
}

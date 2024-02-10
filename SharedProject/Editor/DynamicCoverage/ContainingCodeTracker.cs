using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class ContainingCodeTracker : IContainingCodeTracker
    {
        private bool isDirty;
        private readonly ITrackingSpanRange trackingSpanRange;
        private readonly ITrackedCoverageLines trackedCoverageLines;

        public ContainingCodeTracker(ITrackedCoverageLines trackedCoverageLines, ITrackingSpanRange trackingSpanRange = null)
        {
            this.trackingSpanRange = trackingSpanRange;
            this.trackedCoverageLines = trackedCoverageLines;
        }

        private bool ProcessTrackingSpanRangeChangesIfNotDirty(ITextSnapshot currentSnapshot, List<Span> newSpanChanges)
        {
            if (!isDirty)
            {
                return ProcessTrackingSpanRangeChanges(currentSnapshot, newSpanChanges);
            }
            return false;
        }

        private bool ProcessTrackingSpanRangeChanges(ITextSnapshot currentSnapshot, List<Span> newSpanChanges)
        {
            if (trackingSpanRange == null) return false;

            var trackingSpanRangeChanged = trackingSpanRange.IntersectsWith(currentSnapshot, newSpanChanges);
            if (trackingSpanRangeChanged)
            {
                trackedCoverageLines.Dirty();
                isDirty = true;
            }
            return trackingSpanRangeChanged;
        }

        public bool ProcessChanges(ITextSnapshot currentSnapshot, List<Span> newSpanChanges)
        {
            var trackingSpanRangeChanged = ProcessTrackingSpanRangeChangesIfNotDirty(currentSnapshot, newSpanChanges);
            var coverageLinesChanged = trackedCoverageLines.Update(currentSnapshot);
            return trackingSpanRangeChanged || coverageLinesChanged;
        }

        public IEnumerable<IDynamicLine> Lines => trackedCoverageLines.Lines;
    }
    
}

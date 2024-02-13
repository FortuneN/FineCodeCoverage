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

        private List<Span> ProcessTrackingSpanRangeChanges(ITextSnapshot currentSnapshot, List<Span> newSpanChanges)
        {
            if (trackingSpanRange == null) return newSpanChanges;

            return trackingSpanRange.GetNonIntersecting(currentSnapshot, newSpanChanges);
        }

        private bool ProcessChanged(List<Span> newSpanChanges, List<Span> nonIntersecting)
        {
            var trackingSpanRangeChanged = nonIntersecting.Count < newSpanChanges.Count;
            var changed = false;
            if (trackingSpanRangeChanged & !isDirty)
            {
                Dirty();
                changed = true;
            }
            return changed;
        }

        private void Dirty()
        {
            trackedCoverageLines.Dirty();
            isDirty = true;
        }

        public IContainingCodeTrackerProcessResult ProcessChanges(ITextSnapshot currentSnapshot, List<Span> newSpanChanges)
        {
            var nonIntersecting = ProcessTrackingSpanRangeChanges(currentSnapshot, newSpanChanges);
            var changed = ProcessChanged(newSpanChanges, nonIntersecting);
            var result = new ContainingCodeTrackerProcessResult(changed, nonIntersecting);
            // todo - if have a solitary line......
            var coverageLinesChanged = trackedCoverageLines.Update(currentSnapshot);
            if(coverageLinesChanged)
            {
                result.Changed = true;
            }
            return result;
        }

        public IEnumerable<IDynamicLine> Lines => trackedCoverageLines.Lines;
    }
    
}

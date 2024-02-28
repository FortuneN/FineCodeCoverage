using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class TrackingSpanRangeUpdatingTracker : IContainingCodeTracker
    {
        private readonly ITrackingSpanRange trackingSpanRange;
        private readonly IUpdatableDynamicLines updatableDynamicLines;

        public TrackingSpanRangeUpdatingTracker(
            ITrackingSpanRange trackingSpanRange,
            IUpdatableDynamicLines updatableDynamicLines
        )
        {
            this.trackingSpanRange = trackingSpanRange;
            this.updatableDynamicLines = updatableDynamicLines;
        }

        public IEnumerable<IDynamicLine> Lines => updatableDynamicLines.Lines;

        public ContainingCodeTrackerState GetState()
        {
            return new ContainingCodeTrackerState(updatableDynamicLines.Type, trackingSpanRange.ToCodeSpanRange(), Lines);
        }

        public IContainingCodeTrackerProcessResult ProcessChanges(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanAndLineRanges)
        {
            var trackingSpanRangeProcessResult = trackingSpanRange.Process(currentSnapshot, newSpanAndLineRanges);
            var nonIntersectingSpans = trackingSpanRangeProcessResult.NonIntersectingSpans;
            if (trackingSpanRangeProcessResult.IsEmpty)
            {
                // todo - determine changed parameter
                return new ContainingCodeTrackerProcessResult(true, nonIntersectingSpans, true);
            }
            var changed = updatableDynamicLines.Update(trackingSpanRangeProcessResult, currentSnapshot, newSpanAndLineRanges);
            return new ContainingCodeTrackerProcessResult(changed, nonIntersectingSpans, false);
        }
    }
}

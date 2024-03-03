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

        public IEnumerable<IDynamicLine> Lines => this.updatableDynamicLines.Lines;

        public ContainingCodeTrackerState GetState() 
            => new ContainingCodeTrackerState(this.updatableDynamicLines.Type, this.trackingSpanRange.ToCodeSpanRange(), this.Lines);

        public IContainingCodeTrackerProcessResult ProcessChanges(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanAndLineRanges)
        {
            TrackingSpanRangeProcessResult trackingSpanRangeProcessResult = this.trackingSpanRange.Process(currentSnapshot, newSpanAndLineRanges);
            List<SpanAndLineRange> nonIntersectingSpans = trackingSpanRangeProcessResult.NonIntersectingSpans;
            if (trackingSpanRangeProcessResult.IsEmpty)
            {
                // todo - determine changed parameter
                return new ContainingCodeTrackerProcessResult(true, nonIntersectingSpans, true);
            }

            bool changed = this.updatableDynamicLines.Update(trackingSpanRangeProcessResult, currentSnapshot, newSpanAndLineRanges);
            return new ContainingCodeTrackerProcessResult(changed, nonIntersectingSpans, false);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;

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
                IEnumerable<int> lines = this.updatableDynamicLines.Lines.Select(l => l.Number);
                return new ContainingCodeTrackerProcessResult(lines, nonIntersectingSpans, true);
            }

            IEnumerable<int> changedLines = this.updatableDynamicLines.GetUpdatedLineNumbers(trackingSpanRangeProcessResult, currentSnapshot, newSpanAndLineRanges);
            return new ContainingCodeTrackerProcessResult(changedLines, nonIntersectingSpans, false);
        }
    }
}

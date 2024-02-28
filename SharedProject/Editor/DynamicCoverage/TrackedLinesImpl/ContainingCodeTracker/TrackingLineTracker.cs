using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal abstract class TrackingLineTracker : IUpdatableDynamicLines
    {
        private readonly ITrackingLine trackingLine;

        public TrackingLineTracker(
            ITrackingLine trackingLine, ContainingCodeTrackerType containingCodeTrackerType
            )
        {
            this.trackingLine = trackingLine;
            Type = containingCodeTrackerType;
        }
        public IEnumerable<IDynamicLine> Lines => new List<IDynamicLine> { trackingLine.Line };

        public ContainingCodeTrackerType Type { get; }

        public bool Update(TrackingSpanRangeProcessResult trackingSpanRangeProcessResult, ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanAndLineRanges)
        {
            return trackingLine.Update(currentSnapshot);
        }
    }
}

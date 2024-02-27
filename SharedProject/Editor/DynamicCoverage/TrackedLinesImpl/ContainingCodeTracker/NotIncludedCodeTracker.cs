using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class NotIncludedCodeTracker : IUpdatableDynamicLines
    {
        private readonly ITrackingLine notIncludedTrackingLine;

        public NotIncludedCodeTracker(
            ITrackingLine notIncludedTrackingLine
            )
        {
            this.notIncludedTrackingLine = notIncludedTrackingLine;
        }
        public IEnumerable<IDynamicLine> Lines => new List<IDynamicLine> { notIncludedTrackingLine.Line };

        public bool Update(TrackingSpanRangeProcessResult trackingSpanRangeProcessResult, ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanAndLineRanges)
        {
            return notIncludedTrackingLine.Update(currentSnapshot);
        }
    }
}

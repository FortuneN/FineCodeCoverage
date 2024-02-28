using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class OtherLinesTracker : IUpdatableDynamicLines
    {

        public OtherLinesTracker(
        )
        {
        }

        public IEnumerable<IDynamicLine> Lines { get; } = Enumerable.Empty<IDynamicLine>();

        public ContainingCodeTrackerType Type => ContainingCodeTrackerType.OtherLines;

        public bool Update(TrackingSpanRangeProcessResult trackingSpanRangeProcessResult, ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanAndLineRanges)
        {
            return false;
        }
    }
}

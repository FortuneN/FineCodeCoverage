using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface IUpdatableDynamicLines
    {
        IEnumerable<IDynamicLine> Lines { get; }
        ContainingCodeTrackerType Type { get; }

        bool Update(
            TrackingSpanRangeProcessResult trackingSpanRangeProcessResult, 
            ITextSnapshot currentSnapshot,
            List<SpanAndLineRange> newSpanAndLineRanges);
    }
}

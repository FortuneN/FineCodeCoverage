using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface IUpdatableDynamicLines
    {
        IEnumerable<IDynamicLine> Lines { get; }

        bool Update(
            TrackingSpanRangeProcessResult trackingSpanRangeProcessResult, 
            ITextSnapshot currentSnapshot,
            List<SpanAndLineRange> newSpanAndLineRanges);
    }
}

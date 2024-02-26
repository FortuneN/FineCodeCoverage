using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface IContainingCodeTrackerProcessResult
    {
        bool IsEmpty { get; }
        bool Changed { get; }
        List<SpanAndLineRange> UnprocessedSpans { get; }
    }
    interface IContainingCodeTracker
    {
        IContainingCodeTrackerProcessResult ProcessChanges(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanAndLineRanges);

        IEnumerable<IDynamicLine> Lines { get; }
    }
}

using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface IContainingCodeTrackerProcessResult
    {
        bool IsEmpty { get; }
        bool Changed { get; }
        List<SpanAndLineRange> UnprocessedSpans { get; }
    }
}

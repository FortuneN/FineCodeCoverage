using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class ContainingCodeTrackerProcessResult : IContainingCodeTrackerProcessResult
    {
        public ContainingCodeTrackerProcessResult(bool changed, List<SpanAndLineRange> unprocessedSpans, bool isEmpty)
        {
            Changed = changed;
            UnprocessedSpans = unprocessedSpans;
            IsEmpty = isEmpty;
        }
        public bool IsEmpty { get; }
        public bool Changed { get; set; }

        public List<SpanAndLineRange> UnprocessedSpans { get; }
    }
}

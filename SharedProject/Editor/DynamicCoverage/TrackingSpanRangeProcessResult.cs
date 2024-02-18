using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class TrackingSpanRangeProcessResult
    {
        public TrackingSpanRangeProcessResult(List<SpanAndLineRange> nonIntersectingSpans, bool isEmpty, bool textChanged)
        {
            NonIntersectingSpans = nonIntersectingSpans;
            IsEmpty = isEmpty;
            TextChanged = textChanged;
        }
        public List<SpanAndLineRange> NonIntersectingSpans { get; }
        public bool IsEmpty { get; }
        public bool TextChanged { get; }
    }
}

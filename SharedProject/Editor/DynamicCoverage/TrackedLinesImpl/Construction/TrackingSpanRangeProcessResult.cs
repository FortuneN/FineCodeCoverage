using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class TrackingSpanRangeProcessResult
    {
        public TrackingSpanRangeProcessResult(ITrackingSpanRange trackingSpanRange, List<SpanAndLineRange> nonIntersectingSpans, bool isEmpty, bool textChanged)
        {
            this.TrackingSpanRange = trackingSpanRange;
            this.NonIntersectingSpans = nonIntersectingSpans;
            this.IsEmpty = isEmpty;
            this.TextChanged = textChanged;
        }
        public ITrackingSpanRange TrackingSpanRange { get; }
        public List<SpanAndLineRange> NonIntersectingSpans { get; }
        public bool IsEmpty { get; }
        public bool TextChanged { get; }
    }
}

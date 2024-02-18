using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    class TrackingSpanRangeProcessResult
    {
        public TrackingSpanRangeProcessResult(List<SpanAndLineRange> nonIntersectingSpans, bool isEmpty,bool textChanged)
        {
            NonIntersectingSpans = nonIntersectingSpans;
            IsEmpty = isEmpty;
            TextChanged = textChanged;
        }
        public List<SpanAndLineRange> NonIntersectingSpans { get; }
        public bool IsEmpty { get; }
        public bool TextChanged { get; }
    }
    interface ITrackingSpanRange
    {
        TrackingSpanRangeProcessResult Process(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanAndLIneRanges);
        ITrackingSpan GetFirstTrackingSpan();
    }

}

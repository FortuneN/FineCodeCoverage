using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    class NonIntersectingResult
    {
        public NonIntersectingResult(List<SpanAndLineRange> nonIntersectingSpans, bool isEmpty,bool textChanged)
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
        NonIntersectingResult GetNonIntersecting(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanChanges);
        ITrackingSpan GetFirstTrackingSpan();
    }

}

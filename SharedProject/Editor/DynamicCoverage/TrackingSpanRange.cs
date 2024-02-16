using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class TrackingSpanRange : ITrackingSpanRange
    {
        private readonly List<ITrackingSpan> trackingSpans;
        private string lastRangeText;

        public TrackingSpanRange(List<ITrackingSpan> trackingSpans, ITextSnapshot currentSnapshot)
        {
            this.trackingSpans = trackingSpans;
            var (currentFirstSpan, currentEndSpan) = GetEndCurrent(currentSnapshot);
            SetRangeText(currentSnapshot, currentFirstSpan, currentEndSpan);
        }
        
        private (SnapshotSpan, SnapshotSpan) GetEndCurrent(ITextSnapshot currentSnapshot)
        {
            var currentFirstSpan = trackingSpans.First().GetSpan(currentSnapshot);
            var currentEndSpan = trackingSpans.Last().GetSpan(currentSnapshot);
            return (currentFirstSpan, currentEndSpan);
        }
        
        private void SetRangeText(ITextSnapshot currentSnapshot,SnapshotSpan currentFirstSpan, SnapshotSpan currentEndSpan)
        {
            lastRangeText = currentSnapshot.GetText(new Span(currentFirstSpan.Start, currentEndSpan.End - currentFirstSpan.Start));
        }
        
        public NonIntersectingResult GetNonIntersecting(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanChanges)
        {
            var (currentFirstSpan, currentEndSpan) = GetEndCurrent(currentSnapshot);
            var previousRangeText = lastRangeText;
            SetRangeText(currentSnapshot, currentFirstSpan, currentEndSpan);
            var textChanged = previousRangeText != lastRangeText;
            var isEmpty = string.IsNullOrWhiteSpace(lastRangeText);

            var currentFirstTrackedLineNumber = currentSnapshot.GetLineNumberFromPosition(currentFirstSpan.End);
            var currentEndTrackedLineNumber = currentSnapshot.GetLineNumberFromPosition(currentEndSpan.End);
            newSpanChanges = newSpanChanges.Where(spanAndLineNumber =>
            {
                return OutsideRange(currentFirstTrackedLineNumber, currentEndTrackedLineNumber, spanAndLineNumber.StartLineNumber)
                &&
                OutsideRange(currentFirstTrackedLineNumber, currentEndTrackedLineNumber, spanAndLineNumber.EndLineNumber);
            }).ToList();
            
            return new NonIntersectingResult(newSpanChanges, isEmpty,textChanged);
        }

        private bool OutsideRange(int firstLineNumber, int endLineNumber, int spanLineNumber)
        {
            return spanLineNumber < firstLineNumber || spanLineNumber > endLineNumber;
        }

        public ITrackingSpan GetFirstTrackingSpan()
        {
            return trackingSpans.First();
        }
    }

}

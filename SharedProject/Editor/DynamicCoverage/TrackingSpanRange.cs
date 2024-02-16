using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class TrackingSpanRange : ITrackingSpanRange
    {
        private readonly ITrackingSpan startTrackingSpan;
        private readonly ITrackingSpan endTrackingSpan;
        private string lastRangeText;

        public TrackingSpanRange(ITrackingSpan startTrackingSpan, ITrackingSpan endTrackingSpan,ITextSnapshot currentSnapshot)
        {
            this.startTrackingSpan = startTrackingSpan;
            this.endTrackingSpan = endTrackingSpan;
            var (currentStartSpan, currentEndSpan) = GetCurrentRange(currentSnapshot);
            SetRangeText(currentSnapshot, currentStartSpan, currentEndSpan);
        }
        
        private (SnapshotSpan, SnapshotSpan) GetCurrentRange(ITextSnapshot currentSnapshot)
        {
            var currentStartSpan = startTrackingSpan.GetSpan(currentSnapshot);
            var currentEndSpan = endTrackingSpan.GetSpan(currentSnapshot);
            return (currentStartSpan, currentEndSpan);
        }
        
        private void SetRangeText(ITextSnapshot currentSnapshot,SnapshotSpan currentFirstSpan, SnapshotSpan currentEndSpan)
        {
            lastRangeText = currentSnapshot.GetText(new Span(currentFirstSpan.Start, currentEndSpan.End - currentFirstSpan.Start));
        }
        
        public TrackingSpanRangeProcessResult Process(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanChanges)
        {
            var (currentFirstSpan, currentEndSpan) = GetCurrentRange(currentSnapshot);
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
            
            return new TrackingSpanRangeProcessResult(newSpanChanges, isEmpty,textChanged);
        }

        private bool OutsideRange(int firstLineNumber, int endLineNumber, int spanLineNumber)
        {
            return spanLineNumber < firstLineNumber || spanLineNumber > endLineNumber;
        }

        public ITrackingSpan GetFirstTrackingSpan()
        {
            return startTrackingSpan;
        }
    }

}

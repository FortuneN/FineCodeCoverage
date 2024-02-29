using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class TrackingSpanRange : ITrackingSpanRange
    {
        private readonly ITrackingSpan startTrackingSpan;
        private readonly ITrackingSpan endTrackingSpan;
        private readonly ILineTracker lineTracker;
        private string lastRangeText;
        private CodeSpanRange codeSpanRange;

        public TrackingSpanRange(
            ITrackingSpan startTrackingSpan, 
            ITrackingSpan endTrackingSpan,
            ITextSnapshot currentSnapshot,
            ILineTracker lineTracker
        )
        {
            this.startTrackingSpan = startTrackingSpan;
            this.endTrackingSpan = endTrackingSpan;
            this.lineTracker = lineTracker;
            var (currentStartSpan, currentEndSpan) = GetCurrentRange(currentSnapshot);
            SetRangeText(currentSnapshot, currentStartSpan, currentEndSpan);
        }
        
        private (SnapshotSpan, SnapshotSpan) GetCurrentRange(ITextSnapshot currentSnapshot)
        {
            var currentStartSpan = startTrackingSpan.GetSpan(currentSnapshot);
            var currentEndSpan = endTrackingSpan.GetSpan(currentSnapshot);
            var startLineNumber = lineTracker.GetLineNumber(startTrackingSpan, currentSnapshot,false);
            var endLineNumber = lineTracker.GetLineNumber(endTrackingSpan, currentSnapshot, true);
            codeSpanRange = new CodeSpanRange(startLineNumber, endLineNumber);
            return (currentStartSpan, currentEndSpan);
        }
        
        private void SetRangeText(ITextSnapshot currentSnapshot,SnapshotSpan currentFirstSpan, SnapshotSpan currentEndSpan)
        {
            lastRangeText = currentSnapshot.GetText(new Span(currentFirstSpan.Start, currentEndSpan.End - currentFirstSpan.Start));
        }
        
        public TrackingSpanRangeProcessResult Process(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanAndLineRanges)
        {
            var (currentFirstSpan, currentEndSpan) = GetCurrentRange(currentSnapshot);
            var (isEmpty, textChanged) = GetTextChangeInfo(currentSnapshot, currentFirstSpan, currentEndSpan);
            var nonIntersecting = GetNonIntersecting(currentSnapshot, currentFirstSpan, currentEndSpan, newSpanAndLineRanges);
            return new TrackingSpanRangeProcessResult(this,nonIntersecting, isEmpty,textChanged);
        }

        private (bool isEmpty,bool textChanged) GetTextChangeInfo(ITextSnapshot currentSnapshot, SnapshotSpan currentFirstSpan, SnapshotSpan currentEndSpan)
        {
            var previousRangeText = lastRangeText;
            SetRangeText(currentSnapshot, currentFirstSpan, currentEndSpan);
            var textChanged = previousRangeText != lastRangeText;
            var isEmpty = string.IsNullOrWhiteSpace(lastRangeText);
            return (isEmpty, textChanged);

        }

        private List<SpanAndLineRange> GetNonIntersecting(
            ITextSnapshot currentSnapshot,SnapshotSpan currentFirstSpan, SnapshotSpan currentEndSpan,List<SpanAndLineRange> newSpanAndLineRanges)
        {
            var currentFirstTrackedLineNumber = currentSnapshot.GetLineNumberFromPosition(currentFirstSpan.End);
            var currentEndTrackedLineNumber = currentSnapshot.GetLineNumberFromPosition(currentEndSpan.End);
            return newSpanAndLineRanges.Where(spanAndLineNumber =>
            {
                return OutsideRange(currentFirstTrackedLineNumber, currentEndTrackedLineNumber, spanAndLineNumber.StartLineNumber)
                &&
                OutsideRange(currentFirstTrackedLineNumber, currentEndTrackedLineNumber, spanAndLineNumber.EndLineNumber);
            }).ToList();
        }

        private bool OutsideRange(int firstLineNumber, int endLineNumber, int spanLineNumber)
        {
            return spanLineNumber < firstLineNumber || spanLineNumber > endLineNumber;
        }

        public ITrackingSpan GetFirstTrackingSpan()
        {
            return startTrackingSpan;
        }

        public CodeSpanRange ToCodeSpanRange() => codeSpanRange;
        
    }

}

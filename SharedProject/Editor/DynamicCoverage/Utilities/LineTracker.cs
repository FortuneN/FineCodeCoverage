using Microsoft.VisualStudio.Text;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(ITrackingLineFactory))]
    [Export(typeof(ILineTracker))]
    internal class LineTracker : ILineTracker, ITrackingLineFactory
    {
        public int GetLineNumber(ITrackingSpan trackingSpan, ITextSnapshot currentSnapshot, bool lineFromEnd)
        {
            SnapshotPoint position = this.GetPoint(trackingSpan, currentSnapshot, lineFromEnd);
            return currentSnapshot.GetLineNumberFromPosition(position);
        }

        private SnapshotPoint GetPoint(ITrackingSpan trackingSpan, ITextSnapshot currentSnapshot, bool lineFromEnd) 
            => lineFromEnd ? trackingSpan.GetEndPoint(currentSnapshot) : trackingSpan.GetStartPoint(currentSnapshot);

        public TrackedLineInfo GetTrackedLineInfo(ITrackingSpan trackingSpan, ITextSnapshot currentSnapshot, bool lineFromEnd)
        {
            SnapshotPoint position = this.GetPoint(trackingSpan, currentSnapshot, lineFromEnd);

            ITextSnapshotLine line = currentSnapshot.GetLineFromPosition(position);
            int lineNumber = line.LineNumber;
            string text = currentSnapshot.GetText(line.Extent);
            
            return new TrackedLineInfo(lineNumber, text);
        }

        public ITrackingSpan CreateTrackingSpan(ITextSnapshot textSnapshot, int lineNumber, SpanTrackingMode spanTrackingMode)
        {
            SnapshotSpan span = textSnapshot.GetLineFromLineNumber(lineNumber).Extent;
            return textSnapshot.CreateTrackingSpan(span, spanTrackingMode);
        }
    }
}

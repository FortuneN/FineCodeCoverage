using Microsoft.VisualStudio.Text;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(ITrackingLineFactory))]
    internal class LineTracker : ILineTracker, ITrackingLineFactory
    {
        public int GetLineNumber(ITrackingSpan trackingSpan, ITextSnapshot currentSnapshot, bool lineFromEnd)
        {
            var position = GetPoint(trackingSpan, currentSnapshot, lineFromEnd);
            return currentSnapshot.GetLineNumberFromPosition(position);
        }

        private SnapshotPoint GetPoint(ITrackingSpan trackingSpan, ITextSnapshot currentSnapshot, bool lineFromEnd)
        {
            return lineFromEnd ? trackingSpan.GetEndPoint(currentSnapshot) : trackingSpan.GetStartPoint(currentSnapshot);
        }

        public TrackedLineInfo GetTrackedLineInfo(ITrackingSpan trackingSpan, ITextSnapshot currentSnapshot, bool lineFromEnd)
        {
            var position = GetPoint(trackingSpan, currentSnapshot, lineFromEnd);

            var line = currentSnapshot.GetLineFromPosition(position);
            var lineNumber = line.LineNumber;
            var text = currentSnapshot.GetText(line.Extent);
            
            return new TrackedLineInfo(lineNumber, text);
        }

        public ITrackingSpan CreateTrackingSpan(ITextSnapshot textSnapshot, int lineNumber, SpanTrackingMode spanTrackingMode)
        {
            var span = textSnapshot.GetLineFromLineNumber(lineNumber).Extent;
            return textSnapshot.CreateTrackingSpan(span, spanTrackingMode);
        }
    }
}

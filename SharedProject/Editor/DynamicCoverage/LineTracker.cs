using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class LineTracker : ILineTracker
    {
        public TrackedLineInfo GetTrackedLineInfo(ITrackingSpan trackingSpan, ITextSnapshot currentSnapshot, bool lineFromEnd, bool getText)
        {
            var position = lineFromEnd ? trackingSpan.GetEndPoint(currentSnapshot) : trackingSpan.GetStartPoint(currentSnapshot);
            
            string text = "";
            int lineNumber;
            if (getText)
            {
                var line = currentSnapshot.GetLineFromPosition(position);
                lineNumber = line.LineNumber;
                var extent = line.Extent;
                text = currentSnapshot.GetText(extent);
            }
            else
            {
                lineNumber = currentSnapshot.GetLineNumberFromPosition(position);
            }
            return new TrackedLineInfo(lineNumber, text);
        }
    }
}

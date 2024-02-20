using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class LineTracker : ILineTracker
    {
        public TrackedLineInfo GetTrackedLineInfo(ITrackingSpan trackingSpan, ITextSnapshot currentSnapshot, bool lineFromEnd, bool getText)
        {
            var newSnapshotSpan = trackingSpan.GetSpan(currentSnapshot);
            var line = currentSnapshot.GetLineFromPosition(lineFromEnd ? newSnapshotSpan.End : newSnapshotSpan.Start);
            string text = "";
            if (getText)
            {
                var extent = line.Extent;
                text = currentSnapshot.GetText(extent);
            }
            return new TrackedLineInfo(line.LineNumber, text);
        }
    }
}

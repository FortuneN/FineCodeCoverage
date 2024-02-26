using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class NotIncludedTrackingLine : TrackingLine
    {
        public NotIncludedTrackingLine(
            ITrackingSpan startTrackingSpan,
            ITextSnapshot currentSnapshot,
            ILineTracker lineTracker
            ) : base(startTrackingSpan, currentSnapshot, lineTracker, DynamicCoverageType.NotIncluded)
        {
        }
    }
}

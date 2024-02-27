using Microsoft.VisualStudio.Text;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
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

using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ITrackingSpanRangeContainingCodeTrackerFactory
    {
        IContainingCodeTracker CreateCoverageLines(ITrackingSpanRange trackingSpanRange, ITrackedCoverageLines trackedCoverageLines);
        IContainingCodeTracker CreateDirty(ITrackingSpanRange trackingSpanRange, ITextSnapshot textSnapshot);
        IContainingCodeTracker CreateNotIncluded(ITrackingLine trackingLine, ITrackingSpanRange trackingSpanRange);
        IContainingCodeTracker CreateOtherLines(ITrackingSpanRange trackingSpanRange);
    }
}

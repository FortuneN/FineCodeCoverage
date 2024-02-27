namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface ITrackingSpanRangeContainingCodeTrackerFactory
    {
        IContainingCodeTracker CreateCoverageLines(ITrackingSpanRange trackingSpanRange, ITrackedCoverageLines trackedCoverageLines);
        IContainingCodeTracker CreateNotIncluded(ITrackingLine trackingLine, ITrackingSpanRange trackingSpanRange);
        IContainingCodeTracker CreateOtherLinesTracker(ITrackingSpanRange trackingSpanRange);
    }

}

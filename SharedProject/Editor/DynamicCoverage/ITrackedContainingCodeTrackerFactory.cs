namespace FineCodeCoverage.Impl
{
    interface ITrackedContainingCodeTrackerFactory
    {
        IContainingCodeTracker Create(ITrackedCoverageLines trackedCoverageLines);
        IContainingCodeTracker Create(ITrackingSpanRange trackingSpanRange, ITrackedCoverageLines trackedCoverageLines);
    }

}

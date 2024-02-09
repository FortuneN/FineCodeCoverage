using System.ComponentModel.Composition;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(ITrackedContainingCodeTrackerFactory))]
    internal class TrackedContainingCodeTrackerFactory : ITrackedContainingCodeTrackerFactory
    {
        public IContainingCodeTracker Create(ITrackedCoverageLines trackedCoverageLines)
        {
            return new ContainingCodeTracker(trackedCoverageLines);
        }

        public IContainingCodeTracker Create(ITrackingSpanRange trackingSpanRange, ITrackedCoverageLines trackedCoverageLines)
        {
            return new ContainingCodeTracker(trackedCoverageLines, trackingSpanRange);
        }
    }
}

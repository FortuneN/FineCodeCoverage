using System.ComponentModel.Composition;

namespace FineCodeCoverage.Editor.DynamicCoverage
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

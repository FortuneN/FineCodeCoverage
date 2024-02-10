using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
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

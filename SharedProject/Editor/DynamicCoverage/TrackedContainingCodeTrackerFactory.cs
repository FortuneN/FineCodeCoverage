using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(ITrackedContainingCodeTrackerFactory))]
    internal class TrackedContainingCodeTrackerFactory : ITrackedContainingCodeTrackerFactory
    {
        private readonly IDirtyLineFactory dirtyLineFactory;

        [ImportingConstructor]
        public TrackedContainingCodeTrackerFactory(
            IDirtyLineFactory dirtyLineFactory
        )
        {
            this.dirtyLineFactory = dirtyLineFactory;
        }
        public IContainingCodeTracker Create(ITrackedCoverageLines trackedCoverageLines)
        {
            return new ContainingCodeTracker(trackedCoverageLines,dirtyLineFactory);
        }

        public IContainingCodeTracker Create(ITrackingSpanRange trackingSpanRange, ITrackedCoverageLines trackedCoverageLines)
        {
            return new ContainingCodeTracker(trackedCoverageLines,dirtyLineFactory, trackingSpanRange);
        }

        public IContainingCodeTracker Create(ITrackingLine trackingLine, ITrackingSpanRange trackingSpanRange)
        {
            return new ContainingCodeTracker(trackingLine, trackingSpanRange);
        }
    }
}

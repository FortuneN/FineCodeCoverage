using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(ITrackingSpanRangeContainingCodeTrackerFactory))]
    internal class TrackingSpanRangeContainingCodeTrackerFactory : ITrackingSpanRangeContainingCodeTrackerFactory
    {
        private readonly IDirtyLineFactory dirtyLineFactory;

        [ImportingConstructor]
        public TrackingSpanRangeContainingCodeTrackerFactory(
            IDirtyLineFactory dirtyLineFactory
        )
        {
            this.dirtyLineFactory = dirtyLineFactory;
        }

        public IContainingCodeTracker CreateCoverageLines(ITrackingSpanRange trackingSpanRange, ITrackedCoverageLines trackedCoverageLines)
        {
            return Wrap(trackingSpanRange, new CoverageCodeTracker(trackedCoverageLines, dirtyLineFactory));
        }

        public IContainingCodeTracker CreateNotIncluded(ITrackingLine trackingLine, ITrackingSpanRange trackingSpanRange)
        {
            return Wrap(trackingSpanRange, new NotIncludedCodeTracker(trackingLine));
        }

        public IContainingCodeTracker CreateOtherLinesTracker(ITrackingSpanRange trackingSpanRange)
        {
            return Wrap(trackingSpanRange, new OtherLinesTracker());
        }

        private IContainingCodeTracker Wrap(ITrackingSpanRange trackingSpanRange, IUpdatableDynamicLines updatableDynamicLines)
        {
            return new TrackingSpanRangeUpdatingTracker(trackingSpanRange, updatableDynamicLines);
        }
    }
}

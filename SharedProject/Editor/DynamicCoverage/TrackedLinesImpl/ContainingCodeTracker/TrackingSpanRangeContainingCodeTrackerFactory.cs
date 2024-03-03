using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Text;

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
        ) => this.dirtyLineFactory = dirtyLineFactory;

        public IContainingCodeTracker CreateCoverageLines(ITrackingSpanRange trackingSpanRange, ITrackedCoverageLines trackedCoverageLines)
            => this.Wrap(trackingSpanRange, new CoverageCodeTracker(trackedCoverageLines, this.dirtyLineFactory));

        public IContainingCodeTracker CreateDirty(ITrackingSpanRange trackingSpanRange, ITextSnapshot textSnapshot)
            => this.Wrap(trackingSpanRange, new DirtyCodeTracker(this.dirtyLineFactory.Create(trackingSpanRange.GetFirstTrackingSpan(), textSnapshot)));

        public IContainingCodeTracker CreateNotIncluded(ITrackingLine trackingLine, ITrackingSpanRange trackingSpanRange)
            => this.Wrap(trackingSpanRange, new NotIncludedCodeTracker(trackingLine));

        public IContainingCodeTracker CreateOtherLines(ITrackingSpanRange trackingSpanRange)
            => this.Wrap(trackingSpanRange, new OtherLinesTracker());

        private IContainingCodeTracker Wrap(ITrackingSpanRange trackingSpanRange, IUpdatableDynamicLines updatableDynamicLines)
            => new TrackingSpanRangeUpdatingTracker(trackingSpanRange, updatableDynamicLines);
    }
}

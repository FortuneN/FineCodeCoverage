namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class DirtyCodeTracker : TrackingLineTracker
    {
        public DirtyCodeTracker(
            ITrackingLine notIncludedTrackingLine
            ) : base(notIncludedTrackingLine, ContainingCodeTrackerType.CoverageLines)
        {
        }
    }
}

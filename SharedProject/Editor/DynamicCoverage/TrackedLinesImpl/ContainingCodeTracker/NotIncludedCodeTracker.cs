namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class NotIncludedCodeTracker : TrackingLineTracker
    {
        public NotIncludedCodeTracker(
            ITrackingLine notIncludedTrackingLine
            ) : base(notIncludedTrackingLine, ContainingCodeTrackerType.NotIncluded)
        {
        }
    }
}

using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    class TrackedNewCodeLine : IDynamicLine
    {
        public int ActualLineNumber { get; set; }
        public ITrackingSpan TrackingSpan { get; }

        public int Number => ActualLineNumber + 1;

        public DynamicCoverageType CoverageType => DynamicCoverageType.NewLine;

        public TrackedNewCodeLine(int number, ITrackingSpan trackingSpan)
        {
            ActualLineNumber = number;
            TrackingSpan = trackingSpan;
        }
    }
}

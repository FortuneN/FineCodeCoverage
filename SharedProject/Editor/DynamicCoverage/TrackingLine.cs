using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class TrackingLine : ITrackingLine
    {
        private readonly ITrackingSpan startTrackingSpan;
        private readonly ILineTracker lineTracker;
        private readonly DynamicCoverageType dynamicCoverageType;

        public IDynamicLine Line { get; private set; }
        public TrackingLine(ITrackingSpan startTrackingSpan, ITextSnapshot currentSnapshot, ILineTracker lineTracker, DynamicCoverageType dynamicCoverageType)
        {
            this.startTrackingSpan = startTrackingSpan;
            this.lineTracker = lineTracker;
            this.dynamicCoverageType = dynamicCoverageType;
            SetLine(currentSnapshot);
        }

        private void SetLine(ITextSnapshot currentSnapshot)
        {
            var startLineNumber = lineTracker.GetLineNumber(startTrackingSpan, currentSnapshot, false);

            Line = new DynamicLine(startLineNumber, dynamicCoverageType);
        }

        public bool Update(ITextSnapshot currentSnapshot)
        {
            var currentFirstLineNumber = Line.Number;
            SetLine(currentSnapshot);
            return currentFirstLineNumber != Line.Number;
        }

    }

}

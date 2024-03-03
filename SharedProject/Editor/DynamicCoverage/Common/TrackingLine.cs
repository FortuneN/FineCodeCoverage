using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class TrackingLine : ITrackingLine
    {
        private readonly ITrackingSpan startTrackingSpan;
        private readonly ILineTracker lineTracker;
        private readonly DynamicCoverageType dynamicCoverageType;

        public IDynamicLine Line { get; private set; }
        public TrackingLine(
            ITrackingSpan startTrackingSpan,
            ITextSnapshot currentSnapshot,
            ILineTracker lineTracker,
            DynamicCoverageType dynamicCoverageType)
        {
            this.startTrackingSpan = startTrackingSpan;
            this.lineTracker = lineTracker;
            this.dynamicCoverageType = dynamicCoverageType;
            this.SetLine(currentSnapshot);
        }

        private void SetLine(ITextSnapshot currentSnapshot)
        {
            int startLineNumber = this.lineTracker.GetLineNumber(this.startTrackingSpan, currentSnapshot, false);

            this.Line = new DynamicLine(startLineNumber, this.dynamicCoverageType);
        }

        public bool Update(ITextSnapshot currentSnapshot)
        {
            int currentFirstLineNumber = this.Line.Number;
            this.SetLine(currentSnapshot);
            return currentFirstLineNumber != this.Line.Number;
        }
    }
}

using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class DirtyLine : IDirtyLine
    {
        private readonly ITrackingSpan startTrackingSpan;
        private readonly ILineTracker lineTracker;

        public IDynamicLine Line { get; private set; }
        public DirtyLine(ITrackingSpan startTrackingSpan, ITextSnapshot currentSnapshot, ILineTracker lineTracker)
        {
            this.startTrackingSpan = startTrackingSpan;
            this.lineTracker = lineTracker;
            SetLine(currentSnapshot);
        }

        private void SetLine(ITextSnapshot currentSnapshot)
        {
            var startLineNumber = lineTracker.GetTrackedLineInfo(startTrackingSpan, currentSnapshot, false, false).LineNumber;

            Line = new DynamicLine(startLineNumber,DynamicCoverageType.Dirty);
        }

        public bool Update(ITextSnapshot currentSnapshot)
        {
            var currentFirstLineNumber = Line.Number;
            SetLine(currentSnapshot);
            return currentFirstLineNumber != Line.Number;
        }

    }

}

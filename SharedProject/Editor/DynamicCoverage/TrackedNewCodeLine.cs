using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class TrackedNewCodeLine : ITrackedNewCodeLine
    {
        private readonly ITrackingSpan trackingSpan;
        private readonly DynamicLine line;
        private readonly ILineTracker lineTracker;

        public TrackedNewCodeLine(ITrackingSpan trackingSpan, int lineNumber, ILineTracker lineTracker)
        {
            line = new DynamicLine(lineNumber, DynamicCoverageType.NewLine);
            this.lineTracker = lineTracker;
            this.trackingSpan = trackingSpan;
        }

        public IDynamicLine Line => line;

        public string GetText(ITextSnapshot currentSnapshot)
        {
            return lineTracker.GetTrackedLineInfo(trackingSpan, currentSnapshot, true).LineText;
        }

        public TrackedNewCodeLineUpdate Update(ITextSnapshot currentSnapshot)
        {
            var trackedLineInfo = lineTracker.GetTrackedLineInfo(trackingSpan, currentSnapshot, true);
            var changed = line.ActualLineNumber != trackedLineInfo.LineNumber;
            line.ActualLineNumber = trackedLineInfo.LineNumber;
            return new TrackedNewCodeLineUpdate(trackedLineInfo.LineText, line.ActualLineNumber, changed);
        }
    }

}

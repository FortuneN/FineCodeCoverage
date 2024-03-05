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
            this.line = new DynamicLine(lineNumber, DynamicCoverageType.NewLine);
            this.lineTracker = lineTracker;
            this.trackingSpan = trackingSpan;
        }

        public IDynamicLine Line => this.line;

        public string GetText(ITextSnapshot currentSnapshot)
            => this.lineTracker.GetTrackedLineInfo(this.trackingSpan, currentSnapshot, true).LineText;

        public TrackedNewCodeLineUpdate Update(ITextSnapshot currentSnapshot)
        {
            int oldLineNumber = this.line.Number;
            TrackedLineInfo trackedLineInfo = this.lineTracker.GetTrackedLineInfo(this.trackingSpan, currentSnapshot, true);
            this.line.Number = trackedLineInfo.LineNumber;
            return new TrackedNewCodeLineUpdate(trackedLineInfo.LineText, this.line.Number, oldLineNumber);
        }
    }
}

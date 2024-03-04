using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class CoverageLine : ICoverageLine
    {
        private readonly ITrackingSpan trackingSpan;
        private readonly ILineTracker lineTracker;
        private readonly TrackedLineLine line;
        public IDynamicLine Line => this.line;

        public CoverageLine(ITrackingSpan trackingSpan, ILine line, ILineTracker lineTracker)
        {
            this.line = new TrackedLineLine(line);
            this.trackingSpan = trackingSpan;
            this.lineTracker = lineTracker;
        }

        public bool Update(ITextSnapshot currentSnapshot)
        {
            bool updated = false;
            int newLineNumber = this.lineTracker.GetLineNumber(this.trackingSpan, currentSnapshot, true);
            if (newLineNumber != this.Line.Number)
            {
                this.line.Number = newLineNumber;
                updated = true;
            }

            return updated;
        }
    }
}

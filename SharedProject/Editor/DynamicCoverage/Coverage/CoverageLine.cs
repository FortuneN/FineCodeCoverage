using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    class CoverageLine : ICoverageLine
    {
        private readonly ITrackingSpan trackingSpan;
        private readonly ILineTracker lineTracker;
        private readonly TrackedLineLine line;
        public IDynamicLine Line => line;

        public CoverageLine(ITrackingSpan trackingSpan, ILine line, ILineTracker lineTracker)
        {
            this.line = new TrackedLineLine(line);
            this.trackingSpan = trackingSpan;
            this.lineTracker = lineTracker;
        }

        public bool Update(ITextSnapshot currentSnapshot)
        {
            var updated = false;
            var newLineNumber = lineTracker.GetLineNumber(trackingSpan, currentSnapshot, true);
            if (newLineNumber != Line.Number)
            {
                line.Number = newLineNumber;
                updated = true;
            }
            return updated;
        }
    }

}

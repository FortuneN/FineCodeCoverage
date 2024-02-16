using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    class CoverageLine : ICoverageLine
    {
        private readonly ITrackingSpan trackingSpan;
        private readonly TrackedLineLine line;
        public IDynamicLine Line => line;

        public void Dirty()
        {
            line.Dirty();
        }

        public CoverageLine(ITrackingSpan trackingSpan, ILine line)
        {
            this.line = new TrackedLineLine(line);
            this.trackingSpan = trackingSpan;
        }

        public CoverageLineUpdateType Update(ITextSnapshot currentSnapshot)
        {
            var newSnapshotSpan = trackingSpan.GetSpan(currentSnapshot);
            if (newSnapshotSpan.IsEmpty)
            {
                return CoverageLineUpdateType.Removal;
            }
            else
            {
                var newLineNumber = currentSnapshot.GetLineNumberFromPosition(newSnapshotSpan.End) + 1;
                if (newLineNumber != Line.Number)
                {
                    line.Number = newLineNumber;
                    return CoverageLineUpdateType.LineNumberChange;
                }
            }
            return CoverageLineUpdateType.NoChange;
        }
    }

}

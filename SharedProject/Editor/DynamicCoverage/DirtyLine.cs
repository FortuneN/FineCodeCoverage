using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class DirtyLine : IDirtyLine
    {
        private ITrackingSpan startTrackingSpan;
        public IDynamicLine Line { get; private set; }
        public DirtyLine(ITrackingSpan startTrackingSpan, ITextSnapshot currentSnapshot)
        {
            this.startTrackingSpan = startTrackingSpan;
            SetLine(currentSnapshot);
        }

        private void SetLine(ITextSnapshot currentSnapshot)
        {
            var startLineNumber = currentSnapshot.GetLineNumberFromPosition(startTrackingSpan.GetStartPoint(currentSnapshot));

            Line = new DirtyDynamicLine(startLineNumber + 1);
        }

        public bool Update(ITextSnapshot currentSnapshot)
        {
            var currentFirstLineNumber = Line.Number;
            SetLine(currentSnapshot);
            return currentFirstLineNumber != Line.Number;
        }

    }

}

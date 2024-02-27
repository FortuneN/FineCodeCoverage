using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class DirtyLine : TrackingLine, IDirtyLine
    {
        public DirtyLine(ITrackingSpan startTrackingSpan, ITextSnapshot currentSnapshot, ILineTracker lineTracker) : 
            base(startTrackingSpan,currentSnapshot,lineTracker, DynamicCoverageType.Dirty)
        {          
        }
    }

}

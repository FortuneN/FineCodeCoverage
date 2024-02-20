using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ILineTracker
    {
        TrackedLineInfo GetTrackedLineInfo(ITrackingSpan trackingSpan, ITextSnapshot currentSnapshot, bool lineFromEnd, bool getText);
    }
}

using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ILineTracker
    {
        int GetLineNumber(ITrackingSpan trackingSpan, ITextSnapshot currentSnapshot, bool lineFromEnd);

        TrackedLineInfo GetTrackedLineInfo(ITrackingSpan trackingSpan, ITextSnapshot currentSnapshot, bool lineFromEnd);
    }
}

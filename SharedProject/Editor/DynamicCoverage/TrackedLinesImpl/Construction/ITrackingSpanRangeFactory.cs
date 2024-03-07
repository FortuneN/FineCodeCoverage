using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ITrackingSpanRangeFactory
    {
        ITrackingSpanRange Create(ITrackingSpan startTrackingSpan, ITrackingSpan endTrackingSpan, ITextSnapshot currentSnapshot);
    }
}

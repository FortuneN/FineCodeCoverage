using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface ITrackingLineFactory
    {
        ITrackingSpan Create(ITextSnapshot textSnapshot, int lineNumber, SpanTrackingMode spanTrackingMode);
    }

}

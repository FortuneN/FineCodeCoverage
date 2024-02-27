using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface ITrackingLineFactory
    {
        ITrackingSpan CreateTrackingSpan(ITextSnapshot textSnapshot, int lineNumber, SpanTrackingMode spanTrackingMode);
    }

}

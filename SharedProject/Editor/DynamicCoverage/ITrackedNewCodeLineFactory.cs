using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface ITrackedNewCodeLineFactory
    {
        ITrackedNewCodeLine Create(ITextSnapshot textSnapshot, SpanTrackingMode spanTrackingMode, int lineNumber);
    }
}

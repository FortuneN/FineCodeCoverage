using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ITrackedNewCodeLineFactory
    {
        ITrackedNewCodeLine Create(ITextSnapshot textSnapshot, SpanTrackingMode spanTrackingMode, int lineNumber);
    }
}

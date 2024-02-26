using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface INotIncludedLineFactory
    {
        ITrackingLine Create(ITrackingSpan startTrackingSpan, ITextSnapshot currentSnapshot);
    }
}

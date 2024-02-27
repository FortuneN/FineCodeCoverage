using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface IDirtyLineFactory
    {
        IDirtyLine Create(ITrackingSpan trackingSpan, ITextSnapshot snapshot);
    }
}

using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Impl
{
    interface ITrackingLineFactory
    {
        ITrackingSpan Create(ITextSnapshot textSnapshot, int lineNumber);
    }

}

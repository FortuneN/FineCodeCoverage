using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Impl
{
    interface ILineSpan
    {
        IDynamicLine Line { get; }
        SnapshotSpan Span { get; }
    }
}

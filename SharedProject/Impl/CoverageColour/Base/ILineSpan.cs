using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Impl
{
    interface ILineSpan
    {
        ILine Line { get; }
        SnapshotSpan Span { get; }
    }
}

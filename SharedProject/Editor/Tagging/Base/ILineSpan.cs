using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.Tagging.Base
{
    internal interface ILineSpan
    {
        IDynamicLine Line { get; }
        SnapshotSpan Span { get; }
    }
}

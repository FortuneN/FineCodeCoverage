using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.Tagging.Base
{
    internal class LineSpan : ILineSpan
    {
        public LineSpan(IDynamicLine line, SnapshotSpan span)
        {
            Line = line;
            Span = span;
        }
        public IDynamicLine Line { get; }

        public SnapshotSpan Span { get; }
    }
}

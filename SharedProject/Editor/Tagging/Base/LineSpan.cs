using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Impl
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

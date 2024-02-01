using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Impl
{
    internal class LineSpan : ILineSpan
    {
        public LineSpan(ILine line, SnapshotSpan span)
        {
            Line = line;
            Span = span;
        }
        public ILine Line { get; }

        public SnapshotSpan Span { get; }
    }
}

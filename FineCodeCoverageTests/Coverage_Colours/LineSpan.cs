using FineCodeCoverage.Impl;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverageTests
{
    internal class LineSpan : ILineSpan
    {
        public IDynamicLine Line { get; set; }

        public SnapshotSpan Span { get; set; }
    }
}
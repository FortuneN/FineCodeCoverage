using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverageTests
{
    internal class LineSpan : ILineSpan
    {
        public ILine Line { get; set; }

        public SnapshotSpan Span { get; set; }
    }
}
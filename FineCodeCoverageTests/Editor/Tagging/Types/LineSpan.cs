using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.Tagging.Base;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverageTests.Editor.Tagging.Types
{
    internal class LineSpan : ILineSpan
    {
        public IDynamicLine Line { get; set; }

        public SnapshotSpan Span { get; set; }
    }
}
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface INewCodeTracker
    {
        IEnumerable<IDynamicLine> Lines { get; }

        IEnumerable<int> GetChangedLineNumbers(
            ITextSnapshot currentSnapshot,
            List<SpanAndLineRange> newSpanChanges,
            IEnumerable<CodeSpanRange> newCodeCodeRanges);
    }
}

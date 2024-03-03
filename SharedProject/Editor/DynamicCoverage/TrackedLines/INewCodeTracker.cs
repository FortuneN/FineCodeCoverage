using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface INewCodeTracker
    {
        IEnumerable<IDynamicLine> Lines { get; }

        bool ProcessChanges(
            ITextSnapshot currentSnapshot,
            List<SpanAndLineRange> newSpanChanges,
            IEnumerable<CodeSpanRange> newCodeCodeRanges);
    }
}

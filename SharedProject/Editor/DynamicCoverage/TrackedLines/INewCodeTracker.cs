using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface INewCodeTracker
    {
        IEnumerable<IDynamicLine> Lines { get; }

        bool ApplyNewCodeCodeRanges(IEnumerable<CodeSpanRange> newCodeCodeRanges);
        bool ProcessChanges(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanChanges);
    }
}

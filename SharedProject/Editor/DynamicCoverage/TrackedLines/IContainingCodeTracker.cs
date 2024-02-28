using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal enum ContainingCodeTrackerType { CoverageLines, NotIncluded, OtherLines}
    internal class ContainingCodeTrackerState
    {
        public ContainingCodeTrackerState(
            ContainingCodeTrackerType type,
            CodeSpanRange codeSpanRange,
            IEnumerable<IDynamicLine> lines
        )
        {
            Type = type;
            CodeSpanRange = codeSpanRange;
            Lines = lines;
        }

        public ContainingCodeTrackerType Type { get; }
        public CodeSpanRange CodeSpanRange { get; }
        public IEnumerable<IDynamicLine> Lines { get; }
    }
    interface IContainingCodeTracker
    {
        IContainingCodeTrackerProcessResult ProcessChanges(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanAndLineRanges);
        ContainingCodeTrackerState GetState();

        IEnumerable<IDynamicLine> Lines { get; }
    }
}

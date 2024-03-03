using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class ContainingCodeTrackerState
    {
        public ContainingCodeTrackerState(
            ContainingCodeTrackerType type,
            CodeSpanRange codeSpanRange,
            IEnumerable<IDynamicLine> lines
        )
        {
            this.Type = type;
            this.CodeSpanRange = codeSpanRange;
            this.Lines = lines;
        }

        public ContainingCodeTrackerType Type { get; }
        public CodeSpanRange CodeSpanRange { get; }
        public IEnumerable<IDynamicLine> Lines { get; }
    }
}

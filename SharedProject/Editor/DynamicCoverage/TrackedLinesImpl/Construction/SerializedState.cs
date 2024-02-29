using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class SerializedState
    {
        public SerializedState(CodeSpanRange codeSpanRange, ContainingCodeTrackerType type, List<DynamicLine> dynamicLines)
        {
            CodeSpanRange = codeSpanRange;
            Type = type;
            Lines = dynamicLines;
        }

        public static SerializedState From(ContainingCodeTrackerState containingCodeTrackerState)
        {
            return new SerializedState(
                containingCodeTrackerState.CodeSpanRange,
                containingCodeTrackerState.Type,
                containingCodeTrackerState.Lines.Select(line => new DynamicLine(line.Number, line.CoverageType)).ToList()
             );
        }

        public CodeSpanRange CodeSpanRange { get; set; }
        public ContainingCodeTrackerType Type { get; set; }
        public List<DynamicLine> Lines { get; set; }
    }

}

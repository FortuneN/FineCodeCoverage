using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class SerializedContainingCodeTracker
    {
        public SerializedContainingCodeTracker(CodeSpanRange codeSpanRange, ContainingCodeTrackerType type, List<DynamicLine> dynamicLines)
        {
            this.CodeSpanRange = codeSpanRange;
            this.Type = type;
            this.Lines = dynamicLines;
        }

        public static SerializedContainingCodeTracker From(ContainingCodeTrackerState containingCodeTrackerState)
            => new SerializedContainingCodeTracker(
                containingCodeTrackerState.CodeSpanRange,
                containingCodeTrackerState.Type,
                containingCodeTrackerState.Lines.Select(line => new DynamicLine(line.Number, line.CoverageType)).ToList()
             );

        public CodeSpanRange CodeSpanRange { get; set; }
        public ContainingCodeTrackerType Type { get; set; }
        public List<DynamicLine> Lines { get; set; }
    }
}

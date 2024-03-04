using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface IContainingCodeTracker
    {
        IContainingCodeTrackerProcessResult ProcessChanges(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanAndLineRanges);
        ContainingCodeTrackerState GetState();

        IEnumerable<IDynamicLine> Lines { get; }
    }
}

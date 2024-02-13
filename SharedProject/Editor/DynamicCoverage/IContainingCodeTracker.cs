using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface IContainingCodeTrackerProcessResult
    {
        bool Changed { get; }
        List<Span> UnprocessedSpans { get; }
    }
    interface IContainingCodeTracker
    {
        IContainingCodeTrackerProcessResult ProcessChanges(ITextSnapshot currentSnapshot, List<Span> newSpanChanges);
        IEnumerable<IDynamicLine> Lines { get; }
    }
}

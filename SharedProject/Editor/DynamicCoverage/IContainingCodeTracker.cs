using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface IContainingCodeTracker
    {
        bool ProcessChanges(ITextSnapshot currentSnapshot, List<Span> newSpanChanges);
        IEnumerable<IDynamicLine> Lines { get; }
    }
}

using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    interface IContainingCodeTracker
    {
        bool ProcessChanges(ITextSnapshot currentSnapshot, List<Span> newSpanChanges);
        IEnumerable<IDynamicLine> Lines { get; }
    }
}

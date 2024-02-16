using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface INewCodeTracker
    {
        IEnumerable<IDynamicLine> Lines { get; }

        bool ProcessChanges(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanChanges);
    }
}

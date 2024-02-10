using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface ITrackedLines
    {
        IEnumerable<IDynamicLine> GetLines(int startLineNumber, int endLineNumber);
        bool Changed(ITextSnapshot currentSnapshot, List<Span> newSpanChanges);
    }
}

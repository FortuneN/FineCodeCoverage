using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface ITrackedLines
    {
        IEnumerable<IDynamicLine> GetLines(int startLineNumber, int endLineNumber);
        bool Changed(ITextSnapshot currentSnapshot, List<Span> newSpanChanges);
    }
}

using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ITrackedLines
    {
        IEnumerable<IDynamicLine> GetLines(int startLineNumber, int endLineNumber);
        IEnumerable<int> GetChangedLineNumbers(ITextSnapshot currentSnapshot, List<Span> newSpanChanges);
    }
}

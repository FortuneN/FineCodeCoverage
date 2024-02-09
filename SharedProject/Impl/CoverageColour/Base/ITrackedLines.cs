using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    interface IDynamicLine : ILine
    {
        bool IsDirty { get; }
    }
    interface ITrackedLines
    {
        IEnumerable<IDynamicLine> GetLines(int startLineNumber, int endLineNumber);
        bool Changed(ITextSnapshot currentSnapshot, List<Span> newSpanChanges);
    }
}

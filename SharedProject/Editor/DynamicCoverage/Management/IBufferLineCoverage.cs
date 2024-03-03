using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface IBufferLineCoverage
    {
        IEnumerable<IDynamicLine> GetLines(int startLineNumber, int endLineNumber);
    }
}

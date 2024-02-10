using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface IBufferLineCoverage
    {
        IEnumerable<IDynamicLine> GetLines(int startLineNumber, int endLineNumber);
    }
}

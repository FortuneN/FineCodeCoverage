using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    interface IBufferLineCoverage
    {
        IEnumerable<IDynamicLine> GetLines(int startLineNumber, int endLineNumber);
    }
}

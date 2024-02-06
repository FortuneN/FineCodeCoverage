using FineCodeCoverage.Engine.Model;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    interface IBufferLineCoverage
    {
        IEnumerable<ILine> GetLines(int startLineNumber, int endLineNumber);
    }
}

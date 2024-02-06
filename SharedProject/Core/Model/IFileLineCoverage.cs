using System.Collections.Generic;

namespace FineCodeCoverage.Engine.Model
{
    internal interface IFileLineCoverage
    {
        IEnumerable<ILine> GetLines(string filePath, int startLineNumber, int endLineNumber);
    }
}

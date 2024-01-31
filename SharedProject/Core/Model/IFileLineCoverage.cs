using FineCodeCoverage.Engine.Cobertura;
using System.Collections.Generic;

namespace FineCodeCoverage.Engine.Model
{
    internal interface IFileLineCoverage
    {
        IEnumerable<Line> GetLines(string filePath, int startLineNumber, int endLineNumber);
    }
}

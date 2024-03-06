using System.Collections.Generic;

namespace FineCodeCoverage.Engine.Model
{
    internal interface IFileLineCoverage
    {
        void Add(string filename, IEnumerable<ILine> line);
        IEnumerable<ILine> GetLines(string filePath, int startLineNumber, int endLineNumber);
        IEnumerable<ILine> GetLines(string filePath);
        void Sort();
        void UpdateRenamed(string oldFile, string newFile);
    }
}

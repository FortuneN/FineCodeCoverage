using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Impl;
using System;
using System.Collections.Generic;

namespace FineCodeCoverage.Engine.Model
{
    internal interface ILine
    {
        int Number { get; }
        CoverageType CoverageType { get; }
    }

    // FileLineCoverage maps from a filename to the list of lines in the file
    internal class FileLineCoverage : IFileLineCoverage
    {
        private readonly Dictionary<string, List<ILine>> m_coverageLines = new Dictionary<string, List<ILine>>(StringComparer.OrdinalIgnoreCase);

        public void Add(string filename, IEnumerable<ILine> lines)
        {
            if (!m_coverageLines.TryGetValue(filename, out var fileCoverageLines))
            {
                fileCoverageLines = new List<ILine>();
                m_coverageLines.Add(filename, fileCoverageLines);
            }

            fileCoverageLines.AddRange(lines);
        }

        public void Completed()
        {
            foreach (var lines in m_coverageLines.Values)
                lines.Sort((a, b) => a.Number - b.Number);
        }

        public IEnumerable<ILine> GetLines(string filePath, int startLineNumber, int endLineNumber)
        {
            if (!m_coverageLines.TryGetValue(filePath, out var lines))
                yield break;

            int first = lines.LowerBound(line => startLineNumber - line.Number);
            if (first != -1)
            {
                for (int it = first; it < lines.Count && lines[it].Number <= endLineNumber; ++it)
                    yield return lines[it];
            }
            
        }
    }

    
}

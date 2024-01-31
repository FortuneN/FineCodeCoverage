using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Cobertura;
using System;
using System.Collections.Generic;

namespace FineCodeCoverage.Engine.Model
{
    // FileLineCoverage maps from a filename to the list of lines in the file
    internal class FileLineCoverage : IFileLineCoverage
    {
        private Dictionary<string, List<Line>> m_coverageLines = new Dictionary<string, List<Line>>(StringComparer.OrdinalIgnoreCase);

        public void Add(string filename, IEnumerable<Line> lines)
        {
            if (!m_coverageLines.TryGetValue(filename, out var fileCoverageLines))
            {
                fileCoverageLines = new List<Line>();
                m_coverageLines.Add(filename, fileCoverageLines);
            }

            fileCoverageLines.AddRange(lines);
        }

        public void Completed()
        {
            foreach (var lines in m_coverageLines.Values)
                lines.Sort((a, b) => a.Number - b.Number);
        }

        public IEnumerable<Line> GetLines(string filePath, int startLineNumber, int endLineNumber)
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

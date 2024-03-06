using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Impl;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Engine.Model
{
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

        public void Sort()
        {
            foreach (var lines in m_coverageLines.Values)
                lines.Sort((a, b) => a.Number - b.Number);
        }

        public IEnumerable<ILine> GetLines(string filePath)
        {
            if (!m_coverageLines.TryGetValue(filePath, out var lines))
            {
                lines = Enumerable.Empty<ILine>().ToList();
            }
            return lines;
                
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

        public void UpdateRenamed(string oldFilePath, string newFilePath)
        {
            if(m_coverageLines.TryGetValue(oldFilePath, out var lines))
            {
                m_coverageLines.Add(newFilePath, lines);
                m_coverageLines.Remove(oldFilePath);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Engine.Model
{

    internal class UniqueCoverageLines : HashSet<ILine>
    {
        public UniqueCoverageLines() : base(new LineComparer())
        {
        }

        public void AddRange(IEnumerable<ILine> lines)
        {
            foreach (var line in lines)
                Add(line);
        }

        private IEnumerable<ILine> sortedLines;
        public IEnumerable<ILine> SortedLines => sortedLines;

        public void Sort()
        {
            sortedLines = this.OrderBy(l => l.Number).ToList();
        }

        class LineComparer : IEqualityComparer<ILine>
        {
            public bool Equals(ILine x, ILine y)
            {
                return x.Number == y.Number;
            }

            public int GetHashCode(ILine obj)
            {
                return obj.Number;
            }
        }
    }

    // FileLineCoverage maps from a filename to the list of lines in the file
    internal class FileLineCoverage : IFileLineCoverage
    {
        private readonly Dictionary<string, UniqueCoverageLines> m_coverageLines = new Dictionary<string, UniqueCoverageLines>(StringComparer.OrdinalIgnoreCase);

        public void Add(string filename, IEnumerable<ILine> lines)
        {
            if (!m_coverageLines.TryGetValue(filename, out var fileCoverageLines))
            {
                fileCoverageLines = new UniqueCoverageLines();
                m_coverageLines.Add(filename, fileCoverageLines);
            }

            fileCoverageLines.AddRange(lines);
        }

        public void Sort()
        {
            foreach (var lines in m_coverageLines.Values)
                lines.Sort();
        }

        public IEnumerable<ILine> GetLines(string filePath)
        {
            if (!m_coverageLines.TryGetValue(filePath, out var lines))
            {
                return Enumerable.Empty<ILine>().ToList();
            }
            return lines.SortedLines;
                
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

using FineCodeCoverage.Engine.Cobertura;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Engine.Model
{
    // FileLineCoverage maps from a filename to the list of lines in the file
    internal class FileLineCoverage
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

        internal void Completed()
        {
            foreach (var lines in m_coverageLines.Values)
                lines.Sort((a, b) => a.Number - b.Number);
        }

        public IEnumerable<Line> GetLines(string filePath, int startLineNumber, int endLineNumber)
        {
            if (!m_coverageLines.TryGetValue(filePath, out var lines))
                return Enumerable.Empty<Line>();

            int first = lines.LowerBound(line => line.Number < startLineNumber);
            int last = first;
            while (last < lines.Count && lines[last].Number <= endLineNumber)
                ++last;

            return lines.GetRange(first, last);
        }
    }

    public static class ListExtensions
    {
        // Returns the index of the first element in a sorted list where the comparison function is false
        public static int LowerBound<T>(this IList<T> list, Func<T, bool> compare)
        {
            // binary search to find the first line
            int first = 0;
            int count = list.Count;
            while (count > 0)
            {
                int step = count / 2;
                int it = first + step;
                if (compare(list[it]))
                {
                    first = ++it;
                    count -= step + 1;
                }
                else
                {
                    count = step;
                }
            }

            return first;
        }
    }
}

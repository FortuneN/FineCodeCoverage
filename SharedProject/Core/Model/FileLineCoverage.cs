using FineCodeCoverage.Engine.Cobertura;
using FineCodeCoverage.Engine.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;

namespace SharedProject.Core.Model
{
    // FileLineCoverage maps from a filename to the list of lines in the file
    internal class FileLineCoverage
    {
        private Dictionary<string, List<Line>> m_coverageLines = new Dictionary<string, List<Line>>(StringComparer.OrdinalIgnoreCase);


        public void Add(string filename, IEnumerable<Line> lines)
        {
            if (!m_coverageLines.TryGetValue(filename, out var classCoverageLines))
            {
                classCoverageLines = new List<Line>();
                m_coverageLines.Add(filename, classCoverageLines);
            }

            classCoverageLines.AddRange(lines);
            classCoverageLines.Sort((a, b) => a.Number - b.Number);
        }

        public IEnumerable<Line> GetLines(string filePath, int startLineNumber, int endLineNumber)
        {
            if (!m_coverageLines.TryGetValue(filePath, out var lines))
                yield break;

            // binary search to find the first line
            int first = 0;
            int count = lines.Count;
            while (count > 0)
            {
                int step = count / 2;
                int it = first + step;
                if (lines[it].Number < startLineNumber)
                {
                    first = ++it;
                    count -= step + 1;
                }
                else
                {
                    count = step;
                }
            }

            for (int it = first; it < lines.Count && lines[it].Number <= endLineNumber; ++it)
                yield return lines[it];
        }
    }
}

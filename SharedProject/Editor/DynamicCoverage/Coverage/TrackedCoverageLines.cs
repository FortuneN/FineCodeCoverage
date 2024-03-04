using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class TrackedCoverageLines : ITrackedCoverageLines
    {
        private readonly List<ICoverageLine> coverageLines;

        public IEnumerable<IDynamicLine> Lines => this.coverageLines.Select(coverageLine => coverageLine.Line);
        public TrackedCoverageLines(List<ICoverageLine> coverageLines) => this.coverageLines = coverageLines;

        public bool Update(ITextSnapshot currentSnapshot)
        {
            bool changed = false;
            foreach (ICoverageLine coverageLine in this.coverageLines)
            {
                bool updated = coverageLine.Update(currentSnapshot);
                changed = changed || updated;
            }

            return changed;
        }
    }
}

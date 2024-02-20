using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    class TrackedCoverageLines : ITrackedCoverageLines
    {
        private readonly List<ICoverageLine> coverageLines;

        public IEnumerable<IDynamicLine> Lines => coverageLines.Select(coverageLine => coverageLine.Line);
        public TrackedCoverageLines(List<ICoverageLine> coverageLines)
        {
            this.coverageLines = coverageLines;
        }

        public bool Update(ITextSnapshot currentSnapshot)
        {
            var changed = false;
            foreach (var coverageLine in coverageLines)
            {
                var updated = coverageLine.Update(currentSnapshot);
                changed = changed || updated;
            }
            return changed;
        }
    }

}

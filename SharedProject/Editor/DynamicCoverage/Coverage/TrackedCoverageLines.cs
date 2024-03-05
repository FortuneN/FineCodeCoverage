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

        public IEnumerable<int> GetUpdatedLineNumbers(ITextSnapshot currentSnapshot) 
            => this.coverageLines.SelectMany(coverageLine => coverageLine.Update(currentSnapshot));
    }
}

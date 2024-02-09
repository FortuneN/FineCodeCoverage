using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(ITrackedCoverageLinesFactory))]
    internal class TrackedCoverageLinesFactory : ITrackedCoverageLinesFactory
    {
        public ITrackedCoverageLines Create(List<ICoverageLine> coverageLines)
        {
            return new TrackedCoverageLines(coverageLines);
        }
    }
}

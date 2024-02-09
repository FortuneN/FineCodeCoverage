using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal interface ITrackedCoverageLinesFactory
    {
        ITrackedCoverageLines Create(List<ICoverageLine> coverageLines);
    }

}

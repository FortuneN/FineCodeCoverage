using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ITrackedCoverageLinesFactory
    {
        ITrackedCoverageLines Create(List<ICoverageLine> coverageLines);
    }
}

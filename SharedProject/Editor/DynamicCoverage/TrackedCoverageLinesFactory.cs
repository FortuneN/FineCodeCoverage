using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(ITrackedCoverageLinesFactory))]
    internal class TrackedCoverageLinesFactory : ITrackedCoverageLinesFactory
    {
        public ITrackedCoverageLines Create(List<ICoverageLine> coverageLines)
        {
            return new TrackedCoverageLines(coverageLines);
        }
    }
}

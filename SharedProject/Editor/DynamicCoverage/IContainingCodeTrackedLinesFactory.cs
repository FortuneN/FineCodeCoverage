using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal interface IContainingCodeTrackedLinesFactory
    {
        ITrackedLines Create(List<IContainingCodeTracker> containingCodeTrackers);
    }
}

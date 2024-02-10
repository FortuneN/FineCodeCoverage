using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface IContainingCodeTrackedLinesFactory
    {
        ITrackedLines Create(List<IContainingCodeTracker> containingCodeTrackers);
    }
}

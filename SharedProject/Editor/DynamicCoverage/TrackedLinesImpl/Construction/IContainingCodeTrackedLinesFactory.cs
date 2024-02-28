using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface IContainingCodeTrackedLinesFactory
    {
        TrackedLines Create(List<IContainingCodeTracker> containingCodeTrackers,INewCodeTracker newCodeTracker);
    }
}

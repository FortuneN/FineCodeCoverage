using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface IContainingCodeTrackedLinesFactory
    {
        IContainingCodeTrackerTrackedLines Create(
            List<IContainingCodeTracker> containingCodeTrackers,
            INewCodeTracker newCodeTracker,
             IFileCodeSpanRangeService fileCodeSpanRangeService
            );
    }
}

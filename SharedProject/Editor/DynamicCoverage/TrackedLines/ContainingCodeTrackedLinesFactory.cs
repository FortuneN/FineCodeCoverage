using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(IContainingCodeTrackedLinesFactory))]
    internal class ContainingCodeTrackedLinesFactory : IContainingCodeTrackedLinesFactory
    {
        public IContainingCodeTrackerTrackedLines Create(
            List<IContainingCodeTracker> containingCodeTrackers,
            INewCodeTracker newCodeTracker,
            IFileCodeSpanRangeService fileCodeSpanRangeService
        ) => new TrackedLines(containingCodeTrackers, newCodeTracker, fileCodeSpanRangeService);
    }
}

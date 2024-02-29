using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(IContainingCodeTrackedLinesFactory))]
    internal class ContainingCodeTrackedLinesFactory : IContainingCodeTrackedLinesFactory
    {
        private readonly IRolsynCodeSpanRangeService roslynCodeSpanRangeService;

        [ImportingConstructor]
        public ContainingCodeTrackedLinesFactory(
            IRolsynCodeSpanRangeService roslynCodeSpanRangeService
        )
        {
            this.roslynCodeSpanRangeService = roslynCodeSpanRangeService;
        }
        public TrackedLines Create(List<IContainingCodeTracker> containingCodeTrackers,INewCodeTracker newCodeTracker)
        {
            return new TrackedLines(containingCodeTrackers,newCodeTracker, newCodeTracker == null ? null : roslynCodeSpanRangeService);
        }
    }
}

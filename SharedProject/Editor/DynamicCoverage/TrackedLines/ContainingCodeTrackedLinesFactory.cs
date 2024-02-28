using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(IContainingCodeTrackedLinesFactory))]
    internal class ContainingCodeTrackedLinesFactory : IContainingCodeTrackedLinesFactory
    {
        public TrackedLines Create(List<IContainingCodeTracker> containingCodeTrackers,INewCodeTracker newCodeTracker)
        {
            return new TrackedLines(containingCodeTrackers,newCodeTracker);
        }
    }
}

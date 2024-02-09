using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Impl
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(IContainingCodeTrackedLinesFactory))]
    internal class ContainingCodeTrackedLinesFactory : IContainingCodeTrackedLinesFactory
    {
        public ITrackedLines Create(List<IContainingCodeTracker> containingCodeTrackers)
        {
            return new TrackedLines(containingCodeTrackers);
        }
    }
}

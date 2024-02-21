using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(INewCodeTrackerFactory))]
    internal class NewCodeTrackerFactory : INewCodeTrackerFactory
    {
        private readonly ITrackingLineFactory trackingLineFactory;
        private readonly ILineExcluder codeLineExcluder;

        [ImportingConstructor]
        public NewCodeTrackerFactory(
            ITrackingLineFactory trackingLineFactory,
            ILineExcluder codeLineExcluder
        )
        {
            this.trackingLineFactory = trackingLineFactory;
            this.codeLineExcluder = codeLineExcluder;
        }
        public INewCodeTracker Create(bool isCSharp)
        {
            return new NewCodeTracker(isCSharp, new TrackedNewLineFactory(trackingLineFactory), codeLineExcluder);
        }
    }
}

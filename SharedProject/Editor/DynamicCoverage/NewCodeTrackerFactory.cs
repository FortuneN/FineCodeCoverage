using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(INewCodeTrackerFactory))]
    internal class NewCodeTrackerFactory : INewCodeTrackerFactory
    {
        private readonly ITrackingLineFactory trackingLineFactory;

        [ImportingConstructor]
        public NewCodeTrackerFactory(
            ITrackingLineFactory trackingLineFactory
        )
        {
            this.trackingLineFactory = trackingLineFactory;
        }
        public INewCodeTracker Create(bool isCSharp)
        {
            return new NewCodeTracker(isCSharp, new TrackedNewLineFactory(trackingLineFactory), new CodeLineExcluder());
        }
    }
}

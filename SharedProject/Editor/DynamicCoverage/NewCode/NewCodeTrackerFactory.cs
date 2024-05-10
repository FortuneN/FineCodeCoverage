using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(INewCodeTrackerFactory))]
    internal class NewCodeTrackerFactory : INewCodeTrackerFactory
    {
        private readonly ITrackedNewCodeLineFactory trackedNewCodeLineFactory;

        [ImportingConstructor]
        public NewCodeTrackerFactory(
            ITrackedNewCodeLineFactory trackedNewCodeLineFactory
        ) => this.trackedNewCodeLineFactory = trackedNewCodeLineFactory;

        public INewCodeTracker Create(ILineExcluder lineExcluder) => new NewCodeTracker(this.trackedNewCodeLineFactory, lineExcluder);

        public INewCodeTracker Create(ILineExcluder lineExcluder, IEnumerable<int> lineNumbers, ITextSnapshot textSnapshot)
            => new NewCodeTracker(this.trackedNewCodeLineFactory, lineExcluder, lineNumbers, textSnapshot);
    }
}

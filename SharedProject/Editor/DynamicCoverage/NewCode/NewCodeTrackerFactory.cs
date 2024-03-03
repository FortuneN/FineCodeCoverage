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
        private readonly ILineExcluder codeLineExcluder;

        [ImportingConstructor]
        public NewCodeTrackerFactory(
            ITrackedNewCodeLineFactory trackedNewCodeLineFactory,
            ILineExcluder codeLineExcluder
        )
        {
            this.trackedNewCodeLineFactory = trackedNewCodeLineFactory;
            this.codeLineExcluder = codeLineExcluder;
        }
        public INewCodeTracker Create(bool isCSharp) => new NewCodeTracker(isCSharp, this.trackedNewCodeLineFactory, this.codeLineExcluder);

        public INewCodeTracker Create(bool isCSharp, List<int> lineNumbers, ITextSnapshot textSnapshot)
            => new NewCodeTracker(isCSharp, this.trackedNewCodeLineFactory, this.codeLineExcluder, lineNumbers, textSnapshot);
    }
}

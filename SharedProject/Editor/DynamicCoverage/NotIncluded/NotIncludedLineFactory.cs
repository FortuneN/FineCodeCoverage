using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(INotIncludedLineFactory))]
    internal class NotIncludedLineFactory : INotIncludedLineFactory
    {
        private readonly ILineTracker lineTracker;

        [ImportingConstructor]
        public NotIncludedLineFactory(
             ILineTracker lineTracker
        ) => this.lineTracker = lineTracker;

        public ITrackingLine Create(ITrackingSpan startTrackingSpan, ITextSnapshot currentSnapshot)
            => new TrackingLine(startTrackingSpan, currentSnapshot, this.lineTracker, DynamicCoverageType.NotIncluded);
    }
}

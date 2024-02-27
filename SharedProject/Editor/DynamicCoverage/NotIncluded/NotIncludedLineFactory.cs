using Microsoft.VisualStudio.Text;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

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
        )
        {
            this.lineTracker = lineTracker;
        }
        public ITrackingLine Create(ITrackingSpan startTrackingSpan, ITextSnapshot currentSnapshot)
        {
            return new NotIncludedTrackingLine(startTrackingSpan, currentSnapshot, lineTracker);
        }
    }
}

using Microsoft.VisualStudio.Text;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(ITrackedNewCodeLineFactory))]
    internal class TrackedNewLineFactory : ITrackedNewCodeLineFactory
    {
        private readonly ITrackingLineFactory trackingLineFactory;
        private readonly ILineTracker lineTracker;

        [ImportingConstructor]
        public TrackedNewLineFactory(
            ITrackingLineFactory trackingLineFactory,
            ILineTracker lineTracker
            )
        {
            this.trackingLineFactory = trackingLineFactory;
            this.lineTracker = lineTracker;
        }
        public ITrackedNewCodeLine Create(ITextSnapshot textSnapshot, SpanTrackingMode spanTrackingMode, int lineNumber)
        {
            var trackingSpan = trackingLineFactory.CreateTrackingSpan(textSnapshot, lineNumber, spanTrackingMode);
            return new TrackedNewCodeLine(trackingSpan, lineNumber, lineTracker);
        }
    }
}

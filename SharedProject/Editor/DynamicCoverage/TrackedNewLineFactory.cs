using Microsoft.VisualStudio.Text;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    internal class TrackedNewLineFactory : ITrackedNewCodeLineFactory
    {
        private readonly ITrackingLineFactory trackingLineFactory;

        public TrackedNewLineFactory(ITrackingLineFactory trackingLineFactory)
        {
            this.trackingLineFactory = trackingLineFactory;
        }
        public ITrackedNewCodeLine Create(ITextSnapshot textSnapshot, SpanTrackingMode spanTrackingMode, int lineNumber)
        {
            var trackingSpan = trackingLineFactory.CreateTrackingSpan(textSnapshot, lineNumber, spanTrackingMode);
            return new TrackedNewCodeLine(trackingSpan, lineNumber, new LineTracker());
        }
    }
}

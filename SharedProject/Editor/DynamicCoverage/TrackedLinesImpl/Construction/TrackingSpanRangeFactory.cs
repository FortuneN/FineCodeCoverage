using Microsoft.VisualStudio.Text;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(ITrackingSpanRangeFactory))]
    internal class TrackingSpanRangeFactory : ITrackingSpanRangeFactory
    {
        private readonly ILineTracker lineTracker;

        [ImportingConstructor]
        public TrackingSpanRangeFactory(ILineTracker lineTracker)
        {
            this.lineTracker = lineTracker;
        }

        public ITrackingSpanRange Create(ITrackingSpan startTrackingSpan, ITrackingSpan endTrackingSpan, ITextSnapshot currentSnapshot)
        {
            return new TrackingSpanRange(startTrackingSpan,endTrackingSpan, currentSnapshot, lineTracker);
        }
    }
}

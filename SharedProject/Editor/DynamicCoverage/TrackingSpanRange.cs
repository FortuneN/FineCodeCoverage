using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class TrackingSpanRange : ITrackingSpanRange
    {
        private readonly List<ITrackingSpan> trackingSpans;

        public TrackingSpanRange(List<ITrackingSpan> trackingSpans)
        {
            this.trackingSpans = trackingSpans;
        }

        public bool IntersectsWith(ITextSnapshot currentSnapshot, List<Span> newSpanChanges)
        {
            foreach (var trackingSpan in trackingSpans)
            {
                var currentSpan = trackingSpan.GetSpan(currentSnapshot).Span;
                var spanIntersected = newSpanChanges.Any(newSpan => newSpan.IntersectsWith(currentSpan));
                if (spanIntersected)
                {
                    return true;
                }
            }
            return false;
        }
    }

}

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

        public List<Span> GetNonIntersecting(ITextSnapshot currentSnapshot, List<Span> newSpanChanges)
        {
            var removals = new List<ITrackingSpan>();
            foreach (var trackingSpan in trackingSpans)
            {
                var currentSnapshotSpan = trackingSpan.GetSpan(currentSnapshot);
                var currentSpan = currentSnapshotSpan.Span;
                newSpanChanges = newSpanChanges.Where(newSpanChange => !newSpanChange.IntersectsWith(currentSpan)).ToList();
                if (currentSpan.IsEmpty)
                {
                    removals.Add(trackingSpan);
                }
            }
            removals.ForEach(trackingSpan => trackingSpans.Remove(trackingSpan));
            return newSpanChanges;
        }
    }

}

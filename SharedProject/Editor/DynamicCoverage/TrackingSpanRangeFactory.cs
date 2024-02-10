using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(ITrackingSpanRangeFactory))]
    internal class TrackingSpanRangeFactory : ITrackingSpanRangeFactory
    {
        public ITrackingSpanRange Create(List<ITrackingSpan> trackingSpans)
        {
            return new TrackingSpanRange(trackingSpans);
        }
    }
}

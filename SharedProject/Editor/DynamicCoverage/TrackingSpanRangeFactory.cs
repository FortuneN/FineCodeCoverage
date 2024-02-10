using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(ITrackingSpanRangeFactory))]
    internal class TrackingSpanRangeFactory : ITrackingSpanRangeFactory
    {
        public ITrackingSpanRange Create(List<ITrackingSpan> trackingSpans)
        {
            return new TrackingSpanRange(trackingSpans);
        }
    }
}

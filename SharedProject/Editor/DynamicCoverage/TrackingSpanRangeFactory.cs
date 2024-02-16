using Microsoft.VisualStudio.Text;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(ITrackingSpanRangeFactory))]
    internal class TrackingSpanRangeFactory : ITrackingSpanRangeFactory
    {
        public ITrackingSpanRange Create(ITrackingSpan startTrackingSpan, ITrackingSpan endTrackingSpan, ITextSnapshot currentSnapshot)
        {
            return new TrackingSpanRange(startTrackingSpan,endTrackingSpan, currentSnapshot);
        }
    }
}

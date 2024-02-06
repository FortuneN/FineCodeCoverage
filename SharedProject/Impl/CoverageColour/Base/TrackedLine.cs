using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Impl
{
    internal class TrackedLine
    {
        public TrackedLineLine Line { get; }
        public ITrackingSpan TrackingSpan { get; }

        public TrackedLine(ILine line, ITrackingSpan trackingSpan)
        {
            Line = new TrackedLineLine(line);
            TrackingSpan = trackingSpan;
        }
    }

}

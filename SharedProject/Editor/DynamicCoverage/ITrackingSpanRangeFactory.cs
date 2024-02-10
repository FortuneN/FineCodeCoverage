using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal interface ITrackingSpanRangeFactory
    {
        ITrackingSpanRange Create(List<ITrackingSpan> trackingSpans);
    }

}

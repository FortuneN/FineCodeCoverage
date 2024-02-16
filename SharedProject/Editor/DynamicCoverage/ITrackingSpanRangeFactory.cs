using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ITrackingSpanRangeFactory
    {
        ITrackingSpanRange Create(List<ITrackingSpan> trackingSpans, ITextSnapshot currentSnapshot);
    }

}

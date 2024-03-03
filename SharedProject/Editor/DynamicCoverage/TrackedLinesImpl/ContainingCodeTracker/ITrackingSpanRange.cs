using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ITrackingSpanRange
    {
        TrackingSpanRangeProcessResult Process(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanAndLIneRanges);
        ITrackingSpan GetFirstTrackingSpan();
        CodeSpanRange ToCodeSpanRange();
    }
}

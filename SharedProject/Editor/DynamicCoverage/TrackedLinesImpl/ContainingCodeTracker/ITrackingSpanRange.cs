using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ITrackingSpanRange
    {
        TrackingSpanRangeProcessResult Process(ITextSnapshot currentSnapshot, List<SpanAndLineRange> newSpanAndLIneRanges);
        ITrackingSpan GetFirstTrackingSpan();
        CodeSpanRange ToCodeSpanRange();
    }
}

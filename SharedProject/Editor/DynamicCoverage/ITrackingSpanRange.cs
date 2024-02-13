using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface ITrackingSpanRange
    {
        List<Span> GetNonIntersecting(ITextSnapshot currentSnapshot, List<Span> newSpanChanges);
    }

}

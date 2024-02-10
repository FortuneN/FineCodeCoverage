using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface ITrackingSpanRange
    {
        bool IntersectsWith(ITextSnapshot currentSnapshot, List<Span> newSpanChanges);
    }

}

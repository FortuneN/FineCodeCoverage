using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    interface ITrackingSpanRange
    {
        bool IntersectsWith(ITextSnapshot currentSnapshot, List<Span> newSpanChanges);
    }

}

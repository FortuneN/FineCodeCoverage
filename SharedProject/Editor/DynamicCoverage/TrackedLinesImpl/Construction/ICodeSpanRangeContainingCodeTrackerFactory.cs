using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ICodeSpanRangeContainingCodeTrackerFactory
    {
        IContainingCodeTracker CreateNotIncluded(ITextSnapshot textSnapshot, CodeSpanRange containingRange, SpanTrackingMode spanTrackingMode);
        IContainingCodeTracker CreateCoverageLines(ITextSnapshot textSnapshot, List<ILine> lines, CodeSpanRange containingRange, SpanTrackingMode spanTrackingMode);
        IContainingCodeTracker CreateOtherLines(ITextSnapshot textSnapshot, CodeSpanRange containingRange, SpanTrackingMode spanTrackingMode);
        IContainingCodeTracker CreateDirty(ITextSnapshot currentSnapshot, CodeSpanRange codeSpanRange, SpanTrackingMode spanTrackingMode);
    }
}

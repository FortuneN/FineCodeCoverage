using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ILinesContainingCodeTrackerFactory
    {
        IContainingCodeTracker Create(ITextSnapshot textSnapshot, List<ILine> lines, CodeSpanRange containingRange,SpanTrackingMode spanTrackingMode);
        IContainingCodeTracker CreateOtherLinesTracker(ITextSnapshot textSnapshot, CodeSpanRange containingRange, SpanTrackingMode spanTrackingMode);
    }
}

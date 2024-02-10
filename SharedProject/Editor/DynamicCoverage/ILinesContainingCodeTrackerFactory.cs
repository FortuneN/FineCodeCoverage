using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal interface ILinesContainingCodeTrackerFactory
    {
        IContainingCodeTracker Create(ITextSnapshot textSnapshot, List<ILine> lines, CodeSpanRange containingRange);
        IContainingCodeTracker Create(ITextSnapshot textSnapshot, ILine line);
    }
}

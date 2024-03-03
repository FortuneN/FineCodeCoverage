using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface INewCodeTrackerFactory
    {
        INewCodeTracker Create(bool isCSharp);
        INewCodeTracker Create(bool isCSharp, List<int> lineNumbers, ITextSnapshot textSnapshot);
    }
}

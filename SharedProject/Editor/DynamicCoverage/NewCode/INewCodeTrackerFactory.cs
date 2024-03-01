using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface INewCodeTrackerFactory
    {
        INewCodeTracker Create(bool isCSharp);
        INewCodeTracker Create(bool isCSharp, List<int> lineNumbers, ITextSnapshot textSnapshot);
    }
}

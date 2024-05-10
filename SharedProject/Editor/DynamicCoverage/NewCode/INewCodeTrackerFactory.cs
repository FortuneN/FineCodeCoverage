using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface INewCodeTrackerFactory
    {
        INewCodeTracker Create(ILineExcluder lineExcluder);
        INewCodeTracker Create(ILineExcluder lineExcluder, IEnumerable<int> lineNumbers, ITextSnapshot textSnapshot);
    }
}

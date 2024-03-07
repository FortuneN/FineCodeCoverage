using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ITrackingLine
    {
        IDynamicLine Line { get; }

        List<int> Update(ITextSnapshot currentSnapshot);
    }
}

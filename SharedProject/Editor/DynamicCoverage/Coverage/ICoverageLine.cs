using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ICoverageLine
    {
        List<int> Update(ITextSnapshot currentSnapshot);
        IDynamicLine Line { get; }
    }
}

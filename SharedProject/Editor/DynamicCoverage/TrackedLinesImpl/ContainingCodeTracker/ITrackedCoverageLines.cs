using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface ITrackedCoverageLines
    {
        IEnumerable<IDynamicLine> Lines { get; }
        bool Update(ITextSnapshot currentSnapshot);
    }
}

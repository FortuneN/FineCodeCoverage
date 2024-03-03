using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface ITrackedCoverageLines
    {
        IEnumerable<IDynamicLine> Lines { get; }
        bool Update(ITextSnapshot currentSnapshot);
    }
}

using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    interface ITrackedCoverageLines
    {
        IEnumerable<IDynamicLine> Lines { get; }
        void Dirty();
        bool Update(ITextSnapshot currentSnapshot);
    }

}

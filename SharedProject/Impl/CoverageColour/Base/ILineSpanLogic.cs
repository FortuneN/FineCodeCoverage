using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal interface ILineSpanLogic
    {
        IEnumerable<ILineSpan> GetLineSpans(IBufferLineCoverage bufferLineCoverage, NormalizedSnapshotSpanCollection spans);
    }
}

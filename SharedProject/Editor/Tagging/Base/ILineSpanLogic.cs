using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.Tagging.Base
{
    internal interface ILineSpanLogic
    {
        IEnumerable<ILineSpan> GetLineSpans(IBufferLineCoverage bufferLineCoverage, NormalizedSnapshotSpanCollection spans);
    }
}

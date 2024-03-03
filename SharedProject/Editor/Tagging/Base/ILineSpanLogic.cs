using System.Collections.Generic;
using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.Tagging.Base
{
    internal interface ILineSpanLogic
    {
        IEnumerable<ILineSpan> GetLineSpans(IBufferLineCoverage bufferLineCoverage, NormalizedSnapshotSpanCollection spans);
    }
}

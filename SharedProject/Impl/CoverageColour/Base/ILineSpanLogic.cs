using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal interface ILineSpanLogic
    {
        IEnumerable<ILineSpan> GetLineSpans(IFileLineCoverage fileLineCoverage, string filePath, NormalizedSnapshotSpanCollection spans);
    }
}

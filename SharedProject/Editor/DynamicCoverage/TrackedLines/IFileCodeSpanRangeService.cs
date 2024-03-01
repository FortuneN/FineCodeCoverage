using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface IFileCodeSpanRangeService
    {
        List<CodeSpanRange> GetFileCodeSpanRanges(ITextSnapshot snapshot);
    }
}

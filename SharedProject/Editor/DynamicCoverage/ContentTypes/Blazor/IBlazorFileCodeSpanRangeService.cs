using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor
{
    internal interface IBlazorFileCodeSpanRangeService {

        Task<List<CodeSpanRange>> GetFileCodeSpanRangesAsync(ITextSnapshot snapshot);
    }
}

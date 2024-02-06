using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FineCodeCoverage.Impl
{
    interface IRoslynService
    {
        Task<List<ContainingCodeLineRange>> GetContainingCodeLineRangesAsync(ITextSnapshot textSnapshot, List<int> list);
    }

}
